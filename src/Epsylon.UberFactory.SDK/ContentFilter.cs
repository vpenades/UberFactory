﻿using System;
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
            #region lifecycle

            internal void _Initialize(IBuildContext bc, Func<Type, ContentObject> scr, ITaskFileIOTracker trackerContext)
            {
                if (_BuildContext != null || _SharedContentResolver != null) throw new InvalidOperationException("already initialized");
                
                _BuildContext = bc ?? throw new ArgumentNullException(nameof(bc));
                _SharedContentResolver = scr ?? throw new ArgumentNullException(nameof(scr));

                _TrackerContext = trackerContext;
            }

            #endregion

            #region data

            // TODO: add a "EnqueueForDispose" or "AtExit" to store disposable objects that need to be disposed at the end of the processing pipeline

            private ITaskFileIOTracker _TrackerContext;
            private IBuildContext _BuildContext;            
            private Func<Type, ContentObject> _SharedContentResolver;

            #endregion

            #region API     

            public IBuildContext BuildContext => _BuildContext;            

            public ImportContext GetImportContext(String absoluteUri) { return _BuildContext.GetImportContext(absoluteUri, _TrackerContext); }
            
            public ExportContext GetExportContext(String absoluteUri) { return _BuildContext.GetExportContext(absoluteUri, _TrackerContext); }

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

            #region user's API

            protected bool IsCancelled      => _MonitorContext.IsCancelRequested;

            protected bool IsRunning        => !IsCancelled;

            protected void CheckCancelation() { if (IsCancelled) throw new OperationCanceledException(); }            

            protected void SetProgressPercent(int percent) { SetProgress((float)percent / 100.0f); }

            protected void SetProgress(float value) { CheckCancelation(); _MonitorContext.Report(value); }

            public void LogTrace(string message) { _MonitorContext.LogTrace(this.GetType().Name,message); }
            public void LogDebug(string message) { _MonitorContext.LogDebug(this.GetType().Name, message); }
            public void LogInfo(string message) { _MonitorContext.LogInfo(this.GetType().Name, message); }
            public void LogWarning(string message) { _MonitorContext.LogWarning(this.GetType().Name, message); }
            public void LogError(string message) { _MonitorContext.LogError(this.GetType().Name, message); }
            public void LogCritical(string message) { _MonitorContext.LogCritical(this.GetType().Name, message); }

            protected abstract Object EvaluateObject();

            protected virtual Object EvaluatePreview(PreviewContext previewContext) { return EvaluateObject(); }

            #endregion

            #region internals

            internal Object _EvaluateObject(IMonitorContext monitor)
            {
                if (BuildContext == null) throw new InvalidOperationException($"{this.GetType().Name} not initialized");

                _MonitorContext = monitor;

                var r = EvaluateObject();

                _MonitorContext = null;

                return r;                
            }

            internal Object _EvaluatePreview(IMonitorContext monitor)
            {
                if (BuildContext == null) throw new InvalidOperationException($"{this.GetType().Name} not initialized");                

                var previewContext = BuildContext.GetPreviewContext();
                if (previewContext == null) throw new InvalidOperationException($"{nameof(PreviewContext)} is Null");

                _MonitorContext = monitor;

                var r = EvaluatePreview(previewContext);

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
        
        #endregion
    }

      
}
