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
             _BuildDocument("TestFiles\\Test1.uberfactory");           

            // Assert.AreEqual("Root", result.ConfigurationJoined);            
        }

        private static void _BuildDocument(string docFileName)
        {
            docFileName = docFileName.GetAbsolutePath();

            Assert.IsFalse(string.IsNullOrWhiteSpace(docFileName));

            Client.CommandLineContext.Build("-SIMULATE", "-CFG:Root", docFileName);            

            
        }

        
    }
}
