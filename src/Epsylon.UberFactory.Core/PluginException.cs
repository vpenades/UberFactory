using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    public class PluginException : Exception
    {
        public PluginException(Exception ex) : base(string.Empty, ex) { }


        public static PluginException GetPluginException(Exception ex)
        {
            if (ex == null) return null;
            if (ex is PluginException) return ex as PluginException;
            return GetPluginException(ex.InnerException);
        }
    }
}
