using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory.Evaluation
{
    using MSLOGGER = ILoggerFactory;

    public sealed class MonitorContext : SDK.IMonitorContext
    {
        #region lifecycle

        private MonitorContext() { }

        public static MonitorContext CreateNull()
        {
            return new MonitorContext()
            {
                _Cancelator = System.Threading.CancellationToken.None,
                _Progress = null,
                _Logger = null
            };
        }

        public static MonitorContext Create(MSLOGGER logger, System.Threading.CancellationToken cancelToken, IProgress<float> progressAgent)
        {
            return new MonitorContext()
            {
                _Cancelator = cancelToken,
                _Progress = progressAgent,
                _Logger = logger
            };
        }

        public SDK.IMonitorContext GetProgressPart(int part, int total)
        {
            return new MonitorContext()
            {
                _Cancelator = this._Cancelator,
                _Progress = this._Progress.CreatePart(part, total),
                _Logger = this._Logger
            };
        }

        #endregion

        #region data        

        private System.Threading.CancellationToken _Cancelator;
        private IProgress<float> _Progress;
        private MSLOGGER _Logger;

        #endregion

        #region API

        public void SetLogger(MSLOGGER logger) { _Logger = logger; }

        public bool IsCancelRequested => _Cancelator.IsCancellationRequested;

        public void Report(float value)
        {
            if (_Progress == null) return;
            _Progress.Report(value.Clamp(0, 1));
        }

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
    }

    public sealed class BuildContext : SDK.IBuildContext
    {
        #region lifecycle

        public static BuildContext Create(BuildContext other, PathString td)
        {
            return new BuildContext(other.Configuration, other.SourceDirectory, td, other._SimulateOutput);
        }

        public static BuildContext Create(string cfg, PathString sd) { return Create(cfg, sd, PathString.Empty); }

        public static BuildContext Create(string cfg, PathString sd, PathString td)
        {
            var xcfg = cfg?.Split('.').ToArray();

            return new BuildContext(xcfg, sd, td, false);
        }

        public static BuildContext CreateSimulator(string cfg, PathString sd)
        {
            var xcfg = cfg?.Split('.').ToArray();

            return new BuildContext(xcfg, sd, PathString.Empty, true);
        }

        private BuildContext(string[] cfg, PathString sd, PathString td, bool simulateOutput)
        {
            _Configuration = cfg ?? (new string[0]);
            _SourceDirectoryAbsPath = sd;
            _TargetDirectoryAbsPath = td;
            _SimulateOutput = simulateOutput;

            if (!_SimulateOutput)
            {
                if (string.IsNullOrWhiteSpace(_TargetDirectoryAbsPath))
                {
                    var targetDir = System.IO.Path.Combine(_SourceDirectoryAbsPath, "bin");

                    if (IsValidConfiguration(_Configuration)) targetDir = System.IO.Path.Combine(targetDir, string.Join(System.IO.Path.DirectorySeparatorChar.ToString(), _Configuration));

                    _TargetDirectoryAbsPath = new PathString(targetDir);
                }
            }
            else
            {
                _Checksum = System.Security.Cryptography.MD5.Create();
            }
        }

        #endregion

        #region data

        public const Char ConfigurationSeparator = '.';
            
        private readonly String[] _Configuration;
        private readonly PathString _SourceDirectoryAbsPath;
        private readonly PathString _TargetDirectoryAbsPath;

        private readonly bool _SimulateOutput;
        private readonly System.Security.Cryptography.MD5 _Checksum;

        private readonly Dictionary<string, string> _SimulatedOutputFiles = new Dictionary<string, string>();

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

                if (!_SimulateOutput) // if we're simulating output, we don't require to test it
                {
                    if (!_TargetDirectoryAbsPath.IsValidDirectoryAbsolutePath) return "Invalid Target Directory";

                    // TODO: have a "-DisableSourceTargetDirCollisionCheck" to allow the same (for self-generated code)
                    if (string.Equals(_SourceDirectoryAbsPath, _TargetDirectoryAbsPath, StringComparison.CurrentCultureIgnoreCase)) return "Source and Target directories must be different";
                }

                if (!System.IO.Directory.Exists(_SourceDirectoryAbsPath)) return "Source Directory doesn't exist";

                return null;
            }
        }

        #endregion

        #region API        

        public static bool IsValidConfiguration(params string[] cfg)
        {
            if (cfg == null || cfg.Length == 0) return false;
            return cfg.All(item => IsValidConfigurationNode(item) == null);
        }

        public static Exception IsValidConfigurationNode(string cfgNode)
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

        public PathString MakeRelativeToSource(string absFilePath) { return _SourceDirectoryAbsPath.MakeRelativePath(absFilePath); }

        public PathString MakeRelativeToTarget(string absFilePath) { return _TargetDirectoryAbsPath.MakeRelativePath(absFilePath); }

        public PathString MakeAbsoluteToSource(string relFilePath) { return _SourceDirectoryAbsPath.MakeAbsolutePath(relFilePath); }

        public PathString MakeAbsoluteToTarget(string relFilePath) { return _TargetDirectoryAbsPath.MakeAbsolutePath(relFilePath); }        

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
            if (_SimulateOutput)
            {
                _SimulateExportContext.Create(absoluteUri, _TargetDirectoryAbsPath, _NotifyCreateFileSimulation);
            }
            
            return _ExportContext.Create(absoluteUri, _TargetDirectoryAbsPath);
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
