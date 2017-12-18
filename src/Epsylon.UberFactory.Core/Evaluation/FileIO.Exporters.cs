using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Epsylon.UberFactory.Evaluation
{
    abstract class _ExportContextBase : SDK.ExportContextEx
    {
        #region lifecycle        

        protected _ExportContextBase(PathString path, IFileTracker tc)
        {
            _FileTracker = tc;
        }

        #endregion

        #region data

        protected readonly PathString _TargetPath;
        private readonly IFileTracker _FileTracker;

        #endregion

        #region properties

        public override string FileName => _TargetPath.FileName;

        public override string FilePath => _TargetPath;

        #endregion

        #region API

        protected void RegisterOpenFile(string relativePath)
        {
            var parentFile = string.Equals(relativePath, this.FileName, StringComparison.OrdinalIgnoreCase) ? null : this.FileName;

            _FileTracker?.RegisterOutputFile(relativePath, parentFile);            
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Export Context for: {_TargetPath.ToString()}")]
    class _FileSystemExportContext : _ExportContextBase
    {
        #region lifecycle

        public static _FileSystemExportContext Create(PathString path, PathString outDir, IFileTracker tc)
        {
            if (!path.IsValidAbsoluteFilePath) throw new ArgumentException(nameof(path));

            return new _FileSystemExportContext(path, outDir, tc);
        }

        protected _FileSystemExportContext(PathString path, PathString o, IFileTracker tc) : base(path, tc)
        {
            System.Diagnostics.Debug.Assert(path.IsValidAbsoluteFilePath);
            
            _OutDir = o;
        }

        #endregion

        #region data

        private readonly HashSet<string> _OutputFiles = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);        

        private readonly PathString _OutDir;

        #endregion

        #region API        

        public override string OutputDirectory => _OutDir;

        protected override System.IO.Stream OpenFileCore(string relativePath)
        {
            if (_OutputFiles.Contains(relativePath)) throw new ArgumentException($"{relativePath} already written", nameof(relativePath));

            this.RegisterOpenFile(relativePath);

            _OutputFiles.Add(relativePath);

            System.IO.Directory.CreateDirectory(_TargetPath.DirectoryPath);

            var newPath = _TargetPath.DirectoryPath.MakeAbsolutePath(relativePath);

            return System.IO.File.Create(newPath);
        }

        #endregion
    }

    /// <summary>
    /// export context that writes nothing, used for simulation and debug.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Export Context for: {_TargetPath.ToString()}")]
    sealed class _SimulateExportContext : _ExportContextBase
    {
        #region lifecycle

        public static _SimulateExportContext Create(PathString path, Action<string, Byte[]> fileCreationNotifier, IFileTracker tc)
        {
            // TODO: ensure path is within the specified target path            

            return new _SimulateExportContext(path, fileCreationNotifier, tc);
        }

        private _SimulateExportContext(PathString p, Action<string, Byte[]> fileCreationNotifier, IFileTracker tc) : base(p, tc)
        {            
            _FileCreationNotifier = fileCreationNotifier;
        }

        #endregion

        #region data        

        private readonly Action<string, Byte[]> _FileCreationNotifier;

        #endregion

        #region API        

        public override string OutputDirectory => throw new NotSupportedException("Write to file not supported.");

        protected override Stream OpenFileCore(string relativePath)
        {
            this.RegisterOpenFile(relativePath);

            // for writing very large files, using System.IO.MemoryStream would use a lot of RAM
            // alternatives:
            // - write a Stream object that updates Position and Length, but does nothing else
            // - write to a temporary file

            return new _MemoryStream(relativePath, _FileCreationNotifier);
        }

        #endregion
    }


    /// <summary>
    /// export context that stores everything in memory, used for preview and testing
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Export Context for: {_DefaultFileName}")]
    class _MemoryExportContext : SDK.ExportContext, IPreviewResult
    {
        #region lifecycle

        public static _MemoryExportContext Create(String fileName)
        {
            var fp = new PathString(fileName);
            if (!fp.IsValidRelativeFilePath) return null;

            return new _MemoryExportContext(fp);
        }

        private _MemoryExportContext(String fileName)
        {
            _DefaultFileName = fileName;
        }

        #endregion

        #region data

        private readonly String _DefaultFileName;
        private readonly Dictionary<String, Byte[]> _Files = new Dictionary<String, byte[]>(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region API

        public IReadOnlyDictionary<String, Byte[]> Content => _Files;

        public override String FileName => _DefaultFileName;

        protected override Stream OpenFileCore(string localName)
        {            
            return new _MemoryStream(localName, (key, val) => _Files[key] = val);
        }

        #endregion        
    }
}
