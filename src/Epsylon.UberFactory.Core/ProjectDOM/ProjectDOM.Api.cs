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
            if (name == typeof(Settings).Name) return new Settings(unk);

            return unk;
        }

        public static void BuildProject(Project srcDoc, Evaluation.BuildContext bsettings, Func<String, SDK.ContentObject> filterFactory,  SDK.IMonitorContext monitor)
        {
            if (srcDoc == null) throw new ArgumentNullException(nameof(srcDoc));
            if (bsettings == null) throw new ArgumentNullException(nameof(bsettings));
            if (filterFactory == null) throw new ArgumentNullException(nameof(filterFactory));

            var tasks = srcDoc
                .Items
                .OfType<Task>()
                .Where(item => item.Enabled)
                .ToArray();

            var templates = srcDoc
                .Items
                .OfType<Template>()
                .ToArray();

            // Before running the tasks we have to ensure:
            // 1- BuildContext is valid
            // 2- there's enough space to write
            // 3- we're able to create instances of all the filters
            // 4- all the source files are available            

            _ValidateFactory(bsettings, filterFactory, tasks, templates);            

            for (int i = 0; i < tasks.Length; ++i)
            {
                if (monitor.IsCancelRequested) throw new OperationCanceledException();

                var task = tasks[i];

                var evaluator = Evaluation.PipelineEvaluator.CreatePipelineInstance(task.Pipeline, filterFactory, srcDoc.UseSettings, srcDoc.GetTemplate);
                evaluator.Setup(bsettings);

                var srcData = evaluator.Evaluate(monitor.GetProgressPart(i, tasks.Length));
                if (srcData is Exception) { throw new InvalidOperationException("Failed processing " + task.Title, (Exception)srcData); }
            }
        }

        private static void _ValidateFactory(Evaluation.BuildContext bsettings, Func<string, SDK.ContentObject> filterFactory, Task[] tasks, Template[] templates)
        {
            var classIds = tasks
                            .SelectMany(item => item.Pipeline.Nodes)
                            .Select(item => item.ClassIdentifier)
                            .Concat
                            (
                                templates
                                .SelectMany(item => item.Pipeline.Nodes)
                                .Select(item => item.ClassIdentifier)
                            )
                            .Distinct();

            foreach (var cid in classIds)
            {
                try
                {
                    var instance = filterFactory(cid);
                    if (instance == null) throw new NullReferenceException();
                }
                catch (Exception ex)
                {
                    throw new ArgumentException("Unable to create " + cid + " instance.", nameof(filterFactory), ex);
                }
            }
        }


    }
}
