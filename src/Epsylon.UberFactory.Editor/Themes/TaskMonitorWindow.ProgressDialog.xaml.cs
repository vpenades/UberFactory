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
using System.Windows.Shapes;

namespace Epsylon.UberFactory.Themes
{
    
    public partial class TaskMonitorProgressDialog : TaskMonitorWindow , IProgress<float>
    {
        static TaskMonitorProgressDialog()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TaskMonitorProgressDialog), new FrameworkPropertyMetadata(typeof(Window)));
        }

        public TaskMonitorProgressDialog()
        {
            InitializeComponent();
        }


        protected override void ShowReport(object value)
        {
            if (value is float asFloat) this.Report(asFloat);
        }

        public void Report(float progress)
        {
            if (progress < 0)
            {
                this.myProgressBar.IsIndeterminate = true;
                this.myPercent.Text = string.Empty;
            }
            else
            {
                var percent = (int)(progress.Clamp(0, 1) * 100.0f);

                this.myProgressBar.IsIndeterminate = false;
                this.myProgressBar.Value = percent;
                this.myPercent.Text = percent.ToString() + "%";
            }

            this.myElapsedTime.Text = string.Format("Elapsed Time: {0:hh\\:mm\\:ss}", this.ElapsedTime);
        }

        private void _OnTaskCancelRequest(object sender, RoutedEventArgs e)
        {
            this.Abort();
        }
    }
}
