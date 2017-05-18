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

            private System.Threading.CancellationToken _CancellationToken;

            private IProgress<float> _Progress;

            #endregion            

            #region API            

            public IBuildContext BuildContext => _BuildContext;            

            protected bool IsCancelled { get { return _CancellationToken.IsCancellationRequested; } }

            protected bool IsRunning { get { return !IsCancelled; } }

            protected void CheckCancelation() { if (IsCancelled) throw new OperationCanceledException(); }            

            protected void SetProgressPercent(int percent) { SetProgress((float)percent / 100.0f); }

            protected void SetProgress(float value) { CheckCancelation(); _Progress.Report(value); }

            public void LogTrace(string message) { BuildContext.LogTrace(this.GetType().Name,message); }
            public void LogDebug(string message) { BuildContext.LogDebug(this.GetType().Name, message); }
            public void LogInfo(string message) { BuildContext.LogInfo(this.GetType().Name, message); }
            public void LogWarning(string message) { BuildContext.LogWarning(this.GetType().Name, message); }
            public void LogError(string message) { BuildContext.LogError(this.GetType().Name, message); }
            public void LogCritical(string message) { BuildContext.LogCritical(this.GetType().Name, message); }



            internal Object _Evaluate(System.Threading.CancellationToken cancelToken, IProgress<float> progress)
            {
                LogTrace("Evaluating...");

                return _Evaluate(EvaluateObject, cancelToken, progress);
            }

            internal Object _Preview(System.Threading.CancellationToken cancelToken, IProgress<float> progress)
            {
                LogTrace("Previewing...");

                return _Evaluate(PreviewObject, cancelToken, progress);
            }


            internal Object _Evaluate(Func<Object> evaluator, System.Threading.CancellationToken cancelToken, IProgress<float> progress)
            {
                this._CancellationToken = cancelToken;
                this._Progress = progress;

                var r = evaluator();

                this._Progress = null;
                this._CancellationToken = System.Threading.CancellationToken.None;

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
