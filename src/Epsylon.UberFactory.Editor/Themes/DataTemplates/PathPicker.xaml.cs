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

namespace Epsylon.UberFactory.Themes.DataTemplates
{
    
    public partial class PathPicker : UserControl
    {
        public PathPicker()
        {
            InitializeComponent();
        }

        private void _OnClick_PickFile(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is Bindings.SourceFilePickBinding binding)
            {
                var startDir = binding.DataContext.BuildContext.GetSourceAbsolutePath("dummy.txt");

                var path = _Dialogs.ShowOpenFileDialog(binding.GetFileFilter(), new PathString(startDir).DirectoryPath);

                if (path.IsEmpty) return;

                binding.Value = path;
            }
        }
    }
}
