using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Epsylon.UberFactory.Themes.ProjectItems
{
    
    public partial class PathPicker : UserControl
    {
        public PathPicker()
        {
            InitializeComponent();            
        }

        private void _OnClick_PickFile(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is Bindings.InputStringBinding vbinding)
            {
                var startDir = vbinding.DataContext.GetAbsoluteSourcePath("dummy.txt").DirectoryPath;

                var path = PathString.Empty;

                if (vbinding.IsFilePicker) path = _Dialogs.ShowOpenFileDialog(vbinding.GetFileFilter(), startDir);
                if (vbinding.IsDirectoryPicker) path = _Dialogs.ShowBrowseDirectoryDialog(startDir);

                if (path.IsEmpty) return;

                Bindings.InputStringBinding.SetAbsoluteSourcePath(vbinding, path);
            }
        }

        private void _OnClick_InspectFile(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is Bindings.InputStringBinding vbinding)
            {
                var absPath = Bindings.InputStringBinding.GetAbsoluteSourcePath(vbinding);

                absPath.TryOpenContainingFolder();
            }
        }
    }
}
