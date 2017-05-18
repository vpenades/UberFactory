using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{

    public class EventLoggerProvider : ILoggerProvider
    {
        #region data

        private readonly System.Collections.ObjectModel.ObservableCollection<String> _Rows = new System.Collections.ObjectModel.ObservableCollection<string>();

        #endregion

        #region properties

        public IReadOnlyCollection<String> Rows => _Rows;        

        #endregion

        #region API

        internal void _AppendToLog(string text)
        {
            // TODO:
            // for massive loggers, this can overload the message pump, another approach would be
            // to lock the row, and call an invalidate, and in the main thread, maybe at 10fps, check if its invalid

            if (System.Windows.Threading.Dispatcher.CurrentDispatcher.CheckAccess()) { _Rows.Add(text); return; }

            Action<String> act = _AppendToLog;

            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(act, System.Windows.Threading.DispatcherPriority.Background, text);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new EventLogger(this, categoryName);
        }

        public void Dispose()
        {
            
        }

        #endregion
    }



    public class EventLogger : ILogger
    {
        internal EventLogger(EventLoggerProvider provider, String name) { _Provider = provider; _Name = name; }

        private readonly EventLoggerProvider _Provider;
        private readonly String _Name;

        private class NoopDisposable : IDisposable
        {
            public static NoopDisposable Instance = new NoopDisposable();

            public void Dispose() { }
        }

        public IDisposable BeginScope<TState>(TState state) { return NoopDisposable.Instance; }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var text = logLevel.ToString() + " " + _Name + " " + formatter(state, exception);

            _Provider._AppendToLog(text);
        }
    }
}
