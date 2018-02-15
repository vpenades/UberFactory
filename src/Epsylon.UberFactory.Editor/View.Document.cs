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
        public abstract class Document<T> : BindableBase
        {
            // handles relationship between File and Live document

            // Use Cases:
            // Live document is newer than File Document => Save dialog
            // File document is newer than File document => Reload dialog
            // Live document is not dirty, but File was deleted => Save dialog

            #region lifecycle

            private Document(PathString path)
            {                
                _DocumentPath = path;

                SaveCmd = new RelayCommand(Save);
            }

            public void Save()
            {
                try
                {
                    SaveContent(_DocumentPath, _Content);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                _ContentVersion = GetContentVersion(_Content);

                RaiseChanged(nameof(IsDirty));
            }

            #endregion

            #region commands

            public ICommand SaveCmd { get; private set; }

            #endregion

            #region data
            
            private readonly PathString _DocumentPath;

            private T _Content;
            private IComparable _ContentVersion;

            #endregion

            #region properties            

            protected T Content             => _Content;

            public String DocumentPath      => _DocumentPath;

            public String DisplayName       => _DocumentPath.FileNameWithoutExtension;

            public String SourceDirectory   => _DocumentPath.DirectoryPath;

            public Boolean IsReadOnly       => _DocumentPath.IsReadOnly;

            public Boolean IsDirty          => _ContentVersion.CompareTo(GetContentVersion(_Content)) == 0;

            #endregion

            #region API

            protected abstract IComparable GetContentVersion(T content);

            protected abstract void SaveContent(PathString dstPath, T content);

            protected abstract T ReadContent(PathString srcPath);

            #endregion

        }

    }
}
