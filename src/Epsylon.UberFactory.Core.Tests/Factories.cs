using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    public static class TestFiltersFactory
    {
        static System.Reflection.Assembly[] _Plugins;

        private static System.Reflection.Assembly[] _CreatePlugins()
        {
            var plugins = TestAssemblyLoadContext.LoadPlugins().ToList();
            plugins.Add(typeof(TestFiltersFactory).Assembly);
            return plugins.ToArray();
        }

        public static SDK.ContentFilter CreateInstance(string classId)
        {
            if (_Plugins == null) _Plugins = _CreatePlugins();

            var typeCollection = _Plugins.GetContentInfoCollection();

            return typeCollection.CreateInstance(classId) as SDK.ContentFilter;
        }

        

        [SDK.ContentNode(nameof(TestFilter1))]
        public class TestFilter1 : SDK.ContentFilter<int>
        {
            [SDK.InputValue("Value1")]
            public int Value1 { get; set; }

            [SDK.InputValue("Value2")]
            public int Value2 { get; set; }

            protected override int Evaluate()
            {
                return Value1 + Value2;
            }
        }

        [SDK.ContentNode(nameof(TestFilter2))]
        public class TestFilter2 : SDK.ContentFilter<int>
        {
            [SDK.InputNode("Value1")]
            public int Value1 { get; set; }

            [SDK.InputNode("Value2")]
            public int Value2 { get; set; }

            protected override int Evaluate()
            {
                return Value1 + Value2;
            }
        }

        [SDK.ContentNode(nameof(TestFilter2))]
        public class TestFilter3 : SDK.ContentFilter<int>
        {
            // values from multiple nodes
            [SDK.InputNode("Values1",true)]
            public int[] Values1 { get; set; }

            // direct values
            [SDK.InputValue("Values2")]
            public int[] Values2 { get; set; }

            protected override int Evaluate()
            {
                return Values1.Sum() + Values2.Sum();
            }
        }


        [SDK.ContentNode(nameof(TestFilter2))]
        public class InvalidTestFilter4 : SDK.ContentFilter<int>
        {
            // values from multiple nodes
            [SDK.InputNode("Values1", true)]
            public int[] Values1 { get; set; }

            // direct values
            [SDK.InputValue("Values2")]
            public int[][] Values2 { get; set; }

            protected override int Evaluate()
            {
                return 0;
            }
        }

    }
}
