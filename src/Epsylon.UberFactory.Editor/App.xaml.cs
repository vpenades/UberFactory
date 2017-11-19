using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Epsylon.UberFactory
{    
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            RecentFilesManager.UseXmlPersister();

            base.OnStartup(e);            
        }
    }
}
