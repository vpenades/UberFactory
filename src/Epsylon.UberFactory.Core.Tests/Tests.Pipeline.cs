using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Epsylon.UberFactory
{
    [TestClass]
    public class PipelineTests
    {        
        [TestMethod]        
        public void BasicPipelineTest()
        {
            var pipeline = TestPipelinesFactory.CreateBasicPipeline();
            var result = (int)_Evaluate(pipeline, "Root");
            Assert.AreEqual(12, result);
        }

        [TestMethod]
        public void TreePipelineTest()
        {
            var pipeline = TestPipelinesFactory.CreateTreePipeline();
            var result = (int)_Evaluate(pipeline,"Root");
            Assert.AreEqual(24, result);
        }


        [TestMethod]
        public void ArithmeticPipelineTest()
        {
            var pipeline = TestPipelinesFactory.CreateArithmeticPipeline();
            var result = (int)_Evaluate(pipeline, "Root");
            Assert.AreEqual(10, result);
        }

        [TestMethod]
        public void RemoveIsolatedNodeTest()
        {
            var pipeline = TestPipelinesFactory.CreateArithmeticPipeline();

            var nodeCount = pipeline.Nodes.Count();

            var isolatedNodeId = pipeline.AddNode("TestFilter1");

            Assert.IsTrue(pipeline.Nodes.Select(item => item.Identifier).Contains(isolatedNodeId));

            pipeline.RemoveIsolatedNodes();

            Assert.IsFalse(pipeline.Nodes.Select(item => item.Identifier).Contains(isolatedNodeId));
            Assert.AreEqual(nodeCount, pipeline.Nodes.Count());
        }



        private static Object _Evaluate(ProjectDOM.Pipeline pipeline, string configuration)
        {
            // create a pipeline evaluator

            var evaluator = Evaluation.PipelineEvaluator.CreatePipelineInstance(pipeline, TestFiltersFactory.CreateInstance, null);
            evaluator.Setup(Evaluation.BuildContext.Create(configuration, new PathString("")));

            // run evaluation

            // note we're using the secondary evaluator instead of the primary one, to prevent creating the target directory.
            return (int)evaluator.EvaluateNode(Evaluation.MonitorContext.CreateNull(), pipeline.RootIdentifier);
        }
    }


    static class TestPipelinesFactory
    {
        /// <summary>
        /// Create a simple pipeline, with a single node that returns the sum of two values
        /// </summary>
        public static ProjectDOM.Pipeline CreateBasicPipeline()
        {
            // create a pipeline
            var pipeline = new ProjectDOM.Pipeline();

            // create a node and set it as root of the pipeline
            var nodeId = pipeline.AddNode("TestFilter1");
            pipeline.RootIdentifier = nodeId;

            // set node properties for "Root" Configuration            
            var nodeProps = pipeline.GetNode(nodeId).GetPropertiesForConfiguration("Root");
            nodeProps.SetValue("Value1", "5");
            nodeProps.SetValue("Value2", "7");

            return pipeline;
        }

        /// <summary>
        /// Create a complex pipeline, with a root node and two leaf nodes
        /// </summary>        
        public static ProjectDOM.Pipeline CreateTreePipeline()
        {
            // create a pipeline
            var pipeline = new ProjectDOM.Pipeline();

            // create root node and set it as root of the pipeline
            var nodeId = pipeline.AddNode("TestFilter2");
            pipeline.RootIdentifier = nodeId;

            // create leaf nodes
            var node2Id = pipeline.AddNode("TestFilter1");
            var node3Id = pipeline.AddNode("TestFilter1");

            // set leaf node properties
            var nodeProps = pipeline.GetNode(node2Id).GetPropertiesForConfiguration("Root");
            nodeProps.SetValue("Value1", "5");
            nodeProps.SetValue("Value2", "7");

            // set leaf node properties
            nodeProps = pipeline.GetNode(node3Id).GetPropertiesForConfiguration("Root");
            nodeProps.SetValue("Value1", "5");
            nodeProps.SetValue("Value2", "7");

            // set root node properties
            nodeProps = pipeline.GetNode(nodeId).GetPropertiesForConfiguration("Root");
            nodeProps.SetNodeIds("Value1", node2Id);
            nodeProps.SetNodeIds("Value2", node3Id);

            return pipeline;
        }        


        public static ProjectDOM.Pipeline CreateArithmeticPipeline()
        {
            // create a pipeline
            var pipeline = new ProjectDOM.Pipeline();            

            var value1Id = pipeline.AddNode(nameof(Epsylon.TestPlugins.AssignIntegerValue));
            pipeline.GetNode(value1Id).GetPropertiesForConfiguration("Root").SetValue("Value", "5");

            var value2Id = pipeline.AddNode(nameof(Epsylon.TestPlugins.AssignIntegerValue));
            pipeline.GetNode(value2Id).GetPropertiesForConfiguration("Root").SetValue("Value", "5");

            var rootId = pipeline.AddNode(nameof(Epsylon.TestPlugins.AddIntegerValues));            
            pipeline.GetNode(rootId).GetPropertiesForConfiguration("Root").SetNodeIds("Value1", value1Id);
            pipeline.GetNode(rootId).GetPropertiesForConfiguration("Root").SetNodeIds("Value2", value2Id);

            pipeline.RootIdentifier = rootId;

            return pipeline;
        }
    }


    static class TestFiltersFactory
    {
        public static SDK.ContentFilter CreateInstance(string classId)
        {
            var filters = new Type[]
            {
                typeof(TestFilter1),
                typeof(TestFilter2),
                typeof(Epsylon.TestPlugins.AssignIntegerValue),
                typeof(Epsylon.TestPlugins.AddIntegerValues),
                typeof(Epsylon.TestPlugins.SubstractIntegerValues),
                typeof(Epsylon.TestPlugins.MultiplyIntegerValues),
                typeof(Epsylon.TestPlugins.DivideIntegerValues),
            };

            var t = filters.FirstOrDefault(item => item.Name == classId);

            return t == null ? null : SDK.Create(t) as SDK.ContentFilter;
        }

        [SDK.ContentNode(nameof(TestFilter1))]
        class TestFilter1 : SDK.ContentFilter<int>
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
        class TestFilter2 : SDK.ContentFilter<int>
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

    }


    
}
