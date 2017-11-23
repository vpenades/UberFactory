using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Epsylon.UberFactory
{
    [TestClass]
    public class TestDocuments
    {

        [TestMethod]
        public void TestInvalidDocuments()
        {
            // future documents           
            Assert.ThrowsException<System.IO.FileLoadException>(() => _BuildDocument("TestFiles\\Test.FutureDocument.uberfactory"));            

            // missing plugin            
            Assert.ThrowsException<ArgumentException>(() => _BuildDocument("TestFiles\\Test.MissingPlugin.uberfactory"));            

            // malformed documents
        }

        [TestMethod]
        public void CommandLineLoad()
        {
            var result = _BuildDocument("TestFiles\\Test1.uberfactory");           

            Assert.AreEqual("Root", result.ConfigurationJoined);            
        }

        private static Evaluation.BuildContext _BuildDocument(string docFileName)
        {
            docFileName = docFileName.GetAbsolutePath();

            Assert.IsFalse(string.IsNullOrWhiteSpace(docFileName));

            var results = Client.CommandLineContext.Build("-SIMULATE", "-CFG:Root", docFileName).ToArray();

            Assert.AreEqual(1, results.Length);

            return results[0];
        }

        
    }
}
