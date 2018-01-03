using System;
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

            var cfg = _GetCommandArgument(args, "-CFG:", "Root");

            var outDir = new PathString(_GetCommandArgument(args, "-OUT:", $"bin\\{cfg}")).AsAbsolute();
            var tmpDir = new PathString(_GetCommandArgument(args, "-TMP:", $"obj\\{cfg}")).AsAbsolute();            

            // in simulate mode, we should replace the ContextWriters with dummy ones
            var targetTask = args.Any(item => item.StartsWith("-SIMULATE")) ? "SIMULATE" : "BUILD";

            return new CommandLineContext(targetTask, prjPath, outDir, tmpDir, cfg);
        }

        private CommandLineContext(string buildTarget, PathString inPrj, PathString outDir, PathString tmpDir, string cfg)
        {
            System.Diagnostics.Debug.Assert(inPrj.IsAbsolute);
            System.Diagnostics.Debug.Assert(outDir.IsAbsolute);
            System.Diagnostics.Debug.Assert(tmpDir.IsAbsolute);

            _TargetTask = buildTarget;

            _SrcPrj = inPrj;            

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

        private readonly PathString _SrcPrj;        
        
        private readonly PathString _OutDir;
        private readonly PathString _TmpDir;        

        private readonly string _Configuration;

        private bool _CancelRequested = false;

        #endregion

        #region properties

        public bool IsSimulation => _TargetTask == "SIMULATE";

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

        public static void Build(params string[] args)
        {
            using (var context = Create(args))
            {
                var monitor = Evaluation.MonitorContext.Create(context._Logger, System.Threading.CancellationToken.None, context);
                
                context.Build(_LoadPluginsFunc, monitor, context._TmpDir);                
            }
        }

        public void Build(Func<ProjectDOM.Project, PathString, Factory.Collection> evalPlugins, SDK.IMonitorContext monitor, PathString buildTarget)
        {
            _CancelRequested = false;            
            
            var state = new Evaluation.PipelineClientState.Manager();

            var prjFilePath = _SrcPrj;

            prjFilePath = prjFilePath.AsAbsolute();
            var dstDirPath = buildTarget.AsAbsolute();

            // load project
            var document = ProjectDOM.LoadProjectFrom(prjFilePath);

            // load plugins
            var prjDir = prjFilePath.DirectoryPath;
            var plugins = evalPlugins(document, prjDir);

            // create build context

            var buildSettings = Evaluation.BuildContext.Create(_Configuration, prjDir, dstDirPath, _TargetTask == "SIMULATE");                

            // do build
            ProjectDOM.BuildProject(document, buildSettings, plugins.CreateInstance, monitor, state);            

            if (!IsSimulation) CommitBuildResults(_TmpDir, _OutDir);
        }

        private void CommitBuildResults(PathString src, PathString dst)
        {
            System.IO.Directory.CreateDirectory(dst);

            foreach(var f in System.IO.Directory.EnumerateFiles(src))
            {
                var fsrc = new PathString(f);

                var fdst = dst.WithFileName(fsrc.FileName);

                System.IO.File.Copy(fsrc, fdst, true);

                foreach(var d in System.IO.Directory.EnumerateDirectories(src))
                {
                    var dsrc = new PathString(d);
                    var ddst = dst.WithFileName(dsrc.FileName);

                    CommitBuildResults(dsrc, ddst);
                }
                
            }
            
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
            sb.AppendLine($"Source Files: {_SrcPrj}");
            sb.AppendLine($"Configuration: {_Configuration}");
            sb.AppendLine($"Temp Directory: {_TmpDir}");
            sb.AppendLine($"Output Directory: {_OutDir}");
            sb.AppendLine($"Intermediate Directory: {_TmpDir}");            

            return sb.ToString();
        }

        #endregion
    }
}
