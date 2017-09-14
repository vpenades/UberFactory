using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberFactory
{
    [TestClass]
    public class TestAssembly
    {
        // dump public API of an assembly
        // https://code.msdn.microsoft.com/windowsdesktop/Code-Index-How-to-discover-98ba517b

        // https://blogs.endjin.com/2016/02/an-experiment-to-automatically-detect-api-breaking-changes-in-dot-net-assemblies-and-suggest-a-semantic-version-number/

        // Dot net API portability test
        // https://github.com/Microsoft/dotnet-apiport

        [TestMethod]
        public void TestAssemblyVersion()
        {
            var SDK_Assembly = typeof(SDK.ContentFilter).Assembly;

            Assert.IsTrue(SDK_Assembly.Version() >= new Version(1, 0, 0));

            var infoVersion = SDK_Assembly.InformationalVersion();
        }
    }
}
