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


    


}
