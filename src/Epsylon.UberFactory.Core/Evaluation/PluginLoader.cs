using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory.Evaluation
{
    

    public interface IPluginLoader
    {
        void UsePlugin(PathString pluginAbsPath);

        Assembly[] GetPlugins();
    }

    


}


