﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Epsylon.UberFactory.Evaluation
{
    // Note: if at some point, a ContentFilter requires to read/write large files (like Video)
    // it can be done with a custom attribute defined in the filter, so, the BuildContext can
    // create special importer/exporters
    
    public interface IPreviewResult
    {
        string FileName { get; }

        IReadOnlyDictionary<String, Byte[]> Content { get; }
    }

    class _ImportContext : SDK.ImportContext
    {
        #region lifecycle

        public static _ImportContext Create(Uri uri, SDK.ITaskFileIOTracker tc)
        {
            var path = new PathString(uri);

            return new _ImportContext(new PathString(uri), tc);
        }

        public static IEnumerable<_ImportContext> CreateBatch(Uri uri, bool recurse, SDK.ITaskFileIOTracker tc)
        {
            var dir = new PathString(uri);

            // dir.IsValidDirectoryAbsolutePath;

            var files = System.IO.Directory.GetFiles(dir, "*.*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            foreach(var f in files)
            {
                yield return Create(new Uri(f, UriKind.Absolute), tc);
            }
        }

        private _ImportContext(PathString path,  SDK.ITaskFileIOTracker tc) :base(tc)
        {
            System.Diagnostics.Debug.Assert(path.IsValidAbsoluteFilePath);

            _SourcePath = path;
        }

        #endregion

        #region data

        private readonly PathString _SourcePath;

        #endregion

        #region API

        #pragma warning disable CS0672

        public override string FileName => _SourcePath.FileName;

        public override string FilePath => _SourcePath;

        #pragma warning restore CS0672

        protected override System.IO.Stream OpenFileCore(string relativePath)
        {
            var newPath = _SourcePath.DirectoryPath.MakeAbsolutePath(relativePath);

            var data = System.IO.File.ReadAllBytes(newPath);

            return new System.IO.MemoryStream(data, false);
        }

        #endregion
    }

    class _ExportContext : SDK.ExportContext
    {
        #region lifecycle

        public static _ExportContext Create(Uri uri, PathString outDir, SDK.ITaskFileIOTracker tc)
        {
            // TODO: ensure uri is within the specified target path

            var path = new PathString(uri);

            return new _ExportContext(path, outDir, tc);
        }

        protected _ExportContext(PathString path, PathString o, SDK.ITaskFileIOTracker tc) : base(tc)
        {
            System.Diagnostics.Debug.Assert(path.IsValidAbsoluteFilePath);

            _TargetPath = path;
            _OutDir = o;
        }

        #endregion

        #region data

        private readonly HashSet<string> _OutputFiles = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        private readonly PathString _TargetPath;

        private readonly PathString _OutDir;

        #endregion

        #region API

        public override string FileName => _TargetPath.FileName;

        #pragma warning disable CS0672

        public override string FilePath => _TargetPath;

        public override string OutputDirectory => _OutDir;

        #pragma warning restore CS0672

        protected override System.IO.Stream OpenFileCore(string relativePath)
        {
            if (_OutputFiles.Contains(relativePath)) throw new ArgumentException($"{relativePath} already written", nameof(relativePath));

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
    sealed class _SimulateExportContext : SDK.ExportContext
    {
        #region lifecycle

        public static _SimulateExportContext Create(Uri uri, Action<string, Byte[]> fileCreationNotifier, SDK.ITaskFileIOTracker tc)
        {
            // TODO: ensure uri is within the specified target path

            var path = new PathString(uri);

            return new _SimulateExportContext(path, fileCreationNotifier, tc);
        }

        private _SimulateExportContext(PathString p, Action<string, Byte[]> fileCreationNotifier, SDK.ITaskFileIOTracker tc) : base(tc)
        {
            _TargetPath = p;
            _FileCreationNotifier = fileCreationNotifier;
        }

        #endregion

        #region data

        private readonly PathString _TargetPath;

        private readonly Action<string, Byte[]> _FileCreationNotifier;

        #endregion

        #region API

        public override string FileName => _TargetPath.FileName;

        #pragma warning disable CS0672

        public override string FilePath => throw new NotSupportedException("Write to file not supported.");

        public override string OutputDirectory => throw new NotSupportedException("Write to file not supported.");

        #pragma warning restore CS0672

        protected override Stream OpenFileCore(string relativePath)
        {
            // for writing very large files, using System.IO.MemoryStream would use a lot of RAM
            // alternatives:
            // - write a Stream object that updates Position and Length, but does nothing else
            // - write to a temporary file

            return new _MemoryStream(relativePath, _FileCreationNotifier);
        }

        #endregion
    }

    class _DictionaryExportContext : SDK.ExportContext , IPreviewResult
    {
        #region lifecycle

        public static _DictionaryExportContext Create(string fileName, SDK.ITaskFileIOTracker tc)
        {
            var fp = new PathString(fileName);
            if (!fp.IsValidRelativeFilePath) return null;            

            return new _DictionaryExportContext(fileName,tc);
        }

        private _DictionaryExportContext(string fileName, SDK.ITaskFileIOTracker tc) : base(tc)
        {
            _DefaultFileName = fileName;
        }

        #endregion

        #region data

        private string _DefaultFileName;
        private readonly Dictionary<String, Byte[]> _Files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region API

        public IReadOnlyDictionary<String, Byte[]> Content => _Files;

        public override string FileName => _DefaultFileName;

        #pragma warning disable CS0672

        public override string FilePath => throw new NotSupportedException("Write to file not supported.");

        public override string OutputDirectory => throw new NotSupportedException("Write to file not supported.");

        #pragma warning restore CS0672

        protected override Stream OpenFileCore(string localName)
        {
            return new _MemoryStream(localName, (key, val) => _Files[key] = val);
        }

        #endregion        
    }

    class _DictionaryImportContext : SDK.ImportContext
    {
        #region lifecycle

        public static _DictionaryImportContext Create(IConvertible value)
        {
            var text = value.ToString();
            var data = Encoding.UTF8.GetBytes(text);

            var dict = new Dictionary<string, byte[]>
            {
                ["preview.txt"] = data
            };

            return Create(dict, "preview.txt", null);
        }

        public static _DictionaryImportContext Create(IReadOnlyDictionary<string,Byte[]> files, string fileName, SDK.ITaskFileIOTracker tc)
        {
            var fp = new PathString(fileName);
            if (!fp.IsValidRelativeFilePath) return null;

            return new _DictionaryImportContext(files, fileName,tc);
        }

        private _DictionaryImportContext(IReadOnlyDictionary<string, Byte[]> files, string fileName, SDK.ITaskFileIOTracker tc) :base(tc)
        {
            _DefaultFileName = fileName;
            _Files = files;
        }

        #endregion

        #region data

        private string _DefaultFileName;
        private readonly IReadOnlyDictionary<String, Byte[]> _Files;

        #endregion

        #region API

        #pragma warning disable CS0672

        public override string FileName => throw new NotSupportedException();

        public override string FilePath => throw new NotSupportedException();

        #pragma warning restore CS0672

        protected override Stream OpenFileCore(string relativePath)
        {
            if (!_Files.TryGetValue(relativePath, out byte[] data)) return null;

            return new MemoryStream(data);
        }

        #endregion
    }

    class _PreviewContext : SDK.PreviewContext
    {
        public override SDK.ExportContext CreateMemoryFile(string fileName)
        {
            return _DictionaryExportContext.Create(fileName, null);
        }
    }

    class _MemoryStream : MemoryStream
    {
        public _MemoryStream(string name, Action<string, Byte[]> onClosingFile)
        {
            _FileName = name;
            _OnClosingFile = onClosingFile;
        }

        private readonly string _FileName;
        private readonly Action<string, Byte[]> _OnClosingFile;

        protected override void Dispose(bool disposing)
        {
            if (disposing && _OnClosingFile != null)
            {
                _OnClosingFile.Invoke(_FileName, this.ToArray());
            }

            base.Dispose(disposing);
        }
    }


}
