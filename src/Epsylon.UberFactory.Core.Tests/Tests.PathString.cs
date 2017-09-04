using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberFactory
{
    [TestClass]
    public class PathStringTests
    {
        [TestMethod]
        public void AbsolutePathTests()
        {
            var p1 = new PathString("c:\\test\\cheetos");
            Assert.IsTrue(p1.IsAbsolute);

            var p2 = p1.MakeAbsolutePath("..\\subdir");
            Assert.IsTrue(p2.IsAbsolute);



        }

    }
}
