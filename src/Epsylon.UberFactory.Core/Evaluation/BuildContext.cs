using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory.Evaluation
{
    [System.Diagnostics.DebuggerDisplay("BuildContext {ConfigurationJoined} Ready:{CanBuild} Input:{SourceDirectory.ToString()} Output:{TargetDirectory.ToString()} Errors:{CurrentError}")]
    public class BuildContext
    {
        #region lifecycle        

        public static BuildContext Create(String cfg, PathString sd, bool simul = false) { return Create(cfg, sd, PathString.Empty, simul); }

        public static BuildContext Create(String cfg, PathString sd, PathString td, bool simul = false)
        {
            var xcfg = cfg?.Split('.').ToArray();

            return new BuildContext(xcfg, sd, td, simul);
        }

        public static BuildContext Create(BuildContext other, PathString td, bool simul = false) { return new BuildContext(other.Configuration, other.SourceDirectory, td, simul); }
        
        protected BuildContext(String[] cfg, PathString sd, bool simul) : this(cfg, sd, PathString.Empty, simul) { }

        private BuildContext(String[] cfg, PathString sd, PathString td, bool simul)
        {
            _Configuration = cfg ?? (new string[0]);
            _SourceDirectoryAbsPath = sd;
            _TargetDirectoryAbsPath = td;
            IsSimulation = simul;

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

        public bool IsSimulation { get; private set; }

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

    
}
