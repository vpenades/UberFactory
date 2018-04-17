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

namespace Epsylon.UberFactory
{    
    public partial class AppWindow : Window
    {
        public AppWindow()
        {
            InitializeComponent();

            this.DataContext = new AppView();

            var wbounds = Environment.GetCommandLineArgs().FirstOrDefault(item => item.StartsWith("-WBOUNDS:"));

            if (wbounds != null)
            {
                var parts = wbounds.Split(':');
                if (parts.Length == 5)
                {
                    var xywl = parts.Skip(1).Select(item => int.Parse(item)).ToArray();

                    // check more constraints
                    if (xywl[2] > 10 && xywl[3] > 10)
                    {
                        this.WindowStartupLocation = WindowStartupLocation.Manual;
                        this.Left = xywl[0];
                        this.Top = xywl[1];
                        this.Width = xywl[2];
                        this.Height = xywl[3];
                    }
                }

                this.Loaded += (s, e) => this.Show();
            }

        }
        

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.DataContext is AppView appv)
            {
                if (appv.DocumentView is ProjectVIEW.Project prjv)
                {
                    if (prjv.IsDirty && !appv.CloseDocument()) { e.Cancel = true; return; }
                }
            }
        }
    }
    
}
