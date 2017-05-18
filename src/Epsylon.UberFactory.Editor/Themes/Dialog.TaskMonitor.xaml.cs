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
    using System.ComponentModel;
    using TASKACTION = Action<System.Threading.CancellationToken, IProgress<float>>;
    using TASKFUNCTION = Func<System.Threading.CancellationToken, IProgress<float>, Object>;

    // http://www.codeproject.com/Articles/137552/WPF-TaskDialog-Wrapper-and-Emulator

    // Threading.Tasks for 3.5
    // http://www.microsoft.com/en-us/download/details.aspx?id=24940


    // API Codec Pack TaskDialog
    // https://www.codeproject.com/Tips/247358/TaskDilaog-via-Windows-API-Code-Pack-for-Microsoft




    public sealed partial class TaskMonitorDialog : Window, IProgress<float> , IDisposable
    {
        #region lifecycle

        static TaskMonitorDialog()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TaskMonitorDialog), new FrameworkPropertyMetadata(typeof(Window)));
        }

        public static void RunTask(TASKACTION task, Window parentWindow = null, string windowTitle = null)
        {
            TASKFUNCTION func = (c, p) => { task(c, p); return null; };

            RunTask(func, parentWindow, windowTitle);            
        }

        public static Object RunTask(TASKFUNCTION task, Window parentWindow = null, string windowTitle = null)
        {
            using (var dlg = new TaskMonitorDialog())
            {
                if (parentWindow == null) parentWindow = Application.Current.MainWindow;
                dlg.Owner = parentWindow;
                dlg.Icon = parentWindow.Icon;
                dlg.Title = !string.IsNullOrEmpty(windowTitle) ? windowTitle : parentWindow.Title;

                dlg._Task = task;
                dlg._Result = null;

                dlg.ShowDialog();
                dlg.Owner = null;

                var result = dlg._Result;

                if (result is Exception) throw new InvalidOperationException("Task Failed", result as Exception);

                return result;
            }
        }        

        private TaskMonitorDialog()
        {
            Loaded += (s,e) => this.Dispatcher.Invoke(_TaskRun);            

            InitializeComponent();
        }

        public void Dispose()
        {
            if (_CancelSource != null) { _CancelSource.Dispose(); _CancelSource = null; }
        }

        #endregion

        #region data

        private DateTime _TaskStart;
        private System.Threading.CancellationTokenSource _CancelSource;

        private TASKFUNCTION _Task;        
        private Object _Result;

        #endregion

        #region events

        private void _TaskRun()
        {
            if (_Task == null) { this.Close(); return; }

            _TaskStart = DateTime.Now;
            _CancelSource = new System.Threading.CancellationTokenSource();
            
            Task.Run<Object>(() => _Task(_CancelSource.Token, this), _CancelSource.Token)
                .ContinueWith(_OnTaskComplete, TaskScheduler.Current);            
        }

        private void _OnTaskCancelRequest(object sender, RoutedEventArgs e)
        {
            if (_CancelSource != null) _CancelSource.Cancel();
        }

        private void _OnTaskComplete(Task<Object> task)
        {
            System.Diagnostics.Debug.Assert(task.IsCompleted);

            _Result = null;
            if (task.IsFaulted) _TaskFinally(task.Exception);
            else if (!task.IsCanceled) _TaskFinally(task.Result);
        }

        private void _TaskFinally(object result)
        {
            _Result = result;

            _CancelSource.Dispose();
            _CancelSource = null;

            this.Dispatcher.Invoke(this.Close);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = _CancelSource != null;

            if (_CancelSource != null) _CancelSource.Cancel();

            base.OnClosing(e);
        }           

        void IProgress<float>.Report(float value) { _Report(value); }

        private void _Report(float value)
        {
            if (!this.Dispatcher.CheckAccess()) { this.Dispatcher.Invoke(() => this._Report(value)); return; }

            if (value < 0)
            {
                this.myProgressBar.IsIndeterminate = true;
                this.myPercent.Text = string.Empty;
            }
            else
            {
                var percent = (int)(value.Clamp(0, 1) * 100.0f);

                this.myProgressBar.IsIndeterminate = false;
                this.myProgressBar.Value = percent;
                this.myPercent.Text = percent.ToString() + "%";
            }

            var ts = DateTime.Now - _TaskStart;

            this.myElapsedTime.Text = string.Format("Elapsed Time: {0:hh\\:mm\\:ss}", ts);
        }        

        #endregion
    }
}
