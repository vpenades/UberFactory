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
            // return;

            var results = Evaluation.CommandLineContext.Build("-SIMULATE", "-CFG:Root", "Test1.uberfactory").ToArray();

            Assert.AreEqual(1, results.Length);

            var r = results[0];

            Assert.AreEqual("Root", r.ConfigurationJoined);
        }
    }
}
