using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Epsylon.UberFactory
{
    using MONITOR = Func<int, string, bool>;

    public static class ProjectEvaluator
    {
        public static void EvaluateCommandLine(string[] args)
        {
            BuildFromCommandLine(args, _LoadPlugins, _MonitorFunc);
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

        public static void BuildFromCommandLine(string[] args, Func<ProjectDOM.Project, PathString, PluginManager> evalPlugins, MONITOR monitor)
        {
            if (args.Length < 2) throw new ArgumentException("argument missing");

            var prjFilePath = new PathString(args[0]);
            var dstDirPath = new PathString(args[1]);

            if (!prjFilePath.IsValidFilePath) throw new ArgumentException("Invalid project file");
            if (!dstDirPath.IsValidDirectoryPath) throw new ArgumentException("Invalid target directory");

            prjFilePath = prjFilePath.AsAbsolute();
            dstDirPath = dstDirPath.AsAbsolute();

            // load project
            var document = ProjectDOM.LoadProjectFrom(prjFilePath);

            // load plugins
            var prjDir = prjFilePath.DirectoryPath;
            var plugins = evalPlugins(document, prjDir);

            using (var logFactory = _CreateLoggerFactory())
            {
                // create build context
                var buildSettings = BuildContext.Create("Root", prjDir, dstDirPath);

                buildSettings.SetLogger(logFactory);

                // do build
                BuildProject(plugins, document, buildSettings, new PipelineEvaluator.Monitor());
            }
        }

        public static void BuildProject(PluginManager plugins, ProjectDOM.Project srcDoc, BuildContext bsettings, PipelineEvaluator.Monitor monitor)
        {
            var tasks = srcDoc
                .Items
                .OfType<ProjectDOM.Task>()
                .Where(item => item.Enabled)
                .ToArray();

            for (int i = 0; i < tasks.Length; ++i)
            {
                if (monitor.Cancelator.IsCancellationRequested) throw new OperationCanceledException();

                var task = tasks[i];

                var evaluator = PipelineEvaluator.CreatePipelineInstance(task.Pipeline, srcDoc.GetTemplate, plugins.CreateNodeInstance, monitor.CreatePart(i, tasks.Length));
                evaluator.Setup(bsettings);

                var srcData = evaluator.Evaluate();
                if (srcData is Exception) { throw new InvalidOperationException("Failed processing " + task.Title, (Exception)srcData); }
            }
        }

        private static bool _MonitorFunc(int progress, string text)
        {
            var line = String.Empty + progress + "% " + text;

            System.Diagnostics.Debug.WriteLine(line);
            System.Console.WriteLine(line);

            // http://stackoverflow.com/questions/10621287/c-sharp-how-to-receive-system-close-or-exit-events-in-a-commandline-application
            // https://social.msdn.microsoft.com/Forums/vstudio/en-US/707e9ae1-a53f-4918-8ac4-62a1eddb3c4a/detecting-console-application-exit-in-c?forum=csharpgeneral

            return false;
        }

        private static PluginManager _LoadPlugins(ProjectDOM.Project project, PathString prjDir)
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
    }
}


