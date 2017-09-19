using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Epsylon.UberFactory
{
    public static partial class SDK
    {
        #region Content Nodes

        public abstract class ContentObject
        {
            #region data

            // TODO: add a "EnqueueForDispose" or "AtExit" to store disposable objects that need to be disposed at the end of the processing pipeline

            private IBuildContext _BuildContext;
            private Func<Type, ContentObject> _SharedContentResolver;

            #endregion

            #region API     
            
            internal void Setup(IBuildContext bc, Func<Type, ContentObject> scr)
            {
                if (_BuildContext != null || _SharedContentResolver != null) throw new InvalidOperationException("already initialized");

                _BuildContext = bc ?? throw new ArgumentNullException(nameof(bc));
                _SharedContentResolver = scr ?? throw new ArgumentNullException(nameof(scr));
            }

            public IBuildContext BuildContext => _BuildContext;

            public SDK.ContentObject GetSharedSettings(Type t) { return _SharedContentResolver?.Invoke(t); }

            public T GetSharedSettings<T>() where T : ContentObject { return _SharedContentResolver?.Invoke(typeof(T)) as T; }

            #endregion   
        }

        public abstract class ContentFilter : ContentObject // ContentEvaluator
        {
            #region lifecycle

            public ContentFilter() { }

            #endregion

            #region data

            // TODO: add a "EnqueueForDispose" or "AtExit" to store disposable objects that need to be disposed at the end of the processing pipeline            

            internal IMonitorContext _MonitorContext;            

            #endregion            

            #region API

            protected bool IsCancelled { get { return _MonitorContext.IsCancelRequested; } }

            protected bool IsRunning { get { return !IsCancelled; } }

            protected void CheckCancelation() { if (IsCancelled) throw new OperationCanceledException(); }            

            protected void SetProgressPercent(int percent) { SetProgress((float)percent / 100.0f); }

            protected void SetProgress(float value) { CheckCancelation(); _MonitorContext.Report(value); }

            public void LogTrace(string message) { _MonitorContext.LogTrace(this.GetType().Name,message); }
            public void LogDebug(string message) { _MonitorContext.LogDebug(this.GetType().Name, message); }
            public void LogInfo(string message) { _MonitorContext.LogInfo(this.GetType().Name, message); }
            public void LogWarning(string message) { _MonitorContext.LogWarning(this.GetType().Name, message); }
            public void LogError(string message) { _MonitorContext.LogError(this.GetType().Name, message); }
            public void LogCritical(string message) { _MonitorContext.LogCritical(this.GetType().Name, message); }

            


            internal Object _Evaluate(IMonitorContext monitor)
            {
                return _Evaluate(EvaluateObject, monitor);
            }

            internal Object _Preview(IMonitorContext monitor)
            {
                return _Evaluate(PreviewObject, monitor);
            }

            internal Object _Evaluate(Func<Object> evaluator, IMonitorContext monitor)
            {
                if (BuildContext == null) throw new InvalidOperationException(this.GetType().Name + " not initialized");

                this._MonitorContext = monitor;                

                var r = evaluator();
                
                this._MonitorContext = null;

                return r;
            }

            // preview evaluation is expected to be much faster and simplified, for heavy filters, it is recomended to use a simplified processing for preview
            protected virtual Object PreviewObject() { return EvaluateObject(); }

            protected abstract Object EvaluateObject();

            #endregion            
        }                

        public abstract class ContentFilter<TValue> : ContentFilter
        {
            protected override object EvaluateObject() { return Evaluate(); }

            protected abstract TValue Evaluate();
        }
        
        #endregion
    }

      
}
