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
    using System.Reflection;
    using TASKACTION = Action<System.Threading.CancellationToken, Action<Object>>;
    using TASKFUNCTION = Func<System.Threading.CancellationToken, Action<Object>, Object>;

    // http://www.codeproject.com/Articles/137552/WPF-TaskDialog-Wrapper-and-Emulator

    // Threading.Tasks for 3.5
    // http://www.microsoft.com/en-us/download/details.aspx?id=24940


    // API Codec Pack TaskDialog
    // https://www.codeproject.com/Tips/247358/TaskDilaog-via-Windows-API-Code-Pack-for-Microsoft


    public abstract class TaskMonitorWindow : Window, IDisposable
    {
        #region lifecycle

        private static bool IsDesignMode => LicenseManager.UsageMode == LicenseUsageMode.Designtime;

        public static void RunTask<T>(TASKACTION task, Window parentWindow = null, string windowTitle = null) where T : TaskMonitorWindow, new()
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            TASKFUNCTION func = (c, p) => { task(c, p); return null; };

            RunTask<T>(func, parentWindow, windowTitle);
        }

        public static Object RunTask<T>(TASKFUNCTION task, Window parentWindow = null, string windowTitle = null) where T: TaskMonitorWindow,new()
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            using (var dlg = new T())
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

        protected TaskMonitorWindow()
        {
            if (!IsDesignMode)
            {
                Loaded += (s, e) => this.Dispatcher.Invoke(_TaskRun);
            }            
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!IsDesignMode)
            {
                e.Cancel = _CancelSource != null;
                if (_CancelSource != null) _CancelSource.Cancel();
            }

            base.OnClosing(e);
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
            _TaskStart = DateTime.Now;
            _CancelSource = new System.Threading.CancellationTokenSource();

            Task.Run<Object>(() => _Task(_CancelSource.Token, _SendReportToWindow), _CancelSource.Token)
                .ContinueWith(_OnTaskComplete, TaskScheduler.Current);
        }

        protected void Abort()
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

        private void _SendReportToWindow(object value)
        {
            if (value == null) return;

            if (!this.Dispatcher.CheckAccess()) { this.Dispatcher.Invoke(() => this.ShowReport(value)); return; }

            this.ShowReport(value);
        }

        protected TimeSpan ElapsedTime => DateTime.Now - _TaskStart;

        protected virtual void ShowReport(Object message)
        {
            _TryReportProgress(this, message);
        }

        /// <summary>
        /// if recipient instance implements <code>IProgress with T=value</code> it casts the recipient and invokes the method
        /// </summary>
        /// <param name="recipient">the recipient of the message</param>
        /// <param name="message"></param>
        private static void _TryReportProgress(Object recipient, Object message)
        {
            // this is convenient... but probably not very fast

            if (recipient == null || message == null) return;

            var progressInterface = typeof(IProgress<>).MakeGenericType(message.GetType());            

            if (!progressInterface.GetTypeInfo().IsAssignableFrom(recipient.GetType())) return;

            progressInterface.GetMethod("Progress").Invoke(recipient, new Object[] { message });
        }

        #endregion
    }
}
