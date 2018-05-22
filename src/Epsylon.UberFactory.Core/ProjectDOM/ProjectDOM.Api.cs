using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Epsylon.UberFactory
{
    public static partial class ProjectDOM
    {
        /// <summary>
        /// Notice that this value is different that Guid.EMPTY to prevent 
        /// </summary>
        public static readonly Guid RESETTODEFAULT = new Guid("{00000000-0000-0000-0000-000000000001}");

        public static string GetDisplayName(Object o)
        {
            if (o == null) return null;
            if (o is Configuration cfg) return cfg.ConfigurationFullName;
            if (o is Node node) return node.ClassIdentifier;
            if (o is Task task) return task.Title;
            if (o is Settings settings) return settings.ClassName;
            if (o is PluginReference plugref) return plugref.AssemblyPath;

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

        internal static ObjectBase _Factory(Unknown unk)
        {
            var name = unk.ClassName;

            if (name == "Content") name = "Task";

            if (name == typeof(Configuration).Name) return new Configuration(unk);
            if (name == typeof(Node).Name) return new Node(unk);
            if (name == typeof(Pipeline).Name) return new Pipeline(unk);
            if (name == typeof(Task).Name) return new Task(unk);            
            if (name == typeof(PluginReference).Name) return new PluginReference(unk);
            if (name == typeof(Project).Name) return new Project(unk);
            if (name == typeof(Settings).Name) return new Settings(unk);
            if (name == typeof(DocumentInfo).Name) return new DocumentInfo(unk);

            return unk;
        }

        public static void BuildProject(Project srcDoc, Evaluation.BuildContext bsettings, Func<String, SDK.ContentObject> filterFactory,  SDK.IMonitorContext monitor, Evaluation.PipelineClientState.Manager outState)
        {
            if (srcDoc == null) throw new ArgumentNullException(nameof(srcDoc));
            if (bsettings == null) throw new ArgumentNullException(nameof(bsettings));
            if (filterFactory == null) throw new ArgumentNullException(nameof(filterFactory));

            var tasks = srcDoc
                .Items
                .OfType<Task>()
                .Where(item => item.Enabled)
                .ToArray();            

            // Before running the tasks we have to ensure:
            // 1- BuildContext is valid
            // 2- there's enough space to write
            // 3- we're able to create instances of all the filters
            // 4- all the source files are available            

            _ValidateFactory(bsettings, filterFactory, tasks);

            

            for (int i = 0; i < tasks.Length; ++i)
            {
                if (monitor.IsCancelRequested) throw new OperationCanceledException();

                var watch = new System.Diagnostics.Stopwatch();

                watch.Start();

                var task = tasks[i];                

                var instance = Evaluation.PipelineInstance.CreatePipelineInstance(task.Pipeline, filterFactory, srcDoc.UseSettings);
                instance.Setup(bsettings);

                using (var evaluator = instance.CreateEvaluator(monitor.CreatePart(i, tasks.Length)))
                {
                    var result = evaluator.EvaluateRoot();
                    // if (result is Exception ex) throw new InvalidOperationException($"Failed processing {task.Title}", ex);

                    var fileReport = result.FileManager;

                    watch.Stop();

                    outState?.SetResults(task.Pipeline.RootIdentifier, result, watch.Elapsed, fileReport);
                }                    
            }

            
        }

        private static void _ValidateFactory(Evaluation.BuildContext bsettings, Func<string, SDK.ContentObject> filterFactory, Task[] tasks)
        {
            var classIds = tasks
                            .SelectMany(item => item.Pipeline.Nodes)
                            .Select(item => item.ClassIdentifier)                            
                            .Distinct();

            foreach (var cid in classIds)
            {
                try
                {
                    var instance = filterFactory(cid);
                    if (instance is Evaluation._UnknownNode) throw new NullReferenceException();
                    if (instance == null) throw new NullReferenceException();
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Unable to create {cid} instance.", nameof(filterFactory), ex);
                }
            }
        }


    }
}
