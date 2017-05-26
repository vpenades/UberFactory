using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Epsylon.UberFactory
{
    public static partial class ProjectDOM
    {
        public static string GetDisplayName(Object o)
        {
            if (o == null) return null;
            if (o is Configuration) return ((Configuration)o).ConfigurationFullName;
            if (o is Node) return ((Node)o).ClassIdentifier + " " + ((Node)o).TemplateIdentifier;
            if (o is Task) return ((Task)o).Title;
            if (o is Template) return ((Template)o).Title;
            if (o is TemplateParameter) return ((TemplateParameter)o).BindingName;
            if (o is PluginReference) return ((PluginReference)o).RelativePath;

            throw new NotSupportedException();
        }

        public static Project CreateNewProject()
        {
            return new Project();
        }

        public static Project LoadProjectFrom(string filePath)
        {
            var body = System.IO.File.ReadAllText(filePath);
            
            return ParseProject(body);
        }

        public static Project ParseProject(string projectBody)
        {
            // this reinterprets empty text body projects as "new projects"
            // which allows to create an empty text file, rename it to "UberFactory" and open it "as is"
            if (string.IsNullOrWhiteSpace(projectBody)) return CreateNewProject();

            var root = System.Xml.Linq.XElement.Parse(projectBody);
            return _ParseProject(root);
        }

        private static Project _ParseProject(System.Xml.Linq.XElement root)
        {
            var ver = root.Attribute("Version").Value;

            if (!Version.TryParse(ver, out Version docVer)) throw new System.IO.FileLoadException();
            if (docVer > _CurrentVersion) throw new System.IO.FileLoadException("Document Version " + docVer + " not supported");
            
            return Unknown.ParseXml(root, _Factory) as Project;
        }

        private static ObjectBase _Factory(Unknown unk)
        {
            var name = unk.ClassName;

            if (name == "Content") name = "Task";

            if (name == typeof(Configuration).Name) return new Configuration(unk);
            if (name == typeof(Node).Name) return new Node(unk);
            if (name == typeof(Pipeline).Name) return new Pipeline(unk);
            if (name == typeof(Task).Name) return new Task(unk);
            if (name == typeof(TemplateParameter).Name) return new TemplateParameter(unk);
            if (name == typeof(Template).Name) return new Template(unk);
            if (name == typeof(PluginReference).Name) return new PluginReference(unk);
            if (name == typeof(Project).Name) return new Project(unk);

            return unk;
        }

        public static void BuildProject(Project srcDoc, BuildContext bsettings, PluginManager plugins, PipelineEvaluator.Monitor monitor)
        {
            var tasks = srcDoc
                .Items
                .OfType<Task>()
                .Where(item => item.Enabled)
                .ToArray();

            for (int i = 0; i < tasks.Length; ++i)
            {
                if (monitor.Cancelator.IsCancellationRequested) throw new OperationCanceledException();

                var task = tasks[i];

                var evaluator = PipelineEvaluator.CreatePipelineInstance(task.Pipeline, srcDoc.GetTemplate, plugins.CreateNodeInstance, monitor.CreatePart(i, tasks.Length));
                evaluator.Setup(bsettings);

                var srcData = evaluator.Evaluate();
                if (srcData is Exception) { throw new InvalidOperationException("Failed processing " + task.Title, (Exception)srcData); }
            }
        }
    }
}
