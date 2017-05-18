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

namespace Epsylon.UberFactory.Themes
{    
    public partial class PreviewResultPanel : UserControl
    {
        public PreviewResultPanel()
        {
            InitializeComponent();
        }

        private void _CopyToClipboard(object sender, RoutedEventArgs e)
        {
            var data = this.DataContext;

            if (data is Exception) { data = ((Exception)data).ToString(); }

            if (data is String) Clipboard.SetText((String)data);
        }
    }
}
