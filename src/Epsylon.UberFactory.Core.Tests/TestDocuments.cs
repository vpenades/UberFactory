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
            Assert.ThrowsException<System.IO.FileLoadException>(() => _BuildDocument("Test.FutureDocument.uberfactory"));            

            // missing plugin            
            Assert.ThrowsException<InvalidOperationException>(() => _BuildDocument("Test.MissingPlugin.uberfactory"));

            // malformed documents
        }

        [TestMethod]
        public void CommandLineLoad()
        {
            var result = _BuildDocument("Test1.uberfactory");           

            Assert.AreEqual("Root", result.ConfigurationJoined);            
        }

        private static Evaluation.BuildContext _BuildDocument(string docFileName)
        {
            docFileName = _GetAbsolutePath(docFileName);

            Assert.IsFalse(string.IsNullOrWhiteSpace(docFileName));

            var results = Evaluation.CommandLineContext.Build("-SIMULATE", "-CFG:Root", docFileName).ToArray();

            Assert.AreEqual(1, results.Length);

            return results[0];
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
