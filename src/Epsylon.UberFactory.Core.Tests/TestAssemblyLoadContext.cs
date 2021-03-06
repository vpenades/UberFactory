﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Epsylon.UberFactory
{
    [TestClass]
    public class TestAssemblyLoadContext
    {
        public static string TestPluginPath => @"Epsylon.UberPlugin.TestTypes\bin\Debug\netstandard2.0\Epsylon.UberPlugin.TestTypes.dll".GetAbsolutePath();

        public static Assembly[] LoadPlugins()
        {
            // bad image test
            Client.PluginLoader.Instance.UsePlugin(new PathString("TestFiles\\BadImage.dll".GetAbsolutePath()));

            // plugin test
            Client.PluginLoader.Instance.UsePlugin(new PathString(TestPluginPath));

            return Client.PluginLoader.Instance.GetPlugins();
        }        

        [TestMethod]
        public void LoadValidPlugin()
        {
            var assemblies = LoadPlugins();
            Assert.IsNotNull(assemblies);

            Assert.IsTrue(assemblies.Any(item => item.Location == TestPluginPath));

            // Assert.AreEqual(assembly.Location, TestPluginPath);

            // Assert.AreEqual(typeof(SDK.ContentFilter).Assembly.GetDirectory(), ""); // check if SDK has been loaded from the entry point path

            // var obj = assembly.CreateInstance("Epsylon.TestPlugins.DependencyInitializer");
            // Assert.IsNotNull(obj);
        }
    }
}
