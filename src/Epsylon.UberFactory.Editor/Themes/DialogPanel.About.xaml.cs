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
            this.mySDKVersion.Text = typeof(SDK.ContentFilter).Assembly.Version().ToString();
            this.myCoreVersion.Text = typeof(Evaluation.PipelineEvaluator).Assembly.Version().ToString();

            var editAsm = typeof(AppView).Assembly;

            this.myEditorVersion.Text = editAsm.Version().ToString();

            this.myArchitecture.Text = editAsm.GetName().ProcessorArchitecture.ToString();

            this.myCopyright.Text = editAsm.InfoCopyright();
        }
    }
}
