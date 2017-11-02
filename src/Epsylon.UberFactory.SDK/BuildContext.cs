using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    public static partial class SDK
    {
        public static string InformationalVersion
        {
            get
            {
                var assembly = typeof(SDK).GetTypeInfo().Assembly;

                var attribute = assembly
                    .GetCustomAttributes<AssemblyInformationalVersionAttribute>()
                    .FirstOrDefault();

                return attribute?.InformationalVersion;
            }
        }        

        public interface IMonitorContext : IProgress<float>
        {
            IMonitorContext GetProgressPart(int part, int total);

            /// <summary>
            /// True if host has requested operation cancellation
            /// </summary>
            bool IsCancelRequested { get; }

            /// <summary>
            /// Writes a Trace message to the log event system
            /// </summary>
            /// <param name="categoryName">category name</param>
            /// <param name="message">message</param>
            void LogTrace(string categoryName, string message);

            /// <summary>
            /// Writes a Debug message to the log event system
            /// </summary>
            /// <param name="categoryName">category name</param>
            /// <param name="message">message</param>
            void LogDebug(string categoryName, string message);

            /// <summary>
            /// Writes a Info message to the log event system
            /// </summary>
            /// <param name="categoryName">category name</param>
            /// <param name="message">message</param>
            void LogInfo(string categoryName, string message);

            /// <summary>
            /// Writes a Warning message to the log event system
            /// </summary>
            /// <param name="categoryName">category name</param>
            /// <param name="message">message</param>
            void LogWarning(string categoryName, string message);

            /// <summary>
            /// Writes a Error message to the log event system
            /// </summary>
            /// <param name="categoryName">category name</param>
            /// <param name="message">message</param>
            void LogError(string categoryName, string message);

            /// <summary>
            /// Writes a Critical message to the log event system
            /// </summary>
            /// <param name="categoryName">category name</param>
            /// <param name="message">message</param>
            void LogCritical(string categoryName, string message);
        }

        public interface ITaskFileIOTracker
        {
            void RegisterInputFile(string filePath, string parentFilePath);
            void RegisterOutputFile(string filePath, string parentFilePath);
        }

        /// <summary>
        /// The build context provides a number of services to the contet filters
        /// </summary>
        /// <remarks>
        /// Content Filters use the build context to:
        /// - convert paths from/to relative/absolute
        /// - Get a context to read source files
        /// - Get a context to write product files
        /// - Write to the log
        /// </remarks>
        public interface IBuildContext
        {
            String[] Configuration { get; }

            /// <summary>
            /// Converts an absolute URI path to a path relative to Context's Source path
            /// </summary>
            /// <param name="absoluteUri">An absolute URI path</param>
            /// <returns>A relative path</returns>
            String GetRelativeToSource(Uri absoluteUri);

            /// <summary>
            /// Converts an absolute URI path to a path relative to Context's Target path
            /// </summary>
            /// <param name="absoluteUri">An absolute URI path</param>
            /// <returns>A relative path</returns>
            String GetRelativeToTarget(Uri absoluteUri);

            /// <summary>
            /// Converts a path relative to Context's Source path to an absolute path
            /// </summary>
            /// <param name="relativePath">A path relative to Context's Source</param>
            /// <returns>An absolute URI path</returns>
            Uri GetSourceAbsoluteUri(String relativePath);

            /// <summary>
            /// Converts a path relative to Context's Target path to an absolute path
            /// </summary>
            /// <param name="relativePath">A path relative to Context's Target</param>
            /// <returns>An absolute URI path</returns>
            Uri GetTargetAbsoluteUri(String relativePath);            

            /// <summary>
            /// Creates an inport context to read files from the given location
            /// </summary>
            /// <param name="absoluteUri">A path to the location</param>
            /// <returns>An import context</returns>
            ImportContext GetImportContext(Uri absoluteUri, ITaskFileIOTracker trackerContext);

            /// <summary>
            /// Creates an Export context to write files to the given location
            /// </summary>
            /// <param name="absoluteUri">A path to the location</param>
            /// <returns>An export context</returns>
            ExportContext GetExportContext(Uri absoluteUri, ITaskFileIOTracker trackerContext);

            /// <summary>
            /// Gets a preview context to 
            /// </summary>            
            /// <returns>A preview context</returns>
            PreviewContext GetPreviewContext();
        }


        

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
            #region lifecycle

            protected ImportContext(ITaskFileIOTracker trackerContext)
            {
                this._TrackerContext = trackerContext;
            }

            #endregion

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

            #region data

            private readonly ITaskFileIOTracker _TrackerContext;

            #endregion

            #region API - Abstract

            /// <summary>
            /// Name of the default file being imported.
            /// </summary>            
            public abstract string FileName { get; }

            [Obsolete("Avoid using full file path whenever possible")]
            public abstract String FilePath { get; }

            
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
                _TrackerContext?.RegisterInputFile(relativePath, string.Equals(relativePath,this.FileName,StringComparison.OrdinalIgnoreCase)?null:this.FileName );
                return OpenFileCore(relativePath);
            }

            /// <summary>
            /// Reads an object from a path relative to current context
            /// </summary>
            /// <typeparam name="T">The type of the object to read</typeparam>
            /// <param name="relativePath">Path to the file, relative to the current context</param>
            /// <param name="readFunc">Deserialization function</param>
            /// <returns>The object instance</returns>
            public T ReadStream<T>(String relativePath, Func<System.IO.Stream,T> readFunc)
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
            public Byte[] ReadAllBytes(Encoding encoding) { return ReadAllBytes(FileName,encoding); }

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
            public T ReadText<T>(String relativePath, Encoding encoding, Func<System.IO.TextReader,T> readFunc)
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
            #region lifecycle

            protected ExportContext(ITaskFileIOTracker trackerContext)
            {
                this._TrackerContext = trackerContext;
            }

            #endregion

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

            #region data

            private readonly ITaskFileIOTracker _TrackerContext;

            #endregion

            #region API - Abstract

            /// <summary>
            /// Name of the default file being exported.
            /// </summary>            
            public abstract String FileName { get; }

            [Obsolete("Avoid using full file path whenever possible")]
            public abstract String FilePath { get; }

            [Obsolete("Avoid using output directory path whenever possible")]
            public abstract String OutputDirectory { get; }

            protected abstract System.IO.Stream OpenFileCore(String localName);

            #endregion

            #region API

            public System.IO.Stream OpenFile(String localName)
            {
                _TrackerContext?.RegisterOutputFile(localName, string.Equals(localName, this.FileName, StringComparison.OrdinalIgnoreCase) ? null : this.FileName);
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

            public void WriteAllText(String relativePath, String data) { WriteAllText(relativePath,DefaultEncoding,data); }

            public void WriteAllText(Encoding encoding, String data) { WriteAllText(FileName, encoding, data); }

            public void WriteAllText(String data) { WriteAllText(FileName, data); }

            #endregion
        }        


        public abstract class PreviewContext
        {
            public abstract ExportContext CreateMemoryFile(string fileName);
        }
        
    }

}
