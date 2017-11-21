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
    public partial class AboutPanel : UserControl
    {
        public AboutPanel()
        {
            this.Loaded += AboutPanel_Loaded;

            InitializeComponent();            
        }

        private void AboutPanel_Loaded(object sender, RoutedEventArgs e)
        {
            var assembly_SDK = typeof(SDK.ContentFilter).Assembly;
            var assembly_Core = typeof(Evaluation.PipelineInstance).Assembly;
            var assembly_Editor = typeof(AppView).Assembly;

            this.mySDKVersion.Text = assembly_SDK.InformationalVersion();
            this.myCoreVersion.Text = assembly_Core.InformationalVersion();

            this.myEditorVersion.Text = assembly_Editor.InformationalVersion();
            this.myArchitecture.Text = assembly_Editor.GetName().ProcessorArchitecture.ToString();
            this.myCopyright.Text = assembly_Editor.InfoCopyright();
        }

        
    }
}
