using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberFactory.Evaluation
{
    [System.Diagnostics.DebuggerDisplay("File Manager Input:{_SourceDirectoryAbsPath.ToString()} Output:{_TargetDirectoryAbsPath.ToString()}")]
    public class PipelineFileManager : SDK.IFileManager, IFileTracker
    {
        #region lifecycle

        public static PipelineFileManager Create(PathString inPath, PathString outPath, bool isSimulation)
        {
            if (!inPath.IsValidDirectoryAbsolutePath) throw new ArgumentException(nameof(inPath));
            if (!outPath.IsValidDirectoryAbsolutePath) throw new ArgumentException(nameof(outPath));

            return new PipelineFileManager(inPath, outPath, isSimulation);
        }

        private PipelineFileManager(PathString inPath, PathString outPath, bool isSimulation)
        {
            _SourceDirectoryAbsPath = inPath;
            _TargetDirectoryAbsPath = outPath;
            _IsSimulation = isSimulation;

            // _SimulationChecksum = System.Security.Cryptography.MD5.Create();
        }

        #endregion

        #region data

        private readonly PathString _SourceDirectoryAbsPath;
        private readonly PathString _TargetDirectoryAbsPath;

        private readonly bool _IsSimulation;

        // private readonly System.Security.Cryptography.MD5 _SimulationChecksum; // needs to be disposed
        
        private readonly HashSet<String> _InputFiles = new HashSet<String>(StringComparer.OrdinalIgnoreCase);   // files read from Source directory
        private readonly HashSet<String> _OutputFiles = new HashSet<String>(StringComparer.OrdinalIgnoreCase);  // files written to temporary directory

        #endregion

        #region properties        

        public IEnumerable<String> InputFiles => _InputFiles;

        public IEnumerable<String> OutputFiles => _OutputFiles;

        #endregion

        #region evaluation context for nodes

        protected bool IsValidInputFilePath(PathString path)
        {
            if (!path.IsValidAbsoluteFilePath) return false;

            if (!_TargetDirectoryAbsPath.IsEmpty && _TargetDirectoryAbsPath.Contains(path)) return false;

            return true;
        }

        protected bool IsValidOutputFilePath(PathString path)
        {
            if (!path.IsValidAbsoluteFilePath) return false;

            if (!_TargetDirectoryAbsPath.IsEmpty && !_TargetDirectoryAbsPath.Contains(path)) return false;

            return true;
        }        

        String SDK.IFileManager.GetRelativeToSource(String absFilePath) { return _SourceDirectoryAbsPath.MakeRelativePath(absFilePath); }

        String SDK.IFileManager.GetSourceAbsolutePath(String relFilePath) { return _SourceDirectoryAbsPath.MakeAbsolutePath(relFilePath); }

        String SDK.IFileManager.GetRelativeToTarget(String absFilePath) { return _TargetDirectoryAbsPath.MakeRelativePath(absFilePath); }

        String SDK.IFileManager.GetTargetAbsolutePath(String relFilePath) { return _TargetDirectoryAbsPath.MakeAbsolutePath(relFilePath); }

        SDK.ImportContext SDK.IFileManager.GetImportContext(string absolutePath)
        {
            var path = new PathString(absolutePath);

            if (!IsValidInputFilePath(path)) throw new ArgumentException($"Source file {absolutePath} points to a file in the output directory.", nameof(absolutePath));

            return _FileSystemImportContext.Create(path, this);
        }

        SDK.ExportContext SDK.IFileManager.GetExportContext(string absolutePath)
        {
            var path = new PathString(absolutePath);

            if (!IsValidOutputFilePath(path)) throw new ArgumentException($"Source file {absolutePath} points to a file in the output directory.", nameof(absolutePath));

            if (_IsSimulation) return _SimulateExportContext.Create(new PathString(absolutePath), _NotifyCreateFileSimulation, this);

            return _FileSystemExportContext.Create(path, _TargetDirectoryAbsPath, this);
        }

        IEnumerable<SDK.ImportContext> SDK.IFileManager.GetImportContextBatch(string absolutePath, string fileMask, bool allDirectories)
        {
            return _FileSystemImportContext.CreateBatch(new PathString(absolutePath), fileMask, allDirectories, IsValidInputFilePath, this);
        }

        SDK.PreviewContext SDK.IFileManager.GetPreviewContext()
        {
            return new _PreviewContext();
        }

        private void _NotifyCreateFileSimulation(string name, Byte[] data)
        {
            // var hash = _Checksum.ComputeHash(data);

            // var b64hash = Convert.ToBase64String(hash);

            // _SimulatedOutputFiles[name] = b64hash;
        }

        #endregion

        #region evaluation context for io trackers

        void IFileTracker.RegisterInputFile(string filePath, string parentFilePath)
        {
            _InputFiles.Add(filePath);
        }

        void IFileTracker.RegisterOutputFile(string filePath, string parentFilePath)
        {
            _OutputFiles.Add(filePath);
        }

        #endregion
    }
}
