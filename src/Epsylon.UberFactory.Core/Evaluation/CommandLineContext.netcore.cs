using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory.Evaluation
{
    partial class CommandLineContext
    {
        private static Microsoft.Extensions.Logging.ILoggerFactory _CreateLoggerFactory()
        {
            var loggerFactory = new Microsoft.Extensions.Logging.LoggerFactory();
            Microsoft.Extensions.Logging.ConsoleLoggerExtensions.AddConsole(loggerFactory);

            return loggerFactory;
        }

        private static PluginManager _LoadPluginsFunc(ProjectDOM.Project project, PathString prjDir)
        {
            var assemblies = new HashSet<System.Reflection.Assembly>();

            assemblies.UnionWith(GetProjectAssemblies(project, prjDir));

            var plugins = new PluginManager();

            plugins.SetAssemblies(assemblies);

            return plugins;
        }
    }
}
