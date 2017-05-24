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
        public void TemplatePipelineTest()
        {
            var template = TestPipelinesFactory.CreateTemplate();            

            // create a template
            var pipeline = new ProjectDOM.Pipeline();

            // create root node and set it as root of the template
            var nodeId = pipeline.AddNode("TestFilter3");            
            pipeline.RootIdentifier = nodeId;

            var nodeProps = pipeline.GetNode(nodeId).GetPropertiesForConfiguration("Root");
            nodeProps.SetNodeIds("Template1", template.Identifier);
            nodeProps.SetValue("Value1", "7");

            var result = (int)_Evaluate(pipeline, "Root", id => template);

            Assert.AreEqual(19, result);
        }

        private static Object _Evaluate(ProjectDOM.Pipeline pipeline, string configuration, Func<Guid,ProjectDOM.Template> tfunc = null)
        {
            if (tfunc == null) tfunc = g => null;

            // create a pipeline evaluator

            var evaluator = PipelineEvaluator.CreatePipelineInstance(pipeline, tfunc, TestFiltersFactory.CreateInstance, PipelineEvaluator.Monitor.Empty);
            evaluator.Setup(BuildContext.Create(configuration, new PathString("")));

            // run evaluation

            // note we're using the secondary evaluator instead of the primary one, to prevent creating the target directory.
            return (int)evaluator.Evaluate(pipeline.RootIdentifier);
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



        /// <summary>
        /// Create a templated pipeline, with a root node and two leaf nodes
        /// </summary>        
        public static ProjectDOM.Template CreateTemplate()
        {
            // create a template
            var template = System.Activator.CreateInstance(typeof(ProjectDOM.Template),true) as ProjectDOM.Template;            
            var pipeline = template.Pipeline;

            // create root node and set it as root of the template
            var nodeId = pipeline.AddNode("TestFilter1");
            pipeline.RootIdentifier = nodeId;            

            template.AddNewParameter();
            template.AddNewParameter();

            var ppp = template.Parameters.ToArray();

            ppp[0].BindingName = "TemplateParam1";
            ppp[0].NodeId = nodeId;
            ppp[0].NodeProperty = "Value1";

            ppp[1].BindingName = "TemplateParam2";
            ppp[1].NodeId = nodeId;
            ppp[1].NodeProperty = "Value2";

            return template;            
        }
    }


    static class TestFiltersFactory
    {
        public static SDK.ContentFilter CreateInstance(string classId, BuildContext context)
        {
            var filters = new Type[]
            {
                typeof(TestFilter1),
                typeof(TestFilter2),
                typeof(TestFilter3)
            };

            var t = filters.FirstOrDefault(item => item.Name == classId);

            return t == null ? null : SDK.Create(t, context);
        }

        [SDK.ContentFilter(nameof(TestFilter1))]
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

        [SDK.ContentFilter(nameof(TestFilter2))]
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


        [SDK.ContentFilter(nameof(TestFilter3))]
        class TestFilter3 : SDK.ContentFilter<int>
        {
            [SDK.InputPipeline(nameof(Template1))]
            [SDK.InputMetaData("TemplateSignature",new string[] { "TemplateParam1", "TemplateParam2" })]
            public SDK.IPipelineInstance Template1 { get; set; }

            [SDK.InputValue(nameof(Value1))]
            public int Value1 { get; set; }

            protected override int Evaluate()
            {
                var ppp = Template1.Parameters;

                Assert.AreEqual("TemplateParam1", ppp[0]);
                Assert.AreEqual("TemplateParam2", ppp[1]);

                var t1val = (int)Template1.Evaluate(5, 7);                

                return Value1 + t1val;
            }
        }

    }


    
}
