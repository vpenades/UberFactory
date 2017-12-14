using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberFactory
{
    public static partial class SDK
    {
        /// <summary>
        /// Current context for reading files
        /// </summary>
        /// <remarks>
        /// Used by <code>FileReader&lt;TValue&gt;</code>
        /// Represents the context of the current file being read.
        /// It provides several methods and helpers to open and read a file.
        /// It also allows to read other files, for example,
        /// when you need to read another file, associated to the current one.
        /// </remarks>        
        public abstract class ImportContext
        {
            #region Constants

            /// <summary>
            /// Default text encoding for reading text strings
            /// </summary>
            /// <remarks>
            /// As reference <code>System.IO.File.ReadAllText</code> uses:
            /// Older Net.Framework used Encoding.Default, which is only available on Net.Framework
            /// and maps to the local culture ASCII CodePage.
            /// Latest version of Net.Framework uses UTF8 and encoding detection enabled on ReadAllText
            /// </remarks>
            /// <see cref="http://referencesource.microsoft.com/#mscorlib/system/io/streamreader.cs,137"/>
            /// <seealso cref="http://referencesource.microsoft.com/#mscorlib/system/io/file.cs,794"/>
            public static readonly Encoding DefaultEncoding = Encoding.UTF8;

            #endregion            

            #region API - Abstract

            /// <summary>
            /// Name of the default file being imported.
            /// </summary>            
            public abstract string FileName { get; }            

            protected abstract System.IO.Stream OpenFileCore(String relativePath);

            #endregion

            #region API

            /// <summary>
            /// Opens a Read stream.
            /// </summary>
            /// <param name="relativePath">path relative to the current context</param>
            /// <returns>a read stream</returns>
            public System.IO.Stream OpenFile(String relativePath)
            {                
                return OpenFileCore(relativePath);
            }

            /// <summary>
            /// Reads an object from a path relative to current context
            /// </summary>
            /// <typeparam name="T">The type of the object to read</typeparam>
            /// <param name="relativePath">Path to the file, relative to the current context</param>
            /// <param name="readFunc">Deserialization function</param>
            /// <returns>The object instance</returns>
            public T ReadStream<T>(String relativePath, Func<System.IO.Stream, T> readFunc)
            {
                using (var s = OpenFile(relativePath))
                {
                    return readFunc(s);
                }
            }

            /// <summary>
            /// Reads an object from the current file
            /// </summary>
            /// <typeparam name="T">The type of the object to read</typeparam>
            /// <param name="readFunc">Deserialization function</param>
            /// <returns>The object instance</returns>
            public T ReadStream<T>(Func<System.IO.Stream, T> readFunc) { return ReadStream(FileName, readFunc); }

            /// <summary>
            /// Reads an object from a path relative to current context
            /// </summary>
            /// <typeparam name="T">The type of the object to read</typeparam>
            /// <param name="relativePath">Path to the file, relative to the current context</param>
            /// <param name="encoding"></param>
            /// <param name="readFunc"></param>
            /// <returns></returns>
            public T ReadBinary<T>(String relativePath, Encoding encoding, Func<System.IO.BinaryReader, T> readFunc)
            {
                using (var s = OpenFile(relativePath))
                {
                    using (var b = new System.IO.BinaryReader(s, encoding))
                    {
                        return readFunc(b);
                    }
                }
            }

            /// <summary>
            /// Reads an object from the current file
            /// </summary>
            /// <param name="encoding">The text encoding</param>
            /// <typeparam name="T">The type of the object to read</typeparam>
            /// <param name="readFunc">Deserialization function</param>
            /// <returns>The object instance</returns>
            public T ReadBinary<T>(Encoding encoding, Func<System.IO.BinaryReader, T> readFunc) { return ReadBinary(FileName, encoding, readFunc); }

            /// <summary>
            /// Reads an object from a path relative to current context
            /// </summary>
            /// <param name="relativePath">Path to the file, relative to the current context</param>
            /// <typeparam name="T">The type of the object to read</typeparam>
            /// <param name="encoding">The text encoding</param>
            /// <param name="readFunc">Deserialization function</param>
            /// <returns>The object instance</returns>
            public T ReadBinary<T>(String relativePath, Func<System.IO.BinaryReader, T> readFunc) { return ReadBinary(relativePath, DefaultEncoding, readFunc); }

            /// <summary>
            /// Reads an object from the current context
            /// </summary>
            /// <typeparam name="T">The type of the object to read</typeparam>
            /// <param name="readFunc">Deserialization function</param>
            /// <returns></returns>
            public T ReadBinary<T>(Func<System.IO.BinaryReader, T> readFunc) { return ReadBinary(FileName, DefaultEncoding, readFunc); }

            /// <summary>
            /// Reads all bytes from path relative to the current context
            /// </summary>
            /// <param name="relativePath">Path to the file, relative to the current context</param>
            /// <param name="encoding">The text encoding</param>
            /// <returns>The content bytes</returns>
            public Byte[] ReadAllBytes(String relativePath, Encoding encoding) { return ReadBinary<Byte[]>(relativePath, encoding, rb => rb.ReadBytes((int)rb.BaseStream.Length)); }

            /// <summary>
            /// Reads a byte array from the current context
            /// </summary>
            /// <param name="encoding">The text encoding</param>
            /// <returns>The content bytes</returns>
            public Byte[] ReadAllBytes(Encoding encoding) { return ReadAllBytes(FileName, encoding); }

            /// <summary>
            /// Reads a byte array from a path relative to current context
            /// </summary>
            /// <param name="relativePath">Path to the file, relative to the current context</param>
            /// <returns>The content bytes</returns>
            public Byte[] ReadAllBytes(String relativePath) { return ReadAllBytes(relativePath, DefaultEncoding); }

            /// <summary>
            /// Reads a byte array from the current context
            /// </summary>
            /// <returns>The content bytes</returns>
            public Byte[] ReadAllBytes() { return ReadAllBytes(FileName); }

            /// <summary>
            /// Reads an object from a path relative to current context
            /// </summary>
            /// <typeparam name="T">The type of the object to read</typeparam>
            /// <param name="relativePath">Path to the file, relative to the current context</param>
            /// <param name="encoding">The text encoding</param>
            /// <param name="readFunc"Deserialization function></param>
            /// <returns>The object instance</returns>
            public T ReadText<T>(String relativePath, Encoding encoding, Func<System.IO.TextReader, T> readFunc)
            {
                // TODO: originally in NET.Framework, it uses Encoding.Default which maps to local ANSI character's page

                // how to use codepages in NET.Core
                // http://stackoverflow.com/questions/37870084/net-core-doesnt-know-about-windows-1252-how-to-fix

                using (var s = OpenFile(relativePath))
                {
                    using (var t = new System.IO.StreamReader(s, encoding, true))
                    {
                        return readFunc(t);
                    }
                }
            }

            /// <summary>
            /// Reads an object from the current context
            /// </summary>
            /// <typeparam name="T">The type of the object to read</typeparam>
            /// <param name="encoding">The text encoding</param>
            /// <param name="readFunc">Deserialization function</param>
            /// <returns>The object instance</returns>
            public T ReadText<T>(Encoding encoding, Func<System.IO.TextReader, T> readFunc) { return ReadText(FileName, encoding, readFunc); }

            /// <summary>
            /// Reads an object from a path relative to current context
            /// </summary>
            /// <typeparam name="T">The type of the object to read</typeparam>
            /// <param name="relativePath">Path to the file, relative to the current context</param>
            /// <param name="readFunc">Deserialization function</param>
            /// <returns>The object instance</returns>
            public T ReadText<T>(String relativePath, Func<System.IO.TextReader, T> readFunc) { return ReadText(relativePath, DefaultEncoding, readFunc); }

            /// <summary>
            /// Reads an object from the current context
            /// </summary>
            /// <typeparam name="T">The type of the object to read</typeparam>
            /// <param name="readFunc">Deserialization function</param>
            /// <returns>The object instance</returns>
            public T ReadText<T>(Func<System.IO.TextReader, T> readFunc) { return ReadText(FileName, DefaultEncoding, readFunc); }

            /// <summary>
            /// Reads all the text from the file path relative to the current context
            /// </summary>
            /// <param name="relativePath">Path to the file, relative to the current context</param>
            /// <param name="encoding">The text encoding</param>
            /// <returns>The content text</returns>
            public String ReadAllText(String relativePath, Encoding encoding) { return ReadText<String>(relativePath, encoding, rb => rb.ReadToEnd()); }

            /// <summary>
            /// Reads all the text from the current context
            /// </summary>
            /// <param name="encoding">The text encoding</param>
            /// <returns>The content text</returns>
            public String ReadAllText(Encoding encoding) { return ReadAllText(FileName, encoding); }

            /// <summary>
            /// Reads all the text from a path relative to current context
            /// </summary>
            /// <param name="relativePath">Path to the file, relative to the current context</param>
            /// <returns>The content text</returns>
            public String ReadAllText(String relativePath) { return ReadAllText(relativePath, DefaultEncoding); }

            /// <summary>
            /// Reads all the text from the current file
            /// </summary>
            /// <returns>The content text</returns>
            public String ReadAllText() { return ReadAllText(FileName); }

            #endregion
        }

        public abstract class ImportContextEx : ImportContext
        {
            #region API - Abstract
            
            public abstract String FilePath { get; }

            #endregion
        }
    }
}
