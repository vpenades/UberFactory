using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberFactory
{
    public static partial class SDK
    {
        /// <summary>
        /// Current context for writing files
        /// </summary>
        /// <remarks>
        /// Used by <code>FileWriter;</code>
        /// Represents the context of the current file being written.
        /// It provides several methods and helpers to open and write a file.
        /// It also allows to write other files, for example,
        /// when you need to write another file, associated to the current one.
        /// </remarks>
        public abstract class ExportContext
        {
            #region Constants

            /// <summary>
            /// Default text encoding for writing text strings
            /// </summary>
            /// <remarks>
            /// Net.Framework uses UTF8noBOM as default encoding for WriteAllText
            /// </remarks>
            /// <see cref="http://referencesource.microsoft.com/#mscorlib/system/io/streamwriter.cs,106"/>
            public static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true);

            #endregion            

            #region API - Abstract

            /// <summary>
            /// Name of the default file being exported.
            /// </summary>            
            public abstract String FileName { get; }            

            protected abstract System.IO.Stream OpenFileCore(String localName);

            #endregion

            #region API

            public System.IO.Stream OpenFile(String localName)
            {                
                return OpenFileCore(localName);
            }

            public void WriteStream(String relativePath, Action<System.IO.Stream> writeFunc)
            {
                using (var s = OpenFile(relativePath))
                {
                    writeFunc(s);
                }
            }

            public void WriteStream(Action<System.IO.Stream> writeFunc) { WriteStream(FileName, writeFunc); }

            public void WriteBinary(String relativePath, Encoding encoding, Action<System.IO.BinaryWriter> writeFunc)
            {
                using (var s = OpenFile(relativePath))
                {
                    using (var b = new System.IO.BinaryWriter(s, encoding))
                    {
                        writeFunc(b);
                    }
                }
            }

            public void WriteBinary(String relativePath, Action<System.IO.BinaryWriter> writeFunc) { WriteBinary(relativePath, DefaultEncoding, writeFunc); }

            public void WriteBinary(Encoding encoding, Action<System.IO.BinaryWriter> writeFunc) { WriteBinary(FileName, encoding, writeFunc); }

            public void WriteBinary(Action<System.IO.BinaryWriter> writeFunc) { WriteBinary(FileName, DefaultEncoding, writeFunc); }

            public void WriteAllBytes(String relativePath, Encoding encoding, Byte[] data) { WriteBinary(relativePath, encoding, b => b.Write(data)); }

            public void WriteAllBytes(String relativePath, Byte[] data) { WriteAllBytes(relativePath, DefaultEncoding, data); }

            public void WriteAllBytes(Encoding encoding, Byte[] data) { WriteAllBytes(FileName, encoding, data); }

            public void WriteAllBytes(Byte[] data) { WriteAllBytes(FileName, data); }

            public void WriteText(String relativePath, Encoding encoding, Action<System.IO.StreamWriter> writeFunc)
            {
                using (var s = OpenFile(relativePath))
                {
                    using (var t = new System.IO.StreamWriter(s, encoding))
                    {
                        writeFunc(t);
                    }
                }
            }

            public void WriteText(String relativePath, Action<System.IO.StreamWriter> writeFunc) { WriteText(relativePath, DefaultEncoding, writeFunc); }

            public void WriteText(Encoding encoding, Action<System.IO.StreamWriter> writeFunc) { WriteText(FileName, encoding, writeFunc); }

            public void WriteText(Action<System.IO.StreamWriter> writeFunc) { WriteText(FileName, writeFunc); }

            public void WriteAllText(String relativePath, Encoding encoding, String data) { WriteText(relativePath, encoding, b => b.Write(data)); }

            public void WriteAllText(String relativePath, String data) { WriteAllText(relativePath, DefaultEncoding, data); }

            public void WriteAllText(Encoding encoding, String data) { WriteAllText(FileName, encoding, data); }

            public void WriteAllText(String data) { WriteAllText(FileName, data); }

            #endregion
        }

        public abstract class ExportContextEx : ExportContext
        {
            #region API - Abstract

            public abstract String FilePath { get; }

            public abstract String OutputDirectory { get; }

            #endregion
        }
    }
}
