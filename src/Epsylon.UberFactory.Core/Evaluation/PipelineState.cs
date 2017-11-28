using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using MSLOGGING = Microsoft.Extensions.Logging;

namespace Epsylon.UberFactory.Evaluation
{
    /// <summary>
    /// holds the persistent state of a given pipeline
    /// </summary>
    public class PipelineState : INotifyPropertyChanged , MSLOGGING.ILogger
    {
        // TODO: with the current API, the updates are received from the processing thread, and the notifications are sent to the UI thread.

        #region lifecycle

        private PipelineState() { }

        #endregion

        #region data

        private readonly Object _Mutex = new object();

        private struct _FileInfo
        {            
            public DateTime Time;
            public long Length;
        }

        private readonly Dictionary<string, _FileInfo> _InputFiles = new Dictionary<string, _FileInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, _FileInfo> _OutputFiles = new Dictionary<string, _FileInfo>(StringComparer.OrdinalIgnoreCase);

        private readonly List<String> _Log = new List<string>();

        #endregion

        #region properties

        public event PropertyChangedEventHandler PropertyChanged;

        public IEnumerable<string> Log { get { lock (_Mutex) { return _Log.ToArray(); } } }

        #endregion

        #region API

        public void Update(Object result, PipelineFileManager results)
        {
        }

        bool MSLOGGING.ILogger.IsEnabled(MSLOGGING.LogLevel logLevel) { return true; }

        private class _NoopDisposable : IDisposable
        {
            public static _NoopDisposable Instance = new _NoopDisposable();

            public void Dispose() { }
        }

        IDisposable MSLOGGING.ILogger.BeginScope<TState>(TState state) { return _NoopDisposable.Instance; }

        void MSLOGGING.ILogger.Log<TState>(MSLOGGING.LogLevel logLevel, MSLOGGING.EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var text = formatter(state, exception);

            lock(_Mutex) { _Log.Add(text); }

            _RaiseChanged(nameof(Log));

        }

        private void _RaiseChanged(params string[] ps)
        {
            if (PropertyChanged == null) return;

            if (ps == null || ps.Length == 0) { PropertyChanged(this, new PropertyChangedEventArgs(null)); return; }

            foreach (var p in ps) PropertyChanged(this, new PropertyChangedEventArgs(p));
        }

        #endregion

        #region collection

        public class Manager : IReadOnlyDictionary<Guid, PipelineState>, MSLOGGING.ILoggerProvider
        {
            #region lifecycle

            public void Dispose() // required by ILoggerProvider
            {

            }

            #endregion

            #region data

            private readonly Object _Mutex = new object();

            private readonly Dictionary<Guid, PipelineState> _InternalDict = new Dictionary<Guid, PipelineState>();

            #endregion

            #region API

            public PipelineState this[Guid key] { get { lock (_Mutex) { return _InternalDict[key]; } } }

            public IEnumerable<Guid> Keys { get { lock (_Mutex) { return _InternalDict.Keys.ToList(); } } }

            public IEnumerable<PipelineState> Values { get { lock (_Mutex) { return _InternalDict.Values.ToList(); } } }

            public int Count { get { lock (_Mutex) { return _InternalDict.Count; } } }

            public bool ContainsKey(Guid key) { lock (_Mutex) { return _InternalDict.ContainsKey(key); } }

            public bool TryGetValue(Guid key, out PipelineState value) { lock (_Mutex) { return _InternalDict.TryGetValue(key, out value); } }

            public IEnumerator<KeyValuePair<Guid, PipelineState>> GetEnumerator() { lock (_Mutex) { return _InternalDict.ToList().GetEnumerator(); } }

            IEnumerator IEnumerable.GetEnumerator() { lock (_Mutex) { return _InternalDict.ToList().GetEnumerator(); } }

            public void Clear() { lock (_Mutex) { _InternalDict.Clear(); } }

            public void Recycle(IEnumerable<Guid> items)
            {
                lock (_Mutex)
                {
                    var toInsert = items
                        .Where(item => !_InternalDict.ContainsKey(item))
                        .ToArray();

                    var toRemove = _InternalDict
                        .Keys
                        .Where(item => !items.Contains(item))
                        .ToArray();

                    foreach (var tr in toRemove) _InternalDict.Remove(tr);
                    foreach (var ta in toInsert) _InternalDict.Add(ta, new PipelineState());
                }
            }            

            public void Update(Guid key, Object result, PipelineFileManager results)
            {
                if (this.TryGetValue(key, out PipelineState state)) state.Update(result, results);
            }

            public MSLOGGING.ILogger CreateLogger(string categoryName)
            {
                if (Guid.TryParse(categoryName, out Guid key))
                {
                    if (this.TryGetValue(key, out PipelineState state)) return state;
                }

                return MSLOGGING.Abstractions.NullLogger.Instance;
            }

            #endregion
        }

        #endregion
    }
}
