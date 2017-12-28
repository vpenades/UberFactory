using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberFactory
{
    public static partial class SDK
    {
        public abstract class ContentObject
        {
            #region lifecycle            

            internal void SetOwner(Object owner) { _Owner = owner; }

            internal void BeginProcessing(IFileManager fc, Func<Type, ContentObject> scr)
            {
                if (_FileIOContext != null) throw new InvalidOperationException("already initialized");

                _FileIOContext = fc ?? throw new ArgumentNullException(nameof(fc));
                _SharedContentResolver = scr ?? throw new ArgumentNullException(nameof(scr));
            }

            internal void EndProcessing()
            {
                _SharedContentResolver = null;
                _FileIOContext = null;
            }

            #endregion

            #region data

            // TODO: add a "EnqueueForDispose" or "AtExit" to store disposable objects that need to be disposed at the end of the processing pipeline

            private Object _Owner;
            private IFileManager _FileIOContext;
            private Func<Type, ContentObject> _SharedContentResolver;

            #endregion

            #region API

            public Object Owner => _Owner;

            public IFileManager BuildContext => _FileIOContext;

            public ImportContext GetImportContext(String relativePath)
            {
                var absolutePath = this.BuildContext.GetSourceAbsolutePath(relativePath);

                return _FileIOContext.GetImportContext(absolutePath);
            }

            public IEnumerable<ImportContext> GetImportContextBatch(String relativePath, String fileMask, bool allDirectories)
            {
                var absolutePath = this.BuildContext.GetSourceAbsolutePath(relativePath);

                return _FileIOContext.GetImportContextBatch(absolutePath, fileMask, allDirectories);
            }

            public ExportContext GetExportContext(String relativePath)
            {
                var absolutePath = this.BuildContext.GetTargetAbsolutePath(relativePath);

                return _FileIOContext.GetExportContext(absolutePath);
            }

            public SDK.ContentObject GetSharedSettings(Type t) { return _SharedContentResolver?.Invoke(t); }

            public T GetSharedSettings<T>() where T : ContentObject { return _SharedContentResolver?.Invoke(typeof(T)) as T; }

            #endregion   
        }
    }
}
