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
    public partial class PluginsPanel : UserControl
    {
        public PluginsPanel()
        {
            InitializeComponent();

            var cpuarch = IntPtr.Size == 4 ? "x86" : "x64";

            myCurrentCfg.Text = $"Current Architecture: NetFX {cpuarch}";
        }        
    }
}
