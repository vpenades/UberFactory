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

            // Undo/Redo ??

            #region lifecycle                       

            protected Document(T document, PathString path)
            {
                _DocumentPath = path;
                _DocumentContent = document;
                _DocumentSnapshot = GetContentSnapshot(_DocumentContent);

                SaveCmd = new RelayCommand(Save);
            }

            public void Save()
            {
                try
                {
                    WriteContent(_DocumentPath, _DocumentContent);
                    _DocumentSnapshot = GetContentSnapshot(_DocumentContent);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }                

                RaiseChanged(nameof(IsDirty));
            }

            #endregion

            #region commands

            public ICommand SaveCmd { get; private set; }

            #endregion

            #region data
            
            private PathString  _DocumentPath;
            private T           _DocumentContent;
            private IComparable _DocumentSnapshot;

            #endregion

            #region properties            

            public T Content             => _DocumentContent;

            public String DocumentPath      => _DocumentPath;

            public String DisplayName       => _DocumentPath.FileNameWithoutExtension;

            public PathString SourceDirectory   => _DocumentPath.DirectoryPath;

            public Boolean IsReadOnly       => _DocumentPath.IsReadOnly;

            public Boolean IsDirty          => _DocumentSnapshot.CompareTo(GetContentSnapshot(_DocumentContent)) == 0;

            #endregion

            #region API

            protected abstract IComparable GetContentSnapshot(T content);

            protected abstract void WriteContent(PathString dstPath, T content);

            protected abstract T ReadContent(PathString srcPath);

            #endregion

        }

    }
}
