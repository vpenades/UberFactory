using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Epsylon.UberFactory
{
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
        private readonly Action<string, string> _InsertAssemblyAction;
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
                    .Select(item => PluginView.Create(item, _CheckAction, _InsertAssemblyAction, _RemoveAction))
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

        public static PluginView Create(string basePath, string relPath, Func<string, bool> check, Action<string, string> insert, Action<string> remove)
        {
            var absPath = System.IO.Path.Combine(basePath, relPath);
            return Create(absPath, check, insert, remove);
        }

        public static PluginView Create(string absPath, Func<string, bool> check, Action<string, string> insert, Action<string> remove)
        {
            try
            {
                if (!System.IO.File.Exists(absPath)) return null;

                var ainfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(absPath);
                return new PluginView(ainfo, check, insert, remove);
            }
            catch
            {
                return null;
            }
        }

        private PluginView(System.Diagnostics.FileVersionInfo ainfo, Func<string, bool> check, Action<string, string> insert, Action<string> remove)
        {
            _AssemblyInfo = ainfo;
            _AssemblyName = System.Reflection.AssemblyName.GetAssemblyName(ainfo.FileName);

            _CheckAction = check;
            _InsertAction = insert;
            _RemoveAction = remove;

            ShowContainingFolderCmd = new RelayCommand(() => _AssemblyInfo.Location().TryOpenContainingFolder());
        }

        public ICommand ShowContainingFolderCmd { get; private set; }

        #endregion

        #region data

        private readonly System.Diagnostics.FileVersionInfo _AssemblyInfo;
        private readonly System.Reflection.AssemblyName _AssemblyName;

        private readonly Func<string, bool> _CheckAction;
        private readonly Action<string, string> _InsertAction;
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
            get { return _CheckAction == null ? false : _CheckAction(FilePath); }
            set
            {
                if (value && _InsertAction != null) _InsertAction(FilePath, ProductVersion);
                if (!value && _RemoveAction != null) _RemoveAction(FilePath);
            }
        }

        public String FilePath => _AssemblyInfo.FileName;

        public String FileName => System.IO.Path.GetFileName(_AssemblyInfo.FileName);

        public String ProductVersion => _AssemblyInfo.ProductVersion;

        public String FileVersion => _AssemblyInfo.FileVersion;

        public Boolean IsLoaded => _AssemblyInfo.IsLoaded();        

        public String Status => IsLoaded ? "Loaded" : String.Empty;

        public String Company => _AssemblyInfo.CompanyName;

        public String Description => _AssemblyInfo.Comments; // from AssemblyDescriptionAttribute

        public String Configuration
        {
            get
            {
                if (_AssemblyInfo.IsLoaded()) return _AssemblyInfo.GetLoadedAssembly().IsDebug() ? "Debug" : "Release";

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
