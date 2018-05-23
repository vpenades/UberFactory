using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Epsylon.UberFactory
{
    public static partial class ProjectVIEW
    {
        public static Project TryCreateFromCommandLine(AppView app)
        {
            var docPath = _AppConstants.StartupOpenDocumentPath;

            if (!docPath.HasValue) return null;

            var prj = ProjectDOM.LoadProjectFrom(docPath.Value);

            var prjv = Project.Create(app, prj, docPath.Value);

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

            try
            {
                var prj = ProjectDOM.LoadProjectFrom(filePath);

                return Project.Create(app, prj, filePath);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return null;
            }
        }

        public static string GetDisplayName(Object o)
        {
            if (o == null) return null;
            if (o is Task task) return ProjectDOM.GetDisplayName(task.Source);
            if (o is SettingsView settings) return ProjectDOM.GetDisplayName(settings.Source);

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

                EditPluginsCmd = new RelayCommand(_EditPlugin);
                EditConfigurationsCmd = new RelayCommand(_EditConfigurations);               

                DeleteActiveDocumentCmd = new RelayCommand<BindableBase>(_DeleteTask);                

                CopyTaskToClipboardCmd = new RelayCommand<BindableBase>(_CopyTaskToClipboard);
                PasteTaskFromClipboardCmd = new RelayCommand(_PasteTaskFromClipboard);

                BuildAllCmd = new RelayCommand(Build);
                TestAllCmd = new RelayCommand(Test);

                

                ShowSourceDirectoryCmd = new RelayCommand(() => new PathString(this.SourceDirectory).TryOpenContainingFolder());
                ShowTargetDirectoryCmd = new RelayCommand(() => new PathString(this.TargetDirectory).TryOpenContainingFolder());
            }

            #endregion

            #region cmds

            public ICommand EditPluginsCmd { get; private set; }

            public ICommand EditConfigurationsCmd { get; private set; }

            public ICommand TestAllCmd { get; private set; }

            public ICommand BuildAllCmd { get; private set; }

            public ICommand ShowSourceDirectoryCmd { get; private set; }

            public ICommand ShowTargetDirectoryCmd { get; private set; }

            public ICommand SaveCmd { get; private set; }

            public ICommand AddTaskCmd { get; private set; }

            public ICommand DeleteActiveDocumentCmd { get; private set; }

            public ICommand CopyTaskToClipboardCmd { get; private set; }

            public ICommand PasteTaskFromClipboardCmd { get; private set; }

            #endregion

            #region serialization

            public void Save()
            {
                try
                {
                    _Source.SaveTo(_DocumentPath);
                }
                catch(Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                

                // update dirty
                _SourceBody = _Source.GetBody();
            }

            #endregion

            #region data

            private readonly AppView _Application;

            private readonly ProjectDOM.Project _Source;
            private String                      _SourceBody;    // used to check if the project is dirty

            private readonly PathString _DocumentPath;

            private readonly Configurations _Configurations;
            internal readonly Factory.Collection _Plugins = new Factory.Collection();            

            private BindableBase _ActiveItemView;
            private String _ActiveConfiguration;
            
            private Evaluation.PipelineClientState.Manager _ProjectState = new Evaluation.PipelineClientState.Manager();

            #endregion

            #region properties

            public AppView Application          => _Application;

            public String Title                 => this.GetType().Assembly.GetDisplayTitle(true, true, DisplayName);

            public String DocumentPath          => _DocumentPath;

            public Boolean IsDirty              => _SourceBody != _Source.GetBody();

            public String DisplayName           => _DocumentPath.FileNameWithoutExtension;

            public String SourceDirectory       => _DocumentPath.DirectoryPath;

            public Boolean IsReadOnly           => _DocumentPath.IsReadOnly;

            public Configurations Configurations => _Configurations;

            public Boolean IsRootConfiguration  => _ActiveConfiguration == _Configurations.RootConfiguration;

            public String TargetDirectory       => GetBuildSettings().TargetDirectory;

            public String ActiveConfiguration { get { return _ActiveConfiguration; } set { _SetActiveConfiguration(value); } }

            public IEnumerable<SettingsView> SharedSettings
            {
                get
                {
                    if (_ActiveConfiguration == null) return null;

                    _UpdatePipelineState();

                    return this._Plugins
                        .SettingsClassIds
                        .Select(clsid => _Source.UseSettings(clsid))
                        .Select(item => SettingsView.Create(this, item))
                        .ExceptNulls()
                        .ToArray();
                }
            }

            public IEnumerable<Task> Tasks
            {
                get
                {
                    if (_ActiveConfiguration == null) return null;

                    _UpdatePipelineState();

                    return _Source
                        .Items
                        .OfType<ProjectDOM.Task>()
                        .Select(item => Task.Create(this, item))
                        .ExceptNulls()
                        .ToArray();
                }
            }

            public BindableBase ActiveDocument
            {
                get { return _ActiveItemView; }
                set
                {
                    if (value == _ActiveItemView) return;
                    _ActiveItemView = value;
                    RaiseChanged(nameof(ActiveDocument),nameof(ActiveDocumentCanBeRemoved));
                }
            }

            public Boolean ActiveDocumentCanBeRemoved
            {
                get
                {
                    if (ActiveDocument is Task task) return _Source.Items.Contains(task.Source);
                    return false;
                }
            }

            public IEnumerable<PathString> AbsoluteFilePaths => _Source.References.Select(item => _DocumentPath.DirectoryPath.MakeAbsolutePath(item));

            public bool CanAddItems => _ActiveConfiguration != null;

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

            public Evaluation.BuildContext GetBuildSettings(bool isSimulation=false)
            {
                var cfg = _ActiveConfiguration;

                if (string.IsNullOrWhiteSpace(cfg)) cfg = _Configurations.RootConfiguration;                

                return Evaluation.BuildContext.Create(cfg, _DocumentPath.DirectoryPath, isSimulation);
            }

            public Evaluation.PipelineClientState GetTaskState(Guid id)
            {
                if (id == Guid.Empty) return null;

                return _ProjectState[id];
            }

            private void _UpdatePipelineState()
            {
                var tasks = _Source.Items
                    .OfType<ProjectDOM.Task>()
                    .Select(item => item.Pipeline);

                var settings = _Source.Items
                    .OfType<ProjectDOM.Settings>()
                    .Select(item => item.Pipeline);

                var pipelines = tasks
                    .Concat(settings)
                    .Where(item => item.RootIdentifier != Guid.Empty);

                _ProjectState.Recycle(pipelines.Select(item => item.RootIdentifier) );
            }

            private void _SetActiveConfiguration(string value)
            {
                if (_ActiveConfiguration == value) return;
                _ActiveConfiguration = value;

                _ProjectState.Clear();

                
                ActiveDocument = null; // we must flush active document because it might be in the wrong configuration                

                RaiseChanged(nameof(ActiveConfiguration), nameof(Tasks), nameof(SharedSettings), nameof(IsRootConfiguration), nameof(CanAddItems), nameof(CanBuild));
            }

            private void _EditPlugin()
            {
                _Dialogs.ShowPluginsManagerDialog
                    (
                    _Application,
                    path => _Source.ContainsReference(_DocumentPath.DirectoryPath.MakeRelativePath(path)),
                    (path,ver) => _Source.UseAssemblyReference(_DocumentPath.DirectoryPath.MakeRelativePath(path),ver),
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
            
            internal ProjectDOM.Settings GetSharedSettings(Type t) { return _Source.UseSettings(t); }

            public void Test() { _BuildOrTest(true); RaiseChanged(nameof(Tasks)); }

            public void Build() { _BuildOrTest(false); RaiseChanged(nameof(Tasks)); }

            private void _BuildOrTest(bool isTest)
            {
                if (!CanBuild) { _Dialogs.ShowErrorDialog("Can't build"); return; }

                var bs = GetBuildSettings(isTest);

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
                        xlogger.AddProvider(_ProjectState);                        

                        var monitor = Evaluation.MonitorContext.Create(xlogger, ctoken, progress);                        

                        ProjectDOM.BuildProject(_Source, bs, _Plugins.CreateInstance, monitor,_ProjectState);                        
                    }
                };

                try
                {
                    buildTask.RunWithDialog();
                }
                catch (Exception ex)
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
                    .Where(item => !item.IsEmpty)                    
                    .Select(rp => System.IO.Path.Combine(SourceDirectory, rp))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(item => new PathString(item))
                    .Where(item => item.IsValidAbsoluteFilePath)
                    .ToArray();

                foreach(var apath in paths) Client.PluginLoader.Instance.UsePlugin(apath);
                
                _Plugins.SetAssemblies(Client.PluginLoader.Instance.GetPlugins() );

                RaiseChanged();
            }

            #endregion

            #region API - Tasks management

            private void _AddNewTask(Func<ProjectDOM.Task> addTaskFunc)
            {
                if (_ActiveConfiguration == null || _Configurations.IsEmpty) { _EditConfigurations(); return; }

                var task = addTaskFunc();
                if (task == null) return;

                RaiseChanged(nameof(Tasks), nameof(IsDirty));

                ActiveDocument = Tasks.FirstOrDefault(item => item.Source == task);

                RaiseChanged(nameof(CanBuild));
            }

            private void _AddNewTask()
            {
                _AddNewTask
                    (
                    () =>
                    {
                        var task = _Source.AddTask();
                        task.Title = "New Task " + Tasks.Count();
                        return task;
                    }
                    );                
            }

            private void _DeleteTask(BindableBase item)
            {
                if (item is Task task)
                {
                    if (!_Dialogs.QueryDeletePermanentlyWarningDialog(GetDisplayName(task))) return;

                    _Source.RemoveItem(task.Source);

                    RaiseChanged(nameof(Tasks), nameof(IsDirty));

                    if (ActiveDocument == item) ActiveDocument = null;

                    RaiseChanged(nameof(CanBuild));
                }
            }

            private void _CopyTaskToClipboard(BindableBase item)
            {
                if (item is Task task)
                {
                    _SetToClipboard(task.Source);
                }
            }

            private void _PasteTaskFromClipboard()
            {
                var task = _GetFromClipboard();
                if (task == null) return;

                _AddNewTask(() => _Source.AddTaskCopy(task));
            }

            #endregion

            #region API - Clipboard

            private static void _SetToClipboard(ProjectDOM.Task task)
            {
                try
                {
                    task = task.CreateDeepCopy(true);

                    var assembly_Core = typeof(Evaluation.PipelineInstance).Assembly;
                    var clipID = assembly_Core.InformationalVersion();

                    var xml = task.ToXml();

                    var text = xml.ToString(System.Xml.Linq.SaveOptions.DisableFormatting);

                    System.Windows.Clipboard.SetData(clipID, text);
                }
                catch { }
            }

            private static ProjectDOM.Task _GetFromClipboard()
            {
                try
                {
                    var assembly_Core = typeof(Evaluation.PipelineInstance).Assembly;
                    var clipID = assembly_Core.InformationalVersion();

                    if (!System.Windows.Clipboard.ContainsData(clipID)) return null;

                    var text = System.Windows.Clipboard.GetData(clipID) as String;

                    var xml = System.Xml.Linq.XElement.Parse(text);

                    return ProjectDOM.Task.Parse(xml);
                }
                catch { return null; }                
            }

            #endregion
        }        

        public class Configurations : BindableBase
        {
            #region lifecycle

            internal Configurations(ProjectDOM.Project prj)
            {
                var taskNodes = prj
                    .Items
                    .OfType<ProjectDOM.Task>()
                    .SelectMany(item => item.Pipeline.Nodes);

                var settingNodes = prj
                    .Items
                    .OfType<ProjectDOM.Settings>()
                    .SelectMany(item => item.Pipeline.Nodes);

                var nodes = taskNodes.Concat(settingNodes);

                var cfgQuery = nodes.SelectMany(item => item.AllConfigurations);

                _Configurations.UnionWith(cfgQuery);                
            }

            #endregion

            #region data

            private readonly HashSet<String> _Configurations = new HashSet<string>();

            #endregion

            #region properties

            public bool IsEmpty             => _Configurations.Count == 0;

            public bool HasRoot             => _Configurations.Count > 0;

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

            public BuildSettings(Evaluation.BuildContext bs)
            {                
                _Configuration = string.Join(Evaluation.BuildContext.ConfigurationSeparator.ToString(), bs.Configuration);
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

            public String Configuration         => _Configuration;

            public String SourceDirectory       => _SourceDirectory;
            public String TargetDirectory       => _TargetDirectory;

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

            public Evaluation.BuildContext GetBuildSettings()
            {
                return Evaluation.BuildContext.Create(_Configuration, _SourceDirectory, _TargetDirectory,false);
            }

            #endregion
        }
    }
}
