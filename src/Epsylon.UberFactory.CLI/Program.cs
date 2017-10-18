using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Diagnostics.Debugger.Launch();

            if (args.Contains("-?"))
            {
                System.Console.WriteLine($"Über Factory CLI. SDK Version:{SDK.InformationalVersion}");
                return;
            }

            Evaluation.CommandLineContext.Build(args);
        }        
    }    
}
