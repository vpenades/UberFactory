using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Epsylon.UberFactory
{
    public static partial class SDK
    {
        #region Content Nodes

        /// <summary>
        /// Used by the evaluator to retrieve all the source files used by this node
        /// </summary>
        public interface IProjectFiles
        {
            IEnumerable<String> GetProjectFiles();
        }

        public abstract class ContentFilter
        {
            #region lifecycle

            public ContentFilter() { }

            #endregion

            #region data

            // TODO: add a "EnqueueForDispose" or "AtExit" to store disposable objects that need to be disposed at the end of the processing pipeline
            
            internal IBuildContext _BuildContext;

            internal IMonitorContext _MonitorContext;

            #endregion            

            #region API            

            public IBuildContext BuildContext => _BuildContext;

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
