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
    public partial class ConfigurationsEditPanel : UserControl
    {
        public ConfigurationsEditPanel()
        {
            InitializeComponent();
        }

        private void _OnClick_AddConfiguration(object sender, RoutedEventArgs e)
        {
            var xdata = this.DataContext as ProjectVIEW.Configurations;
            if (xdata == null) return;

            var newCfg = myNewConfig.Text.Trim();

            if (!BuildContext.IsValidConfigurationNode(newCfg)) return;            

            if (xdata.All.Count() == 0)
            {
                xdata.AddConfig(newCfg);
            }

            var basePath = myList.SelectedValue as String;
            if (string.IsNullOrWhiteSpace(basePath)) return;
            xdata.AddConfig(basePath + BuildContext.ConfigurationSeparator + newCfg);
        }

        private void myList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            myCurrentSelection.Text = (myList.SelectedValue as String).EnsureNotNull()+ BuildContext.ConfigurationSeparator;
        }
    }
}
