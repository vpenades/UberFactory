using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    using MONITOR = Func<int, string, bool>;

    public sealed class CommandLineContext : IDisposable
    {
        #region lifecycle

        public static void Build(string[] args)
        {
            using (var context = Create(args))
            {
                context.Build(_LoadPluginsFunc, _MonitorFunc);
            }
        }

        private static string _GetCommandArgument(string[] args, string cmd, string defval)
        {
            var part = args.FirstOrDefault(item => item.StartsWith(cmd, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(part) || part.Length == cmd.Length) return defval;

            return part.Substring(cmd.Length);
        }

        public static CommandLineContext Create(string[] args)
        {
            if (args == null || args.Length == 0) throw new ArgumentNullException(nameof(args));

            // todo: check if first arg is .exe and skip it

            var prjDir = System.IO.Path.GetDirectoryName(args[0]);
            var prjMsk = System.IO.Path.GetFileName(args[0]);

            if (!prjMsk.EndsWith(".UberFactory", StringComparison.OrdinalIgnoreCase)) prjMsk += ".UberFactory";

            var outDir = _GetCommandArgument(args, "-OUT:", "Bin");
            var tmpDir = _GetCommandArgument(args, "-TMP:", "Tmp");
            var cfg = _GetCommandArgument(args, "-CFG:", "Root");

            return new CommandLineContext(new PathString(prjDir), prjMsk, new PathString(outDir), new PathString(tmpDir), cfg);
        }

        private CommandLineContext(PathString prjDir, string prjMsk, PathString outDir, PathString tmpDir, string cfg)
        {
            _SrcDir = prjDir;
            _SrcMask = prjMsk;

            _OutDir = outDir;
            _TmpDir = tmpDir;

            _Configuration = cfg;

            _Logger = _CreateLoggerFactory();
        }

        private static Microsoft.Extensions.Logging.ILoggerFactory _CreateLoggerFactory()
        {
            #if NET462 || NETCOREAPP1_1

            var loggerFactory = new Microsoft.Extensions.Logging.LoggerFactory();
            Microsoft.Extensions.Logging.ConsoleLoggerExtensions.AddConsole(loggerFactory);

            return loggerFactory;

            #else

            return null;

            #endif
        }

        public void Dispose()
        {
            if (_Logger != null) { _Logger.Dispose(); _Logger = null; }
        }

        #endregion

        #region data

        private Microsoft.Extensions.Logging.ILoggerFactory _Logger;

        private readonly PathString _SrcDir;
        private readonly string _SrcMask;   // filter, it can be: "content.UberFactory" "*.UberFactory" "content*.UberFactory" , etc

        private readonly PathString _OutDir;
        private readonly PathString _TmpDir;

        private readonly string _Configuration;

        #endregion

        #region command line helpers

        private static bool _MonitorFunc(int progress, string text)
        {
            var line = String.Empty + progress + "% " + text;

            System.Diagnostics.Debug.WriteLine(line);
            System.Console.WriteLine(line);

            // http://stackoverflow.com/questions/10621287/c-sharp-how-to-receive-system-close-or-exit-events-in-a-commandline-application
            // https://social.msdn.microsoft.com/Forums/vstudio/en-US/707e9ae1-a53f-4918-8ac4-62a1eddb3c4a/detecting-console-application-exit-in-c?forum=csharpgeneral

            return false;
        }

        private static PluginManager _LoadPluginsFunc(ProjectDOM.Project project, PathString prjDir)
        {
            var assemblies = new HashSet<System.Reflection.Assembly>();

            #if (NET462)
            if (true) // load locally referenced assemblies
            {
                var arefs = System.Reflection.Assembly.GetEntryAssembly().GetReferencedAssemblies();
                foreach (var aname in arefs)
                {
                    if (string.IsNullOrWhiteSpace(aname.CodeBase)) continue;

                    var a = PluginLoader.Instance.UsePlugin(new PathString(aname.CodeBase));

                    assemblies.Add(a);
                }
            }
            #endif


            #if (NET462 || NETCOREAPP1_1)
            if (true) // load assemblies referenced by the project
            {
                foreach (var rpath in project.References)
                {

                    var fullPath = prjDir.MakeAbsolutePath(rpath);

                    var defass = PluginLoader.Instance.UsePlugin(fullPath);
                    if (defass == null) continue;

                    assemblies.Add(defass);
                }
            }
            #endif


            var plugins = new PluginManager();

            plugins.SetAssemblies(assemblies);

            return plugins;
        }

        #endregion

        #region API

        public void Build(Func<ProjectDOM.Project, PathString, PluginManager> evalPlugins, MONITOR monitor)
        {
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
                var buildSettings = BuildContext.Create(_Configuration, prjDir, dstDirPath);

                buildSettings.SetLogger(_Logger);

                // do build
                ProjectDOM.BuildProject(document, buildSettings, plugins.CreateNodeInstance, new PipelineEvaluator.Monitor());                
            }
        }                

        #endregion
    }
}
