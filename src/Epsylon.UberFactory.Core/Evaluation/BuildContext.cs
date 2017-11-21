using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory.Evaluation
{
    [System.Diagnostics.DebuggerDisplay("BuildContext {ConfigurationJoined} Ready:{CanBuild} Input:{SourceDirectory.ToString()} Output:{TargetDirectory.ToString()} Errors:{CurrentError}")]
    public class BuildContext : SDK.IBuildContext
    {
        #region lifecycle        

        public static BuildContext Create(String cfg, PathString sd) { return Create(cfg, sd, PathString.Empty); }

        public static BuildContext Create(String cfg, PathString sd, PathString td)
        {
            var xcfg = cfg?.Split('.').ToArray();

            return new BuildContext(xcfg, sd, td);
        }

        public static BuildContext Create(BuildContext other, PathString td) { return new BuildContext(other.Configuration, other.SourceDirectory, td); }

        public static BuildContext CreateWithSimulatedOutput(String cfg, PathString sd)
        {
            var xcfg = cfg?.Split('.').ToArray();

            return new _TestBuildContext(xcfg, sd);
        }

        protected BuildContext(String[] cfg, PathString sd) : this(cfg, sd, PathString.Empty) { }

        private BuildContext(String[] cfg, PathString sd, PathString td)
        {
            _Configuration = cfg ?? (new string[0]);
            _SourceDirectoryAbsPath = sd;
            _TargetDirectoryAbsPath = td;

            if (string.IsNullOrWhiteSpace(_TargetDirectoryAbsPath))
            {
                var targetDir = System.IO.Path.Combine(SourceDirectory, "bin");

                if (IsValidConfiguration(Configuration)) targetDir = System.IO.Path.Combine(targetDir, string.Join(System.IO.Path.DirectorySeparatorChar.ToString(), Configuration));

                _TargetDirectoryAbsPath = new PathString(targetDir);
            }
        }

        #endregion

        #region data

        public const Char ConfigurationSeparator = '.';
            
        private readonly String[] _Configuration;
        private readonly PathString _SourceDirectoryAbsPath;
        // private readonly PathString _IntermediateDirectoryAbsPath; if input hasn't changed and intermediate exists, copy from intermediate to output without processing.
        private readonly PathString _TargetDirectoryAbsPath;

        #endregion

        #region properties

        public String[] Configuration       => _Configuration;        

        public string ConfigurationJoined   => string.Join(ConfigurationSeparator.ToString(), _Configuration);

        /// <summary>
        /// this is typically the directory where the project and source files are located
        /// </summary>
        public PathString SourceDirectory   => _SourceDirectoryAbsPath;

        /// <summary>
        /// this is typically the directory where generated files are saved
        /// </summary>
        public PathString TargetDirectory   => _TargetDirectoryAbsPath;

        public bool CanBuild                => CurrentError == null;

        public String CurrentError          => _GetCurrentError();

        #endregion

        #region API        

        public static bool IsValidConfiguration(params String[] cfg)
        {
            if (cfg == null || cfg.Length == 0) return false;
            return cfg.All(item => IsValidConfigurationNode(item) == null);
        }

        public static Exception IsValidConfigurationNode(String cfgNode)
        {
            if (string.IsNullOrWhiteSpace(cfgNode)) return new ArgumentNullException("Text is empty");

            if (cfgNode.Length > 64) return new ArgumentOutOfRangeException("Text is too long");

            if (cfgNode.Any(item => char.IsWhiteSpace(item))) return new ArgumentException("Text contains white spaces");

            if (cfgNode.Contains(ConfigurationSeparator)) return new ArgumentException($"Text contains invalid character '{ConfigurationSeparator}'");

            var invalidChars = System.IO.Path
                .GetInvalidFileNameChars()
                .Intersect(cfgNode.ToCharArray())
                .ToArray();

            if (invalidChars.Any()) return new ArgumentException($"Text contains invalid characters '{String.Join(" ",invalidChars)}'");            

            return null;
        }

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
        
        public String GetRelativeToSource(String absFilePath)      { return _SourceDirectoryAbsPath.MakeRelativePath(absFilePath); }
        
        public String GetSourceAbsolutePath(String relFilePath)    { return _SourceDirectoryAbsPath.MakeAbsolutePath(relFilePath); }
        
        public String GetRelativeToTarget(String absFilePath)      { return TargetDirectory.MakeRelativePath(absFilePath); }
        
        public String GetTargetAbsolutePath(String relFilePath)    { return TargetDirectory.MakeAbsolutePath(relFilePath); }

        public SDK.ImportContext GetImportContext(String absolutePath, SDK.ITaskFileIOTracker trackerContext)
        {
            var path = new PathString(absolutePath);

            if (!IsValidInputFilePath(path)) throw new ArgumentException($"Source file {absolutePath} points to a file in the output directory.", nameof(absolutePath));

            return _ImportContext.Create(path,trackerContext);
        }

        public IEnumerable<SDK.ImportContext> GetImportContextBatch(String absolutePath, String fileMask, bool allDirectories, SDK.ITaskFileIOTracker trackerContext)
        {
            return _ImportContext.CreateBatch(new PathString(absolutePath), fileMask, allDirectories, IsValidInputFilePath, trackerContext);
        }        

        public virtual SDK.ExportContext GetExportContext(String absolutePath, SDK.ITaskFileIOTracker trackerContext)
        {
            var path = new PathString(absolutePath);

            if (!IsValidOutputFilePath(path)) throw new ArgumentException($"Source file {absolutePath} points to a file in the output directory.", nameof(absolutePath));

            return _ExportContext.Create(path, _TargetDirectoryAbsPath, trackerContext);
        }        

        public SDK.PreviewContext GetPreviewContext()
        {
            return new _PreviewContext();
        }

        private String _GetCurrentError()
        {
            if (!IsValidConfiguration(_Configuration)) return "Invalid Configuration";

            if (!SourceDirectory.IsValidDirectoryAbsolutePath) return "Invalid Source Directory";

            if (!SourceDirectory.DirectoryExists) return "Source Directory doesn't exist";

            if (!TargetDirectory.IsValidDirectoryAbsolutePath) return "Invalid Target Directory";

            // TODO: have a "-DisableSourceTargetDirCollisionCheck" to allow the same (for self-generated code)
            if (string.Equals(SourceDirectory, TargetDirectory, StringComparison.CurrentCultureIgnoreCase)) return "Source and Target directories must be different";

            return null;
        }

        #endregion
    }    

    /// <summary>
    /// Build context designed for testing and validation
    /// It performs a full processing without actually writing anything to the hard drive
    /// </summary>
    sealed class _TestBuildContext : BuildContext
    {
        #region lifecycle

        internal _TestBuildContext(string[] cfg, PathString sd) : base(cfg,sd)
        {
            _Checksum = System.Security.Cryptography.MD5.Create();
        }

        #endregion

        #region data

        private readonly System.Security.Cryptography.MD5 _Checksum;

        private readonly Dictionary<string, string> _SimulatedOutputFiles = new Dictionary<string, string>();

        #endregion

        #region API

        public override SDK.ExportContext GetExportContext(String absolutePath, SDK.ITaskFileIOTracker trackerContext)
        {            
            return _SimulateExportContext.Create(new PathString(absolutePath), _NotifyCreateFileSimulation, trackerContext);           
        }

        private void _NotifyCreateFileSimulation(string name, Byte[] data)
        {
            var hash = _Checksum.ComputeHash(data);

            var b64hash = Convert.ToBase64String(hash);

            _SimulatedOutputFiles[name] = b64hash;
        }

        #endregion

    }
}
