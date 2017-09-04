using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Epsylon.UberFactory
{
    // https://www.visualstudio.com/en-us/docs/test/developer-testing/getting-started/getting-started-with-developer-testing

    [TestClass]
    public class ProjectDomTests
    {
        [TestMethod]
        public void DocumentFileLoadCheck()
        {
            Assert.IsNotNull(ProjectDOM.ParseProject(string.Empty),"Empty files must be enterpreted as new documents.");

            var currentDocumentBody  = "<Project xmlns=\"http://www.uberfactory.com\" Version=\""+ProjectDOM.CurrentVersion+"\"  > <Properties /> </Project>";
            var futureDocumentBody = "<Project xmlns=\"http://www.uberfactory.com\" Version=\"999.0\"> <Properties /> </Project>";

            Assert.IsNotNull(ProjectDOM.ParseProject(currentDocumentBody));

            Assert.ThrowsException<System.IO.FileLoadException> ( () => ProjectDOM.ParseProject(futureDocumentBody) );
            
        }

        [TestMethod]
        public void PropertyTests()
        {
            var pg = new ProjectDOM.PropertyGroup();

            // null key check
            Assert.ThrowsException<ArgumentNullException>(() => pg.SetValue(null, "X"));
            Assert.ThrowsException<ArgumentNullException>(() => pg.SetValue(string.Empty, "X"));
            Assert.ThrowsException<ArgumentNullException>(() => pg.SetValue("  ", "X"));
            
            Assert.IsFalse(pg.Contains("X"));
            Assert.IsTrue(pg.SetArray("X", "A", "B", "C"));
            Assert.IsFalse(pg.SetArray("X", "A", "B", "C"),"Failed to check nothing changed");            
            Assert.AreEqual(3, pg.GetArray("X").Length);
            CollectionAssert.AreEqual(pg.GetArray("X"), new string[] { "A", "B", "C" });
            Assert.IsTrue(pg.Contains("X"));

            Assert.IsTrue(pg.SetArray("X", "A"));
            Assert.IsFalse(pg.SetArray("X", "A"));
            Assert.AreEqual(1, pg.GetArray("X").Length);

            pg.Clear("X");
            Assert.IsFalse(pg.Contains("X"));
        }

        [TestMethod]
        public void NodeTests()
        {
            var n = ProjectDOM.Node.Create("SomeFilter");

            var pgroot = n.GetPropertiesForConfiguration("Root");
            var pgchld = n.GetPropertiesForConfiguration("Root","Child");

            // check property value inheritance
            pgroot.SetValue("X", "A"); Assert.AreEqual("A", pgchld.GetValue("X", null));    
            pgchld.SetValue("X", "B"); Assert.AreEqual("B", pgchld.GetValue("X", null));
            pgchld.Clear("X");         Assert.AreEqual("A", pgchld.GetValue("X", null));
        }
    }
}
