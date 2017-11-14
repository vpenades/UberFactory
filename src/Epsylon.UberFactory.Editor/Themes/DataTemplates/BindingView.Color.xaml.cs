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
    public partial class ColorBindingView : UserControl
    {
        public ColorBindingView()
        {
            InitializeComponent();
        }

        private void _OnClick_ShowColorPaletteDialog(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement f)
            {
                if (f.DataContext is Bindings.InputValueBinding<UInt32> v)
                {
                    v.Value = _Dialogs.ShowColorPickerDialog(v.Value);
                }
            }            
        }
    }
}
