using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Epsylon.UberFactory.Evaluation
{
    [System.Diagnostics.DebuggerDisplay("Import Context for: {_SourcePath.ToString()}")]
    class _FileSystemImportContext : SDK.ImportContextEx
    {
        #region lifecycle

        public static IEnumerable<_FileSystemImportContext> CreateBatch(PathString dir, string fileMask, bool allDirectories, Func<PathString, bool> pathValidator, IFileTracker tc)
        {
            var files = System.IO.Directory.GetFiles(dir, fileMask, allDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            foreach (var f in files)
            {
                var fp = new PathString(f);

                if (!pathValidator(fp)) continue;

                yield return Create(fp, tc);
            }
        }

        public static _FileSystemImportContext Create(PathString path, IFileTracker tc)
        {
            if (!path.IsValidAbsoluteFilePath) throw new ArgumentException(nameof(path));

            return new _FileSystemImportContext(path, tc);
        }        

        private _FileSystemImportContext(PathString path, IFileTracker tc)
        {
            System.Diagnostics.Debug.Assert(path.IsValidAbsoluteFilePath);
            
            _SourcePath = path;
            _FileTracker = tc;
        }

        #endregion

        #region data        

        private readonly PathString _SourcePath;
        private readonly IFileTracker _FileTracker;

        #endregion

        #region API

        public override string FileName => _SourcePath.FileName;

        public override string FilePath => _SourcePath;

        protected override Stream OpenFileCore(string relativePath)
        {
            var parentFile = string.Equals(relativePath, this.FileName, StringComparison.OrdinalIgnoreCase) ? null : this.FileName;

            _FileTracker?.RegisterInputFile(relativePath, parentFile);

            var newPath = _SourcePath.DirectoryPath.MakeAbsolutePath(relativePath);

            // TODO: based on the type of file and size, we can read the memory, or open the file directly

            var data = File.ReadAllBytes(newPath);

            return new MemoryStream(data, false);
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Import Context for: {_DefaultFileName}")]
    class _MemoryImportContext : SDK.ImportContext
    {
        #region lifecycle

        public static _MemoryImportContext Create(IReadOnlyDictionary<string, Byte[]> files, string fileName)
        {
            var fp = new PathString(fileName);
            if (!fp.IsValidRelativeFilePath) return null;

            return new _MemoryImportContext(files, fileName);
        }

        public static _MemoryImportContext CreatePreview(IConvertible value)
        {
            var text = value.ToString();
            var data = Encoding.UTF8.GetBytes(text);

            var dict = new Dictionary<string, byte[]>
            {
                ["preview.txt"] = data
            };

            return Create(dict, "preview.txt");
        }        

        private _MemoryImportContext(IReadOnlyDictionary<string, Byte[]> files, string fileName)
        {
            _DefaultFileName = fileName;
            _Files = files;
        }

        #endregion

        #region data

        private string _DefaultFileName;
        private readonly IReadOnlyDictionary<String, Byte[]> _Files;

        public override string FileName => _DefaultFileName;

        #endregion

        #region API        

        protected override Stream OpenFileCore(string relativePath)
        {
            if (!_Files.TryGetValue(relativePath, out byte[] data)) return null;

            return new MemoryStream(data);
        }

        #endregion
    }
}
