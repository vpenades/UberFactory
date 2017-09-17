﻿using System;
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

        private static Factory.Collection _LoadPluginsFunc(ProjectDOM.Project project, PathString prjDir)
        {
            LoadProjectAssemblies(project, prjDir);

            if (true) // load locally referenced assemblies
            {
                var entry = AssemblyServices.GetEntryAssembly();

                if (entry != null)
                {
                    var arefs = entry.GetReferencedAssemblies();
                    foreach (var aname in arefs)
                    {
                        if (string.IsNullOrWhiteSpace(aname.CodeBase)) continue;

                        PluginLoader.Instance.UsePlugin(new PathString(aname.CodeBase));
                    }
                }
            }

            var plugins = new Factory.Collection();

            plugins.SetAssemblies(PluginLoader.Instance.GetPlugins());

            return plugins;
        }

        
    }
}
