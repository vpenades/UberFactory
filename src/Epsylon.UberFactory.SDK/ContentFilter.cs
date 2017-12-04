using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Epsylon.UberFactory
{
    public static partial class SDK
    {
        public abstract class ContentFilter : ContentObject // ContentEvaluator
        {
            #region lifecycle

            public ContentFilter() { }

            #endregion

            #region data

            // TODO: add a "EnqueueForDispose" or "AtExit" to store disposable objects that need to be disposed at the end of the processing pipeline            

            internal IMonitorContext _MonitorContext;
            internal ILogger _Logger;

            #endregion

            #region user's API

            protected void CheckCancelation()
            {
                if (_MonitorContext != null && _MonitorContext.IsCancelRequested) throw new OperationCanceledException();
            }

            protected void SetProgressPercent(int percent) { SetProgress((float)percent / 100.0f); }

            protected void SetProgress(float value) { _MonitorContext?.Report(value); CheckCancelation(); }            

            public void LogTrace(string message)    { _Logger?.LogTrace(this.GetType().Name,message); CheckCancelation(); }
            public void LogDebug(string message)    { _Logger?.LogDebug(this.GetType().Name, message); CheckCancelation(); }
            public void LogInfo(string message)     { _Logger?.LogInfo(this.GetType().Name, message); CheckCancelation(); }
            public void LogWarning(string message)  { _Logger?.LogWarning(this.GetType().Name, message); CheckCancelation(); }
            public void LogError(string message)    { _Logger?.LogError(this.GetType().Name, message); CheckCancelation(); }
            public void LogCritical(string message) { _Logger?.LogCritical(this.GetType().Name, message); CheckCancelation(); }

            protected abstract Object EvaluateObject();

            protected virtual Object EvaluatePreview(PreviewContext previewContext) { return EvaluateObject(); }

            #endregion

            #region internals

            internal Object _EvaluateObject(IMonitorContext monitor, ILogger logger)
            {
                if (BuildContext == null) throw new InvalidOperationException($"{this.GetType().Name} not initialized");

                _MonitorContext = monitor;
                _Logger = logger;

                var r = EvaluateObject();

                _Logger = null;
                _MonitorContext = null;

                return r;                
            }

            internal Object _EvaluatePreview(IMonitorContext monitor, ILogger logger)
            {
                if (BuildContext == null) throw new InvalidOperationException($"{this.GetType().Name} not initialized");                

                var previewContext = BuildContext.GetPreviewContext();
                if (previewContext == null) throw new InvalidOperationException($"{nameof(PreviewContext)} is Null");

                _MonitorContext = monitor;
                _Logger = logger;

                var r = EvaluatePreview(previewContext);

                _Logger = null;
                _MonitorContext = null;

                return r;
            }                

            #endregion            
        }                

        public abstract class ContentFilter<TValue> : ContentFilter
        {
            protected override object EvaluateObject() { return Evaluate(); }

            protected abstract TValue Evaluate();
        }
    }

      
}
