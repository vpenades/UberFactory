using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    using MSLOGGER = ILoggerFactory;



    public sealed class BuildContext : SDK.IBuildContext
    {
        #region lifecycle

        public static BuildContext Create(BuildContext other, PathString td)
        {
            return new BuildContext(other.Configuration, other.SourceDirectory, td, other._Logger);
        }

        public static BuildContext Create(string cfg, PathString sd) { return Create(cfg, sd, PathString.Empty); }

        public static BuildContext Create(string cfg, PathString sd, PathString td)
        {
            var xcfg = cfg?.Split('.').ToArray();

            return new BuildContext(xcfg, sd, td, null);
        }            

        private BuildContext(string[] cfg, PathString sd, PathString td, MSLOGGER logger)
        {
            _Configuration = cfg ?? (new string[0]);
            _SourceDirectoryAbsPath = sd;
            _TargetDirectoryAbsPath = td;
            _Logger = logger;
        }

        #endregion

        #region data

        public const Char ConfigurationSeparator = '.';            
            
        private readonly String[] _Configuration;            
        private readonly PathString _SourceDirectoryAbsPath;
        private readonly PathString _TargetDirectoryAbsPath;

        private MSLOGGER _Logger;

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

        public String CurrentError
        {
            get
            {
                if (!IsValidConfiguration(_Configuration)) return "Invalid Configuration";

                if (!_SourceDirectoryAbsPath.IsValidDirectoryAbsolutePath) return "Invalid Source Directory";
                if (!_TargetDirectoryAbsPath.IsValidDirectoryAbsolutePath) return "Invalid Target Directory";

                // TODO: have a "-DisableSourceTargetDirCollisionCheck" to allow the same (for self-generated code)
                if (string.Equals(_SourceDirectoryAbsPath, _TargetDirectoryAbsPath, StringComparison.CurrentCultureIgnoreCase)) return "Source and Target directories must be different";                    

                if (!System.IO.Directory.Exists(_SourceDirectoryAbsPath)) return "Source Directory doesn't exist";

                return null;
            }
        }

        #endregion

        #region API

        public void SetLogger(MSLOGGER logger) { _Logger = logger; }

        public static bool IsValidConfiguration(params string[] cfg)
        {
            if (cfg == null || cfg.Length == 0) return false;
            return cfg.All(item => IsValidConfigurationNode(item));
        }

        public static bool IsValidConfigurationNode(string cfgNode)
        {
            if (string.IsNullOrWhiteSpace(cfgNode)) return false;

            if (cfgNode.Contains(ConfigurationSeparator)) return false;

            if (cfgNode.Any(item => char.IsWhiteSpace(item))) return false;

            return true;
        }        

        public PathString MakeRelativeToSource(string absFilePath) { return _SourceDirectoryAbsPath.MakeRelativePath(absFilePath); }

        public PathString MakeRelativeToTarget(string absFilePath) { return _TargetDirectoryAbsPath.MakeRelativePath(absFilePath); }

        public PathString MakeAbsoluteToSource(string relFilePath) { return _SourceDirectoryAbsPath.MakeAbsolutePath(relFilePath); }

        public PathString MakeAbsoluteToTarget(string relFilePath) { return _TargetDirectoryAbsPath.MakeAbsolutePath(relFilePath); }


        private ILogger CreateLogger(string name)
        {
            return _Logger == null ? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance : _Logger.CreateLogger(name);            
        }

        public void LogTrace(string name, string message) { CreateLogger(name).LogTrace(message); }
        public void LogDebug(string name, string message) { CreateLogger(name).LogDebug(message); }
        public void LogInfo(string name, string message) { CreateLogger(name).LogInformation(message); }
        public void LogWarning(string name, string message) { CreateLogger(name).LogWarning(message); }
        public void LogError(string name, string message) { CreateLogger(name).LogError(message); }
        public void LogCritical(string name, string message) { CreateLogger(name).LogCritical(message); }

        #endregion

        #region interface              

        public string GetRelativeToSource(Uri absoluteUri)
        {
            return _SourceDirectoryAbsPath.MakeRelativePath(absoluteUri.ToFriendlySystemPath());
        }

        public string GetRelativeToTarget(Uri absoluteUri)
        {
            return _TargetDirectoryAbsPath.MakeRelativePath(absoluteUri.ToFriendlySystemPath());
        }

        public Uri GetSourceAbsoluteUri(string relativePath)
        {
            var absPath = _SourceDirectoryAbsPath.MakeAbsolutePath(relativePath);

            return new Uri(absPath, UriKind.Absolute);
        }

        public Uri GetTargetAbsoluteUri(string relativePath)
        {
            var absPath = _TargetDirectoryAbsPath.MakeAbsolutePath(relativePath);

            return new Uri(absPath, UriKind.Absolute);
        }       

        public SDK.ImportContext GetImportContext(Uri absoluteUri)
        {
            return _ImportContext.Create(absoluteUri);
        }

        public SDK.ExportContext GetExportContext(Uri absoluteUri)
        {
            return _ExportContext.Create(absoluteUri, _TargetDirectoryAbsPath);
        }

        #endregion
    }   


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

        public override string FileName => _SourcePath.FileName;        

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

        private _ExportContext(PathString p, PathString o) { _TargetPath = p; _OutDir = o; }        

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
}
