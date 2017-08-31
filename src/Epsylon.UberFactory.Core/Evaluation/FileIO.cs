using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberFactory.Evaluation
{
    class _ImportContext : SDK.ImportContext
    {
        public static _ImportContext Create(Uri uri)
        {
            var path = new PathString(uri);

            var data = System.IO.File.ReadAllBytes(path);
            if (data == null) return null;

            return new _ImportContext(new PathString(uri), data);
        }

        private _ImportContext(PathString path, Byte[] data) { _SourcePath = path; }

        private readonly PathString _SourcePath;

        #pragma warning disable CS0672

        public override string FileName => _SourcePath.FileName;

        public override string FilePath => _SourcePath;

        #pragma warning restore CS0672

        public override System.IO.Stream OpenFile(string relativePath)
        {
            var newPath = _SourcePath.DirectoryPath.MakeAbsolutePath(relativePath);

            var data = System.IO.File.ReadAllBytes(newPath);

            return new System.IO.MemoryStream(data, false);
        }
    }

    class _ExportContext : SDK.ExportContext
    {
        public static _ExportContext Create(Uri uri, PathString outDir)
        {
            // TODO: ensure uri is within the specified target path

            var path = new PathString(uri);

            return new _ExportContext(path, outDir);
        }

        protected _ExportContext(PathString p, PathString o) { _TargetPath = p; _OutDir = o; }

        private readonly PathString _TargetPath;

        private readonly PathString _OutDir;

        public override string FileName => _TargetPath.FileName;

        #pragma warning disable CS0672

        public override string FilePath => _TargetPath;

        public override string OutputDirectory => _OutDir;

        #pragma warning restore CS0672

        public override System.IO.Stream OpenFile(string relativePath)
        {
            System.IO.Directory.CreateDirectory(_TargetPath.DirectoryPath);

            var newPath = _TargetPath.DirectoryPath.MakeAbsolutePath(relativePath);

            return System.IO.File.Create(newPath);
        }
    }

    /// <summary>
    /// export context that writes nothing, used for simulation and debug.
    /// </summary>
    sealed class _SimulateExportContext : _ExportContext
    {
        public new static _SimulateExportContext Create(Uri uri, PathString outDir)
        {
            // TODO: ensure uri is within the specified target path

            var path = new PathString(uri);

            return new _SimulateExportContext(path, outDir);
        }

        private _SimulateExportContext(PathString p, PathString o) : base(p, o) { }

        public override System.IO.Stream OpenFile(string relativePath)
        {
            // for writing very large files, using System.IO.MemoryStream would use a lot of RAM
            // alternatives:
            // - write a Stream object that updates Position and Length, but does nothing else
            // - write to a temporary file

            return new System.IO.MemoryStream();
        }
    }
}
