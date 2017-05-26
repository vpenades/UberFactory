using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Epsylon.UberFactory
{
    // deberia estar usando : https://github.com/tgjones/gemini + http://documentup.com/tgjones/gemini

    public static partial class ProjectVIEW
    {

        public static Project CreateFromCommandLine(AppView app, string[] args)
        {
            if (app == null) return null;

            if (args.Length == 0) return null;

            var docPath = new PathString(args[0]);
            if (!docPath.FileExists) return null;
            if (!docPath.HasExtension("uberfactory")) return null;

            var prj = ProjectDOM.LoadProjectFrom(docPath);            

            var prjv = Project.Create(app, prj, docPath);

            // var outDir = args.FirstOrDefault(item => item.StartsWith("-OutDir:"));
            // if (outDir != null) outDir = outDir.Substring(8).Trim('"');
            // prjv.TargetDirectory = outDir;

            return prjv;
            
        }

        public static Project CreateNew(AppView app)
        {
            if (app == null) return null;

            var dlg = new Microsoft.Win32.SaveFileDialog()
            {
                RestoreDirectory = true,
                Filter = "Über Project File|*.uberfactory"
            };

            if (!dlg.ShowDialog().Value) return null;            

            return Project.CreateNew(app, new PathString(dlg.FileName));
        }

        public static Project OpenFile(AppView app, PathString filePath)
        {
            if (app == null) return null;
            if (!filePath.FileExists) return null;

            var prj = ProjectDOM.LoadProjectFrom(filePath);

            return Project.Create(app, prj, filePath);
        }


        public static string GetDisplayName(Object o)
        {
            if (o == null) return null;
            if (o is Task) return ProjectDOM.GetDisplayName(((Task)o).Source);
            if (o is Template) return ProjectDOM.GetDisplayName(((Template)o).Source);


            throw new NotSupportedException();
        }


                


        public class Project : BindableBase
        {
            #region lifecycle

            public static Project CreateNew(AppView app, PathString docPath)
            {
                if (docPath == null) return null;

                if (!System.IO.Path.IsPathRooted(docPath)) return null;

                var doc = ProjectDOM.CreateNewProject();

                var ppp = new Project(app, doc, docPath);

                ppp._ReloadPlugins();

                return ppp;
            }

            public static Project Create(AppView app, ProjectDOM.Project doc, PathString docPath)
            {
                if (doc == null || docPath == null) return null;

                var ppp = new Project(app, doc, docPath);

                ppp._ReloadPlugins();

                return ppp;
            }

            private Project(AppView app, ProjectDOM.Project doc, PathString documentPath)
            {
                _Application = app;

                _Source = doc;
                _SourceBody = doc.GetBody();

                _DocumentPath = documentPath;

                _Configurations = new Configurations(doc);
                _ActiveConfiguration = _Configurations.RootConfiguration;

                SaveCmd = new RelayCommand(Save);
                AddTaskCmd = new RelayCommand(_AddNewTask);
                AddTemplateCmd = new RelayCommand(_AddNewTemplate);
                EditPluginsCmd = new RelayCommand(_EditPlugin);
                EditConfigurationsCmd = new RelayCommand(_EditConfigurations);               

                DeleteActiveDocumentCmd = new RelayCommand<BindableBase>(_DeleteItem);

                BuildAllCmd = new RelayCommand(Build);                
            }

            #endregion

            #region cmds

            public ICommand SaveCmd { get; private set; }

            public ICommand AddTaskCmd { get; private set; }

            public ICommand AddTemplateCmd { get; private set; }

            public ICommand EditPluginsCmd { get; private set; }

            public ICommand EditConfigurationsCmd { get; private set; }

            public ICommand DeleteActiveDocumentCmd { get; private set; }


            public ICommand BuildAllCmd { get; private set; }

            #endregion

            #region serialization

            public void Save()
            {
                _Source.SaveTo(_DocumentPath);

                _SourceBody = _Source.GetBody();
            }

            #endregion

            #region data

            private readonly AppView _Application;

            private readonly ProjectDOM.Project _Source;
            private String                      _SourceBody;    // used to check if the project is dirty

            private readonly PathString _DocumentPath;

            private readonly Configurations _Configurations;
            internal readonly PluginManager _Plugins = new PluginManager();

            private readonly EventLoggerProvider _Logger = new EventLoggerProvider();

            private BindableBase _ActiveItemView;
            private String _ActiveConfiguration;

            #endregion

            #region properties

            public AppView Application          => _Application;

            public String Title                 => this.GetType().Assembly.GetDisplayTitle(true, true, DisplayName);

            public String DocumentPath          => _DocumentPath;

            public Boolean IsDirty              => _SourceBody != _Source.GetBody();

            public String DisplayName           => _DocumentPath.FileNameWithoutExtension;

            public String SourceDirectory       => _DocumentPath.DirectoryPath;

            public Configurations Configurations => _Configurations;

            public Boolean IsRootConfiguration  => _ActiveConfiguration == _Configurations.RootConfiguration;

            public String ActiveConfiguration
            {
                get { return _ActiveConfiguration; }
                set
                {
                    if (_ActiveConfiguration == value) return;
                    _ActiveConfiguration = value;
                    RaiseChanged(nameof(ActiveConfiguration), nameof(Tasks), nameof(Templates), nameof(IsRootConfiguration));
                    ActiveDocument = null; // we must flush active document because it might be in the wrong configuration
                }
            }

            public IEnumerable<Task> Tasks
            {
                get
                {
                    if (_ActiveConfiguration == null) return null;

                    return _Source
                        .Items
                        .OfType<ProjectDOM.Task>()
                        .Select(item => ProjectVIEW.Task.Create(this, item))
                        .ExceptNulls()
                        .ToArray();
                }
            }

            public IEnumerable<Template> Templates
            {
                get
                {
                    if (_ActiveConfiguration == null) return null;

                    return _Source
                        .Items
                        .OfType<ProjectDOM.Template>()
                        .Select(item => ProjectVIEW.Template.Create(this, item))
                        .ExceptNulls()
                        .ToArray();
                }
            }            

            public BindableBase ActiveDocument
            {
                get { return _ActiveItemView; }
                set { if (value == _ActiveItemView) return; _ActiveItemView = value;RaiseChanged(nameof(ActiveDocument)); }
            }

            public IEnumerable<PathString> AbsoluteFilePaths => _Source.References.Select(item => _DocumentPath.DirectoryPath.MakeAbsolutePath(item));

            public String TargetDirectory       => GetBuildSettings().TargetDirectory;

            public bool CanAddItems
            {
                get
                {
                    if (_ActiveConfiguration == null) return false;
                    return true;
                }
            }

            public bool CanBuild
            {
                get
                {
                    if (_ActiveConfiguration == null) return false;
                    if (!_Source.Items.OfType<ProjectDOM.Task>().Any()) return false;
                    return true;
                }
            }


            #endregion

            #region API

            public BuildContext GetBuildSettings()
            {
                var cfg = _ActiveConfiguration;

                if (string.IsNullOrWhiteSpace(cfg)) cfg = _Configurations.RootConfiguration;

                return BuildContext.Create(cfg, _DocumentPath.DirectoryPath);
            }

            private void _EditPlugin()
            {
                _Dialogs.ShowPluginsManagerDialog
                    (
                    _Application,
                    path => _Source.ContainsReference(_DocumentPath.DirectoryPath.MakeRelativePath(path)),
                    path => _Source.InsertReference(_DocumentPath.DirectoryPath.MakeRelativePath(path)),
                    path => _Source.RemoveReference(_DocumentPath.DirectoryPath.MakeRelativePath(path))
                    );
                _ReloadPlugins();
            }

            private void _EditConfigurations()
            {
                bool showConfigDlg = true;

                if (!this.Configurations.HasRoot)
                {
                    var r = System.Windows.MessageBox.Show(System.Windows.Application.Current.MainWindow, "Create Default Configuration?", "Configurations", System.Windows.MessageBoxButton.YesNoCancel);

                    if (r == System.Windows.MessageBoxResult.Cancel) return;
                    if (r == System.Windows.MessageBoxResult.Yes)
                    {
                        this.Configurations.AddConfig("Root");
                        showConfigDlg = false;
                    }
                }

                if (showConfigDlg)
                {
                    _Dialogs.ShowGenericDialog<Themes.ConfigurationsEditPanel>(null, "Configuration Manager", this.Configurations);
                    RaiseChanged(nameof(Configurations), nameof(IsDirty));
                }

                if (ActiveConfiguration == null) ActiveConfiguration = this.Configurations.RootConfiguration;
                RaiseChanged(nameof(CanBuild),nameof(CanAddItems));
            }

            private void _DeleteItem(BindableBase item)
            {
                if (item == null) return;                

                if (!_Dialogs.QueryDeletePermanentlyWarningDialog( GetDisplayName(item) )) return;

                if (item is Task) { _Source.RemoveItem(((Task)item).Source); RaiseChanged(nameof(Tasks), nameof(IsDirty)); }
                if (item is Template) { _Source.RemoveItem(((Template)item).Source); RaiseChanged(nameof(Templates), nameof(IsDirty)); }

                if (ActiveDocument == item) ActiveDocument = null;

                RaiseChanged(nameof(CanBuild));
            }

            private void _AddNewTask()
            {
                if (_ActiveConfiguration == null || _Configurations.IsEmpty) { _EditConfigurations(); return; }

                _Source.AddTask().Title = "New Task " + Tasks.Count(); RaiseChanged(nameof(Tasks), nameof(IsDirty));

                RaiseChanged(nameof(CanBuild));
            }

            private void _AddNewTemplate()
            {
                if (_ActiveConfiguration == null || _Configurations.IsEmpty) { _EditConfigurations(); return; }

                _Source.AddTemplate().Title = "New Template " + Tasks.Count(); RaiseChanged(nameof(Templates), nameof(IsDirty));
            }

            public void Build()
            {
                if (!CanBuild) { _Dialogs.ShowErrorDialog("Can't build"); return; }                

                var bs = GetBuildSettings();

                // bs = SDK.BuildSettings.Create(bs, bs.SourceDirectory + "\\bin\\debug\\");

                while (!bs.CanBuild)
                {
                    var vsview = new BuildSettings(bs);

                    var r = _Dialogs.ShowGenericDialog<Themes.BuildSettingsPanel>(null, "Build Settings", vsview);
                    if (!r) return;

                    bs = vsview.GetBuildSettings();
                }

                Action<System.Threading.CancellationToken, IProgress<float>> buildTask = (ctoken, progress) =>
                {
                    using (var xlogger = new Microsoft.Extensions.Logging.LoggerFactory())
                    {
                        xlogger.AddProvider(_Logger);

                        bs.SetLogger(xlogger);

                        var monitor = PipelineEvaluator.Monitor.Create(ctoken, progress);
                        CommandLineContext.BuildProject(_Source, bs, _Plugins, monitor);
                    }
                };

                try
                {
                    buildTask.RunWithDialog();
                }                
                catch(Exception ex)
                {
                    if (ex is TaskCanceledException || ex is OperationCanceledException)
                    {
                        System.Windows.MessageBox.Show(System.Windows.Application.Current.MainWindow, "Build Cancelled");
                        return;
                    }

                    if (PluginException.GetPluginException(ex) != null) ex = PluginException.GetPluginException(ex);

                    _Dialogs.ShowProductAndDispose(null, ex);

                    // System.Windows.MessageBox.Show(System.Windows.Application.Current.MainWindow, "Build Error");
                }
            }            

            private void _ReloadPlugins()
            {
                var paths = _Source
                    .References
                    .Select(rp => System.IO.Path.Combine(SourceDirectory, rp))
                    .Distinct(StringComparer.InvariantCultureIgnoreCase)
                    .Select(item => new PathString(item))
                    .Where(item => item.IsValidAbsoluteFilePath)
                    .ToArray();

                var assemblies = PluginLoader.Instance.UsePlugins(paths);
                
                _Plugins.SetAssemblies(assemblies);

                RaiseChanged();
            }

            #endregion            
        }

        

        public class Configurations : BindableBase
        {
            #region lifecycle

            internal Configurations(ProjectDOM.Project prj)
            {
                var taskQuery = prj
                    .Items
                    .OfType<ProjectDOM.Task>()
                    .SelectMany(item => item.Pipeline.Nodes)
                    .SelectMany(item => item.AllConfigurations);

                var templateQuery = prj
                    .Items
                    .OfType<ProjectDOM.Template>()
                    .SelectMany(item => item.Pipeline.Nodes)
                    .SelectMany(item => item.AllConfigurations);

                var query = taskQuery.Concat(templateQuery);

                _Configurations.UnionWith(query);                
            }

            #endregion

            #region data

            private readonly HashSet<String> _Configurations = new HashSet<string>();

            #endregion

            #region properties

            public bool IsEmpty => _Configurations.Count == 0;

            public bool HasRoot => _Configurations.Count > 0;

            public IEnumerable<string> All  => _Configurations.OrderBy(item=>item).ToArray();

            public String RootConfiguration => _Configurations.OrderBy(item => item.Length).FirstOrDefault();            

            #endregion

            #region API

            public void AddConfig(string cfg)
            {
                _Configurations.Add(cfg);
                RaiseChanged();
            }

            #endregion

        }


        

        public class BuildSettings : BindableBase
        {
            #region lifecycle

            public BuildSettings(BuildContext bs)
            {                
                _Configuration = string.Join(BuildContext.ConfigurationSeparator.ToString(), bs.Configuration);
                _SourceDirectory = bs.SourceDirectory;
                _TargetDirectory = bs.TargetDirectory;

                BrowseTargetDirectoryCmd = new RelayCommand(BrowseTargetDirectory);
            }

            #endregion

            #region data

            private string _Configuration;
            private PathString _SourceDirectory;
            private PathString _TargetDirectory;

            #endregion

            #region properties

            [System.ComponentModel.DisplayName("Browse Target Directory...")]
            public ICommand BrowseTargetDirectoryCmd { get; private set; }

            public String Configuration => _Configuration;

            public String SourceDirectory => _SourceDirectory;
            public String TargetDirectory => _TargetDirectory;

            public String TargetDirectoryShortestDisplay
            {
                get
                {
                    var paths = new List<String>();

                    if (!string.IsNullOrWhiteSpace(_TargetDirectory))
                    {
                        paths.Add(_TargetDirectory);
                        if (!string.IsNullOrWhiteSpace(_SourceDirectory)) paths.Add(_SourceDirectory.MakeRelativePath(_TargetDirectory));
                    }

                    return paths.OrderBy(item => item.Length).FirstOrDefault();
                }
            }

            #endregion

            #region API

            public void BrowseTargetDirectory()
            {
                var dirPath = _Dialogs.ShowBrowseDirectoryDialog(PathString.Empty);
                if (dirPath.IsEmpty) return;

                _TargetDirectory = dirPath;

                RaiseChanged(nameof(TargetDirectory), nameof(TargetDirectoryShortestDisplay));
            }

            public BuildContext GetBuildSettings()
            {
                return BuildContext.Create(_Configuration, _SourceDirectory, _TargetDirectory);
            }

            #endregion
        }
    }
}
