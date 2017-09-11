using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Epsylon.UberFactory
{
    static class _Extensions
    {
        public static Guid AddRootNode(this ProjectDOM.Pipeline pipeline, string className)
        {
            var id = pipeline.AddNode(className);

            pipeline.RootIdentifier = id;

            return id;
        }

        public static ProjectDOM.Node CreateNode(this ProjectDOM.Pipeline pipeline,string className)
        {
            var id = pipeline.AddNode(className);

            return pipeline.GetNode(id);
        }

        public static ProjectDOM.Node CreateRootNode(this ProjectDOM.Pipeline pipeline, string className)
        {
            var id = pipeline.AddRootNode(className);

            return pipeline.GetNode(id);
        }

        public static void SetReferences(this ProjectDOM.Node parent, string jointCfg, string propertyName, params ProjectDOM.Node[] children)
        {
            var ids = children
                .Select(item => item.Identifier)
                .ToArray();

            var props = parent.GetPropertiesForConfiguration(jointCfg.Split('.'));
            props.SetReferenceIds("Value1", ids);
        }

        public static IEnumerable<Guid> GetReferenceIds(this ProjectDOM.Node parent, string jointCfg, string propertyName)
        {
            var props = parent.GetPropertiesForConfiguration(jointCfg.Split('.'));

            return props.GetReferenceIds(propertyName);
        }

        public static IEnumerable<ProjectDOM.Node> GetReferences(this ProjectDOM.Pipeline pipeline, ProjectDOM.Node parent, string jointCfg, string propertyName)
        {
            var ids = parent.GetReferenceIds(jointCfg, propertyName);

            return ids.Select(item => pipeline.GetNode(item));
        }

    }
}
