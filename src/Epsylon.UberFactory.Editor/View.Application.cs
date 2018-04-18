using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Epsylon.UberFactory
{
    public class AppView : BindableBase
    {
        #region lifecycle

        public AppView()
        {
            NewDocumentCmd = new RelayCommand(_CreateNewDocument);            
            OpenDocumentCmd = new RelayCommand(_OpenDocument);
            OpenKnownDocumentCmd = new RelayCommand<String>(_OpenDocument);
            CloseDocumentCmd = new RelayCommand(()=>CloseDocument());

            ShowAboutDialogCmd = new RelayCommand(() => _Dialogs.ShowAboutDialog(null));

            ExitApplicationCmd = new RelayCommand(ExitApplication);
            
            DocumentView = ProjectVIEW.TryCreateFromCommandLine(this);

            if (_Document == null) _Document = new HomeView(this);
        }

        #endregion

        #region commands

        public ICommand NewDocumentCmd { get; private set; }

        public ICommand OpenDocumentCmd { get; private set; }

        public ICommand OpenKnownDocumentCmd { get; private set; }

        public ICommand CloseDocumentCmd { get; private set; }        

        public ICommand ShowAboutDialogCmd { get; private set; }

        public ICommand ExitApplicationCmd { get; private set; }

        #endregion

        #region data

        private BindableBase _Document;

        internal readonly HashSet<PathString> _PluginPaths = new HashSet<PathString>();

        #endregion

        #region properties        

        public IEnumerable<string> RecentDocuments => RecentFilesManager.RecentFiles.ToArray();

        public BindableBase DocumentView
        {
            get { return _Document; }
            private set
            {
                _Document = value;
                RaiseChanged(nameof(DocumentView));
                _UpdatePluginPaths();
            }
        }

        private void _UpdatePluginPaths()
        {
            if (_Document is ProjectVIEW.Project prj)
            {
                foreach (var pp in prj.AbsoluteFilePaths)
                {
                    var ppv = PluginView.Create(pp, null, null, null);
                    if (ppv == null) continue;

                    _PluginPaths.Add(pp);
                }
            }
        }

        #endregion

        #region API

        private bool _CheckKeepCurrentDocument()
        {
            if (DocumentView is ProjectVIEW.Project prjv)
            {
                if (prjv.IsDirty)
                {
                    var r = _Dialogs.ShowSaveChangesDialog(prjv.DisplayName);
                    if (r == System.Windows.MessageBoxResult.Cancel) return true;
                    if (r == System.Windows.MessageBoxResult.Yes) prjv.Save();
                }

                DocumentView = new HomeView(this); // saved or discarded, we can get rid of it.
            }            

            return false;
        }        

        private void _CreateNewDocument()
        {
            if (_CheckKeepCurrentDocument()) return;

            var plugins = Client.PluginLoader.Instance.GetPlugins();
            if (plugins != null && plugins.Count() > 0)
            {
                (System.Windows.Application.Current as App).Restart();
                return;
            }

            var doc = ProjectVIEW.CreateNew(this);
            if (doc == null) return;

            RecentFilesManager.InsertFile(doc.DocumentPath);

            _Document = doc;            

            RaiseChanged(nameof(DocumentView));
        }

        private void _OpenDocument()
        {
            if (_CheckKeepCurrentDocument()) return;            

            var dlg = new Microsoft.Win32.OpenFileDialog()
            {
                RestoreDirectory = true,
                Filter = "Über Project File|*.uberfactory"
            };

            if (!dlg.ShowDialog().Value) return;

            _OpenDocument(dlg.FileName);
        }

        private void _OpenDocument(String fPath)
        {
            if (_CheckKeepCurrentDocument()) return;            

            var filePath = new PathString(fPath);

            // if there's plugins already loaded, we need to load the new document with an application's restart
            var plugins = Client.PluginLoader.Instance.GetPlugins();
            if (plugins != null && plugins.Count() > 0)
            {                

                (System.Windows.Application.Current as App).RestartAndLoad(filePath);
                return;
            }

            // load normally;

            var doc = ProjectVIEW.OpenFile(this, filePath);
            if (doc == null) { RecentFilesManager.RemoveFile(filePath); return; }

            RecentFilesManager.InsertFile(doc.DocumentPath);

            DocumentView = doc;            
        }        

        public bool CloseDocument()
        {
            if (_CheckKeepCurrentDocument()) return false;

            DocumentView = new HomeView(this);

            return true;
        }        

        public void ExitApplication()
        {
            if (!CloseDocument()) return;

            System.Windows.Application.Current.Shutdown();
        }

        #endregion
    }


    public class HomeView : BindableBase
    {
        #region lifecycle

        public HomeView(AppView parent)
        {
            _Application = parent;

            ShowPluginsManagerCmd = new RelayCommand(()=>_Dialogs.ShowPluginsManagerDialog(_Application));
        }

        public ICommand ShowPluginsManagerCmd { get; private set; }

        #endregion

        #region data

        private readonly AppView _Application;

        #endregion

        #region properties

        public AppView Application  => _Application;

        public string Title         => this.GetType().Assembly.GetDisplayTitle(true, true, null);

        #endregion
    }


    public class PluginsCollectionView : BindableBase
    {
        #region lifecycle

        public PluginsCollectionView(AppView app, Func<string, bool> check, Action<string, string> insertAssembly, Action<string> remove)
        {
            _Application = app;

            _CheckAction = check;
            _InsertAssemblyAction = insertAssembly;
            _RemoveAction = remove;

            DiscoverCmd = new RelayCommand(DiscoverPlugins);
            AddPluginCmd = new RelayCommand(BrowsePlugin);            
        }

        #endregion

        #region data

        private readonly AppView _Application;

        private readonly Func<string, bool> _CheckAction;
        private readonly Action<string,string> _InsertAssemblyAction;
        private readonly Action<string> _RemoveAction;

        #endregion

        #region properties

        public bool ShowEnabled => _CheckAction != null;

        [System.ComponentModel.DisplayName("Discover Plugins...")]
        public ICommand DiscoverCmd { get; private set; }

        [System.ComponentModel.DisplayName("Browse Plugin...")]
        public ICommand AddPluginCmd { get; private set; }        

        public IEnumerable<PluginView> Assemblies
        {
            get
            {
                return _Application._PluginPaths
                    .Select(item => PluginView.Create(item,_CheckAction,_InsertAssemblyAction,_RemoveAction))
                    .ExceptNulls()
                    .ToArray();
            }
        }

        #endregion

        #region API

        public void DiscoverPlugins()
        {
            var dirPath = _Dialogs.ShowBrowseDirectoryDialog(PathString.Empty);
            if (dirPath.IsEmpty) return;

            var files = System.IO.Directory.GetFiles(dirPath, "*.UberPlugin.*dll", System.IO.SearchOption.AllDirectories);            

            foreach (var f in files) _TryAddAssemblyFromFile(new PathString(f));

            RaiseChanged(nameof(Assemblies));
        }

        public void BrowsePlugin()
        {
            var filePath = _Dialogs.ShowOpenFileDialog("Assembly files|*.dll", PathString.Empty);
            if (filePath.IsEmpty) return;            

            _TryAddAssemblyFromFile(filePath);

            RaiseChanged(nameof(Assemblies));            
        }

        private static string _IsValidAssemblyFile(PathString path)
        {
            if (path.ToString().ToLower().ContainsAny("\\obj\\", "\\.vs\\", "\\.svn\\", "\\.git\\")) return "path is temporary";

            if (!path.FileExists) return "Doesn't exist";

            return null;            
        }

        private void _TryAddAssemblyFromFile(PathString path)
        {
            if (path.FileName.ToLower() == "epsylon.uberFactory.sdk.dll") return;
            if (path.FileName.ToLower() == "epsylon.uberFactory.core.dll") return;
            if (path.FileName.ToLower() == "epsylon.uberFactory.client.dll") return;

            if (_IsValidAssemblyFile(path) != null) return;
            _Application._PluginPaths.Add(path);
        }

        #endregion
    }


    public class PluginView : BindableBase
    {
        #region lifecycle

        public static PluginView Create(string basePath, string relPath, Func<string, bool> check, Action<string,string> insert, Action<string> remove)
        {
            var absPath = System.IO.Path.Combine(basePath, relPath);
            return Create(absPath, check,insert,remove);
        }

        public static PluginView Create(string absPath, Func<string, bool> check, Action<string,string> insert, Action<string> remove)
        {
            try
            {
                if (!System.IO.File.Exists(absPath)) return null;

                var ainfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(absPath);
                return new PluginView(ainfo,check,insert,remove);
            }
            catch
            {
                return null;
            }
        }

        private PluginView(System.Diagnostics.FileVersionInfo ainfo, Func<string, bool> check, Action<string,string> insert, Action<string> remove)
        {
            _AssemblyInfo = ainfo;
            _AssemblyName = System.Reflection.AssemblyName.GetAssemblyName(ainfo.FileName);

            _CheckAction = check;
            _InsertAction = insert;
            _RemoveAction = remove;            
            
            ShowContainingFolderCmd = new RelayCommand(() => _AssemblyInfo.Location().TryOpenContainingFolder() );
        }

        public ICommand ShowContainingFolderCmd { get; private set; }

        #endregion

        #region data

        private readonly System.Diagnostics.FileVersionInfo _AssemblyInfo;
        private readonly System.Reflection.AssemblyName _AssemblyName;

        private readonly Func<string,bool> _CheckAction;
        private readonly Action<string,string> _InsertAction;
        private readonly Action<string> _RemoveAction;

        #endregion

        #region properties

        public Boolean CanReference
        {
            get
            {
                if (_AssemblyName.ProcessorArchitecture == System.Reflection.ProcessorArchitecture.MSIL) return true;
                if (_AssemblyName.ProcessorArchitecture == System.Reflection.ProcessorArchitecture.X86 && IntPtr.Size == 4) return true;
                if (_AssemblyName.ProcessorArchitecture == System.Reflection.ProcessorArchitecture.Amd64 && IntPtr.Size == 8) return true;
                if (_AssemblyName.ProcessorArchitecture == System.Reflection.ProcessorArchitecture.IA64 && IntPtr.Size == 8) return true;
                return false;
            }
        }

        public Boolean Referenced
        {
            get { return _CheckAction == null ? false: _CheckAction(FilePath); }
            set
            {
                if ( value && _InsertAction != null) _InsertAction(FilePath, ProductVersion);
                if (!value && _RemoveAction != null) _RemoveAction(FilePath);
            }
        }

        public String FilePath          => _AssemblyInfo.FileName;

        public String FileName          => System.IO.Path.GetFileName(_AssemblyInfo.FileName);

        public String ProductVersion    => _AssemblyInfo.ProductVersion;

        public String FileVersion       => _AssemblyInfo.FileVersion;        

        public Boolean IsLoaded         => _AssemblyInfo.IsLoaded();

        public String Status            => IsLoaded ? "Loaded" : String.Empty;

        public String Company           => _AssemblyInfo.CompanyName;

        public String Description       => _AssemblyInfo.Comments; // from AssemblyDescriptionAttribute

        public String Configuration
        {
            get
            {
                if (_AssemblyInfo.IsDebug) return "Debug"; // not working
                if (FilePath.ToLower().Contains("\\debug\\")) return "Debug";
                return "Release";
            }
        }

        public System.Reflection.ProcessorArchitecture Architecture => _AssemblyName.ProcessorArchitecture;

        public DateTime LastWriteTime => new System.IO.FileInfo(FilePath).LastWriteTime;

        public System.Windows.Media.Imaging.BitmapSource FileIcon
        {
            get
            {
                try
                {
                    var icon = System.Drawing.Icon.ExtractAssociatedIcon(FilePath);
                    using (var bmp = icon.ToBitmap())
                    {
                        var stream = new System.IO.MemoryStream();
                        bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        return System.Windows.Media.Imaging.BitmapFrame.Create(stream);
                    }
                }
                catch
                {
                    return null;
                }
            }

        }        

        #endregion

    }


}
