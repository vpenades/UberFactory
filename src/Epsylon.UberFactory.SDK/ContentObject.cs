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

            internal void BeginProcessing(IFileManager bc, Func<Type, ContentObject> scr)
            {
                if (_BuildContext != null) throw new InvalidOperationException("already initialized");

                _BuildContext = bc ?? throw new ArgumentNullException(nameof(bc));
                _SharedContentResolver = scr ?? throw new ArgumentNullException(nameof(scr));
            }

            internal void EndProcessing()
            {
                _SharedContentResolver = null;
                _BuildContext = null;
            }

            #endregion

            #region data

            // TODO: add a "EnqueueForDispose" or "AtExit" to store disposable objects that need to be disposed at the end of the processing pipeline

            private IFileManager _BuildContext;
            private Func<Type, ContentObject> _SharedContentResolver;

            #endregion

            #region API           

            public IFileManager BuildContext => _BuildContext;

            public ImportContext GetImportContext(String relativePath)
            {
                var absolutePath = this.BuildContext.GetSourceAbsolutePath(relativePath);

                return _BuildContext.GetImportContext(absolutePath);
            }

            public IEnumerable<ImportContext> GetImportContextBatch(String relativePath, String fileMask, bool allDirectories)
            {
                var absolutePath = this.BuildContext.GetSourceAbsolutePath(relativePath);

                return _BuildContext.GetImportContextBatch(absolutePath, fileMask, allDirectories);
            }

            public ExportContext GetExportContext(String relativePath)
            {
                var absolutePath = this.BuildContext.GetTargetAbsolutePath(relativePath);

                return _BuildContext.GetExportContext(absolutePath);
            }

            public SDK.ContentObject GetSharedSettings(Type t) { return _SharedContentResolver?.Invoke(t); }

            public T GetSharedSettings<T>() where T : ContentObject { return _SharedContentResolver?.Invoke(typeof(T)) as T; }

            #endregion   
        }
    }
}
