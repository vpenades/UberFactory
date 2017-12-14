using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    // https://blogs.msdn.microsoft.com/jeremykuhne/2016/04/21/path-normalization/

    /// <summary>
    /// Essentially a text String, but with methods specialised for file and directory path operations
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_Path}")]
    public struct PathString : IEquatable<PathString>
    {
        #region lifecycle

        public PathString(Uri p) { _Path = p?.ToFriendlySystemPath(); }

        public PathString(string p) { _Path = p; }

        public static implicit operator String(PathString p) { return p._Path; }

        #endregion

        #region data

        public static readonly PathString Empty = new PathString((string)null);        

        public static PathString CurrentDirectory { get { return new PathString(System.IO.Directory.GetCurrentDirectory()); } }

        private readonly String _Path;

        public override int GetHashCode()
        {
            return this.Normalized.ToString().ToLower().GetHashCode();
        }

        public static bool Equals(PathString a, PathString b)
        {
            var aa = a.Normalized.ToString();
            var bb = b.Normalized.ToString();

            return string.Equals(aa, bb, StringComparison.OrdinalIgnoreCase);
        }

        public bool Equals(PathString other) { return Equals(this, other); }

        public override bool Equals(Object obj) { return obj is PathString ? Equals(this, (PathString)obj) : false; }

        public static bool operator ==(PathString a, PathString b) { return Equals(a, b); }

        public static bool operator !=(PathString a, PathString b) { return !Equals(a, b); }


        public override string ToString() { return (String)this; }

        #endregion        

        #region properties

        public PathString Normalized
        {
            get
            {
                // http://stackoverflow.com/questions/2281531/how-can-i-compare-directory-paths-in-c

                // https://github.com/psmacchia/NDepend.Path

                var path = System.IO.Path.GetFullPath(_Path);

                path = System.IO.Path.GetFullPath(new Uri(path).LocalPath)
                    .TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)
                    .ToUpperInvariant();

                return new PathString(path);
            }
        }

        public bool IsEmpty => string.IsNullOrWhiteSpace(_Path);

        public bool IsAbsolute
        {
            get
            {
                if (IsEmpty) throw new InvalidOperationException();
                return IsValidDirectoryAbsolutePath;
            }
        }

        public bool IsValidDirectoryPath
        {            
            get
            {
                if (IsEmpty) return false;
                try { new System.IO.DirectoryInfo(_Path); return true; } catch { return false; }
            }
        }

        public bool IsValidFilePath
        {
            get
            {
                if (IsEmpty) return false;
                try { new System.IO.FileInfo(_Path); return true; } catch { return false; }
            }
        }

        public bool IsValidDirectoryAbsolutePath
        {
            get
            {
                if (!IsValidDirectoryPath) return false;
                return System.IO.Path.IsPathRooted(_Path);
            }
        }

        public bool IsValidDirectoryRelativePath
        {
            get
            {
                if (!IsValidDirectoryPath) return false;
                return !System.IO.Path.IsPathRooted(_Path);
            }
        }

        public bool IsValidAbsoluteFilePath
        {
            get
            {
                if (!IsValidFilePath) return false;
                return System.IO.Path.IsPathRooted(_Path);
            }
        }

        public bool IsValidRelativeFilePath
        {
            get
            {
                if (!IsValidFilePath) return false;
                return !System.IO.Path.IsPathRooted(_Path);
            }
        }

        public String FileName => _Path == null ? String.Empty : System.IO.Path.GetFileName(_Path);

        public String FileNameWithoutExtension => _Path == null ? String.Empty : System.IO.Path.GetFileNameWithoutExtension(_Path);

        public PathString DirectoryPath => _Path == null ? PathString.Empty : new PathString(System.IO.Path.GetDirectoryName(_Path));

        #endregion

        #region API

        public static PathString operator +(PathString a, PathString b)
        {
            return new PathString(System.IO.Path.Combine(a, b));
        }

        public bool Contains(PathString other)
        {
            if (!this.IsValidDirectoryPath) return false;

            var thisPath = this.AsAbsolute().ToString();
            var otherPath = other.AsAbsolute().ToString();

            if (otherPath.Length < thisPath.Length) return false;

            otherPath = otherPath.Substring(0, thisPath.Length);

            return new PathString(thisPath) == new PathString(otherPath);
        }

        public PathString AsAbsolute()
        {
            if (IsEmpty) return Empty;

            if (IsAbsolute) return this;

            return CurrentDirectory.MakeAbsolutePath(_Path);
        }

        public PathString MakeAbsolutePath(string relfilePath)
        {
            if (string.IsNullOrWhiteSpace(relfilePath)) return this;            

            var newPath = System.IO.Path.Combine(_Path, relfilePath);

            // todo: split into parts and remove ".." and previous element

            newPath = System.IO.Path.GetFullPath(newPath);
            


            return new PathString(newPath);
        }

        public PathString MakeRelativePath(Uri absUri)
        {
            if (absUri == null) throw new ArgumentNullException(nameof(absUri));
            if (!absUri.IsAbsoluteUri) throw new ArgumentException(nameof(absUri));

            return this.MakeRelativePath(new PathString(absUri));
        }

        public PathString MakeRelativePath(string absFilePath)
        {
            if (string.IsNullOrWhiteSpace(absFilePath)) return this;

            var directoryPath = _Path;

            if (!directoryPath.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString())) directoryPath += System.IO.Path.DirectorySeparatorChar;            

            var uri1 = new Uri(directoryPath, UriKind.Absolute);
            var uri2 = new Uri(absFilePath, UriKind.Absolute);
            var diff = uri1.MakeRelativeUri(uri2);
            var rpath = diff.OriginalString;
            rpath = Uri.UnescapeDataString(rpath).Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);

            return new PathString(rpath);
        }

        public PathString ChangeExtension(string ext)
        {
            var path = System.IO.Path.ChangeExtension(_Path, ext);

            return new PathString(path);
        }

        public bool FileExists => _Path == null ? false: System.IO.File.Exists(_Path);

        public bool DirectoryExists => _Path == null ? false : System.IO.Directory.Exists(_Path);

        public bool HasExtension(string ext)
        {
            var cext = System.IO.Path.GetExtension(_Path);

            return cext.ToLower().EndsWith(ext.ToLower());
        }

        public Uri ToUri()
        {
            return this.IsAbsolute ? new Uri(_Path, UriKind.Absolute) : new Uri(_Path, UriKind.Relative);
        }

        #endregion

    }
}
 