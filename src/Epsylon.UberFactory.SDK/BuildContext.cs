using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    public static partial class SDK
    {
        public interface IBuildContext
        {
            
            String GetRelativeToSource(Uri absoluteUri);
            String GetRelativeToTarget(Uri absoluteUri);

            Uri GetSourceAbsoluteUri(String relativePath);
            Uri GetTargetAbsoluteUri(String relativePath);            

            ImportContext GetImportContext(Uri absoluteUri);
            ExportContext GetExportContext(Uri absoluteUri);

            void LogTrace(string name, string message);
            void LogDebug(string name, string message);
            void LogInfo(string name, string message);
            void LogWarning(string name, string message);
            void LogError(string name, string message);
            void LogCritical(string name, string message);
        }

        public abstract class ImportContext : IDisposable
        {
            /// <summary>
            /// Default text encoding for reading text strings
            /// </summary>
            /// <remarks>
            /// Originally, Net.Framework used Encoding.Default, which is only available on Net.Framework, and
            /// mapped to the local culture CodePage.
            /// Latest version of the platform use UTF8 and encoding detection enabled on ReadAllText
            /// </remarks>
            public static readonly Encoding DefaultEncoding = Encoding.UTF8;

            public virtual void Dispose() { }

            public abstract string FileName { get; }            

            public abstract System.IO.Stream OpenFile(String localName);

            public T ReadStream<T>(String localName, Func<System.IO.Stream,T> readFunc)
            {
                using (var s = OpenFile(localName))
                {
                    return readFunc(s);
                }                
            }

            public T ReadStream<T>(Func<System.IO.Stream, T> readFunc) { return ReadStream(FileName, readFunc); }
            
            public T ReadBinary<T>(String localName, Encoding encoding, Func<System.IO.BinaryReader, T> readFunc)
            {
                using (var s = OpenFile(localName))
                {
                    using (var b = new System.IO.BinaryReader(s, encoding))
                    {
                        return readFunc(b);
                    }
                }
            }

            public T ReadBinary<T>(Encoding encoding, Func<System.IO.BinaryReader, T> readFunc) { return ReadBinary(FileName, encoding, readFunc); }

            public T ReadBinary<T>(String localName, Func<System.IO.BinaryReader, T> readFunc) { return ReadBinary(localName, DefaultEncoding, readFunc); }

            public T ReadBinary<T>(Func<System.IO.BinaryReader, T> readFunc) { return ReadBinary(FileName, DefaultEncoding, readFunc); }

            public Byte[] ReadAllBytes(String localName, Encoding encoding) { return ReadBinary<Byte[]>(localName, encoding, rb => rb.ReadBytes((int)rb.BaseStream.Length)); }

            public Byte[] ReadAllBytes(Encoding encoding) { return ReadAllBytes(FileName,encoding); }

            public Byte[] ReadAllBytes(String localName) { return ReadAllBytes(localName, DefaultEncoding); }

            public Byte[] ReadAllBytes() { return ReadAllBytes(FileName); }

            public T ReadText<T>(String localName, Encoding encoding, Func<System.IO.TextReader,T> readFunc)
            {
                // TODO: originally in NET.Framework, it uses Encoding.Default which maps to local ANSI character's page

                // how to use codepages in NET.Core
                // http://stackoverflow.com/questions/37870084/net-core-doesnt-know-about-windows-1252-how-to-fix

                using (var s = OpenFile(localName))
                {
                    using (var t = new System.IO.StreamReader(s, encoding, true))
                    {
                        return readFunc(t);
                    }
                }
            }

            public T ReadText<T>(Encoding encoding, Func<System.IO.TextReader, T> readFunc) { return ReadText(FileName, encoding, readFunc); }

            public T ReadText<T>(String localName, Func<System.IO.TextReader, T> readFunc) { return ReadText(localName, DefaultEncoding, readFunc); }

            public T ReadText<T>(Func<System.IO.TextReader, T> readFunc) { return ReadText(FileName, DefaultEncoding, readFunc); }

            public String ReadAllText(String localName, Encoding encoding) { return ReadText<String>(localName, encoding, rb => rb.ReadToEnd()); }

            public String ReadAllText(Encoding encoding) { return ReadAllText(FileName, encoding); }

            public String ReadAllText(String localName) { return ReadAllText(localName, DefaultEncoding); }

            public String ReadAllText() { return ReadAllText(FileName); }

        }

        public abstract class ExportContext : IDisposable
        {
            /// <summary>
            /// Default text encoding for writing text strings
            /// </summary>
            /// <remarks>
            /// Net.Framework uses UTF8noBOM as default encoding for WriteAllText
            /// </remarks>
            /// <see cref="http://referencesource.microsoft.com/#mscorlib/system/io/streamwriter.cs,106"/>
            public static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true); 

            public virtual void Dispose() { }

            public abstract String FileName { get; }

            [Obsolete("Avoid using full file path whenever possible")]
            public abstract String FilePath { get; }

            [Obsolete("Avoid using output directory path whenever possible")]
            public abstract String OutputDirectory { get; }

            public abstract System.IO.Stream OpenFile(String localName);

            public void WriteStream(String localName, Action<System.IO.Stream> writeFunc)
            {
                using (var s = OpenFile(localName))
                {
                    writeFunc(s);
                }
            }

            public void WriteStream(Action<System.IO.Stream> writeFunc) { WriteStream(FileName, writeFunc); }

            public void WriteBinary(String localName, Encoding encoding, Action<System.IO.BinaryWriter> writeFunc)
            {
                using (var s = OpenFile(localName))
                {
                    using (var b = new System.IO.BinaryWriter(s, encoding))
                    {
                        writeFunc(b);
                    }
                }
            }

            public void WriteBinary(String localName, Action<System.IO.BinaryWriter> writeFunc) { WriteBinary(localName, DefaultEncoding, writeFunc); }

            public void WriteBinary(Encoding encoding, Action<System.IO.BinaryWriter> writeFunc) { WriteBinary(FileName, encoding, writeFunc); }

            public void WriteBinary(Action<System.IO.BinaryWriter> writeFunc) { WriteBinary(FileName, DefaultEncoding, writeFunc); }

            public void WriteAllBytes(String localName, Encoding encoding, Byte[] data) { WriteBinary(localName, encoding, b => b.Write(data)); }

            public void WriteAllBytes(String localName, Byte[] data) { WriteAllBytes(localName, DefaultEncoding, data); }

            public void WriteAllBytes(Encoding encoding, Byte[] data) { WriteAllBytes(FileName, encoding, data); }

            public void WriteAllBytes(Byte[] data) { WriteAllBytes(FileName, data); }

            public void WriteText(String localName, Encoding encoding, Action<System.IO.StreamWriter> writeFunc)
            {
                using (var s = OpenFile(localName))
                {
                    using (var t = new System.IO.StreamWriter(s, encoding))
                    {
                        writeFunc(t);
                    }
                }
            }

            public void WriteText(String localName, Action<System.IO.StreamWriter> writeFunc) { WriteText(localName, DefaultEncoding, writeFunc); }

            public void WriteText(Encoding encoding, Action<System.IO.StreamWriter> writeFunc) { WriteText(FileName, encoding, writeFunc); }

            public void WriteText(Action<System.IO.StreamWriter> writeFunc) { WriteText(FileName, writeFunc); }

            public void WriteAllText(String localName, Encoding encoding, String data) { WriteText(localName, encoding, b => b.Write(data)); }

            public void WriteAllText(String localName, String data) { WriteAllText(localName,DefaultEncoding,data); }

            public void WriteAllText(Encoding encoding, String data) { WriteAllText(FileName, encoding, data); }

            public void WriteAllText(String data) { WriteAllText(FileName, data); }            
        }        

    }

}
