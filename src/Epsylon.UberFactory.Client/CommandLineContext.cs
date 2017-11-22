﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory.Client
{
    using MONITOR = Func<int, string, bool>;

    public sealed partial class CommandLineContext : IDisposable , IProgress<float>
    {
        #region lifecycle        

        public static CommandLineContext Create(params string[] args)
        {
            if (args == null || args.Length == 0) throw new ArgumentNullException(nameof(args));

            // todo: check if first arg is .exe and skip it            

            var prjPath = new PathString(args.Where(item => !item.StartsWith("-")).First());
            prjPath = prjPath.AsAbsolute();

            var outDir = new PathString(_GetCommandArgument(args, "-OUT:", "Bin")).AsAbsolute();
            var tmpDir = new PathString(_GetCommandArgument(args, "-TMP:", "Tmp")).AsAbsolute();
            var cfg = _GetCommandArgument(args, "-CFG:", "Root");

            // in simulate mode, we should replace the ContextWriters with dummy ones
            var targetTask = args.Any(item => item.StartsWith("-SIMULATE")) ? "SIMULATE" : "BUILD";

            return new CommandLineContext(targetTask, prjPath.DirectoryPath, prjPath.FileName, outDir, tmpDir, cfg);
        }

        private CommandLineContext(string buildTarget, PathString prjDir, string prjMsk, PathString outDir, PathString tmpDir, string cfg)
        {
            System.Diagnostics.Debug.Assert(prjDir.IsAbsolute);
            System.Diagnostics.Debug.Assert(outDir.IsAbsolute);
            System.Diagnostics.Debug.Assert(tmpDir.IsAbsolute);

            _TargetTask = buildTarget;

            _SrcDir = prjDir;
            _SrcMask = prjMsk;

            _OutDir = outDir;
            _TmpDir = tmpDir;

            _Configuration = cfg;

            _Logger = _CreateLoggerFactory();

            System.Console.CancelKeyPress += Console_CancelKeyPress;
        }        

        public void Dispose()
        {
            System.Console.CancelKeyPress -= Console_CancelKeyPress;

            if (_Logger != null) { _Logger.Dispose(); _Logger = null; }
        }

        #endregion

        #region data

        private Microsoft.Extensions.Logging.ILoggerFactory _Logger;

        private readonly string _TargetTask; // BUILD | SIMULATE

        private readonly PathString _SrcDir;
        private readonly string _SrcMask;   // filter, it can be: "content.UberFactory" "*.UberFactory" "content*.UberFactory" , etc
        
        private readonly PathString _OutDir;
        private readonly PathString _TmpDir;        

        private readonly string _Configuration;

        private bool _CancelRequested = false;

        #endregion

        #region command line helpers

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            _CancelRequested = true;
        }

        public void Report(float value)
        {
            if (_CancelRequested) throw new OperationCanceledException();

            if (float.IsNaN(value)) return;

            value = value.Clamp(0, 1);

            _Logger
                .CreateLogger("Progress")
                .Log(Microsoft.Extensions.Logging.LogLevel.Trace, 0, "{0:0%}", null, (s, e) => string.Format(s,value));

            // http://stackoverflow.com/questions/10621287/c-sharp-how-to-receive-system-close-or-exit-events-in-a-commandline-application
            // https://social.msdn.microsoft.com/Forums/vstudio/en-US/707e9ae1-a53f-4918-8ac4-62a1eddb3c4a/detecting-console-application-exit-in-c?forum=csharpgeneral
        }       

        #endregion

        #region API        

        public static IEnumerable<Evaluation.BuildContext> Build(params string[] args)
        {
            using (var context = Create(args))
            {
                var monitor = Evaluation.MonitorContext.Create(context._Logger, System.Threading.CancellationToken.None, context);
                
                return context.Build(_LoadPluginsFunc, monitor);
            }
        }

        public IEnumerable<Evaluation.BuildContext> Build(Func<ProjectDOM.Project, PathString, Factory.Collection> evalPlugins, SDK.IMonitorContext monitor)
        {
            _CancelRequested = false;

            var bbbccc = new List<Evaluation.BuildContext>();

            foreach (var filePath in System.IO.Directory.GetFiles(_SrcDir, _SrcMask))
            {
                var prjFilePath = new PathString(filePath);

                prjFilePath = prjFilePath.AsAbsolute();
                var dstDirPath = _OutDir.AsAbsolute();

                // load project
                var document = ProjectDOM.LoadProjectFrom(prjFilePath);

                // load plugins
                var prjDir = prjFilePath.DirectoryPath;
                var plugins = evalPlugins(document, prjDir);

                // create build context

                var buildSettings = Evaluation.BuildContext.Create(_Configuration, prjDir, dstDirPath, _TargetTask == "SIMULATE");                

                // do build
                ProjectDOM.BuildProject(document, buildSettings, plugins.CreateInstance, monitor);

                bbbccc.Add(buildSettings);
            }

            return bbbccc;
        }

        private static void LoadProjectAssemblies(ProjectDOM.Project project, PathString prjDir)
        {
            foreach (var rpath in project.References)
            {
                var fullPath = prjDir.MakeAbsolutePath(rpath);

                PluginLoader.Instance.UsePlugin(fullPath);
            }            
        }

        private static string _GetCommandArgument(string[] args, string cmd, string defval)
        {
            var part = args.FirstOrDefault(item => item.StartsWith(cmd, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(part) || part.Length == cmd.Length) return defval;

            return part.Substring(cmd.Length);
        }

        private string _GetStatusReport()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Task: {_TargetTask}");
            sb.AppendLine($"Source Files: {System.IO.Path.Combine(_SrcDir,_SrcMask)}");
            sb.AppendLine($"Configuration: {_Configuration}");
            sb.AppendLine($"Output Directory: {_OutDir}");
            sb.AppendLine($"Intermediate Directory: {_TmpDir}");            

            return sb.ToString();
        }

        #endregion
    }
}