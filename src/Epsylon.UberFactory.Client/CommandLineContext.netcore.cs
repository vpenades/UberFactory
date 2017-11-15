using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory.Client
{
    partial class CommandLineContext
    {
        private static Microsoft.Extensions.Logging.ILoggerFactory _CreateLoggerFactory()
        {
            var loggerFactory = new Microsoft.Extensions.Logging.LoggerFactory();
            Microsoft.Extensions.Logging.ConsoleLoggerExtensions.AddConsole(loggerFactory);

            return loggerFactory;
        }

        private static Factory.Collection _LoadPluginsFunc(ProjectDOM.Project project, PathString prjDir)
        {
            LoadProjectAssemblies(project, prjDir);

            var plugins = new Factory.Collection();            

            return PluginLoader.Instance.GetPlugins().GetContentInfoCollection();
        }
    }
}
