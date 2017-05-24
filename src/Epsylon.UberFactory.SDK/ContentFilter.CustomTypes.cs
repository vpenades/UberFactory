using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    partial class SDK
    {
        public interface IPipelineInstance
        {
            string[] Parameters { get; }

            Object Evaluate(params Object[] parameters);            
        }        
    }

}
