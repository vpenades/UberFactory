﻿using System;
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
            throw new PlatformNotSupportedException();
        }

        private static Factory.Collection _LoadPluginsFunc(ProjectDOM.Project project, PathString prjDir)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
