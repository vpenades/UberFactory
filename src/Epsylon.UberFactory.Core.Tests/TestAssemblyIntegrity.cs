using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberFactory
{
    [TestClass]
    public class TestAssembly
    {
        // Microsoft uses LibCheck
        // https://stackoverflow.com/questions/2377855/tool-for-backwards-compatibility-for-the-c-net-api

        // dump public API of an assembly
        // https://code.msdn.microsoft.com/windowsdesktop/Code-Index-How-to-discover-98ba517b

        // https://blogs.endjin.com/2016/02/an-experiment-to-automatically-detect-api-breaking-changes-in-dot-net-assemblies-and-suggest-a-semantic-version-number/

        // Dot net API portability test
        // https://github.com/Microsoft/dotnet-apiport

        // test nuget vs current API, also. how to create a plugin system with NuGet
        // https://stackoverflow.com/a/39578135


        [TestMethod]
        public void TestAssemblyVersion()
        {
            var assembly_SDK = typeof(SDK.ContentFilter).Assembly;
            var assembly_Core = typeof(Evaluation.PipelineInstance).Assembly;

            Assert.IsTrue(assembly_SDK.Version() >= new Version(1, 0, 0));
            Assert.IsTrue(assembly_Core.Version() >= new Version(1, 0, 0));

            var infoVersion1 = assembly_SDK.InformationalVersion();
            var infoVersion2 = assembly_Core.InformationalVersion();
        }
    }
}
