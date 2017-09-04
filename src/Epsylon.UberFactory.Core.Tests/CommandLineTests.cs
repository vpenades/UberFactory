using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Epsylon.UberFactory
{
    [TestClass]
    public class CommandLineTests
    {

        [TestMethod]
        public void CommandLineLoad()
        {            
            var docPath = _GetAbsolutePath("Test1.uberfactory");

            var results = Evaluation.CommandLineContext.Build("-SIMULATE", "-CFG:Root", docPath).ToArray();

            Assert.AreEqual(1, results.Length);

            var r = results[0];

            Assert.AreEqual("Root", r.ConfigurationJoined);            
        }

        private static string _GetAbsolutePath(string documentName)
        {
            var probeDir = Environment.CurrentDirectory;

            while(probeDir.Length > 3)
            {
                var absPath = System.IO.Path.Combine(probeDir, documentName);
                if (System.IO.File.Exists(absPath)) return absPath;

                probeDir = System.IO.Path.GetDirectoryName(probeDir);
            }

            return null;
        }
    }
}
