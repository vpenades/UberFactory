using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Epsylon.UberFactory
{
    public static partial class SDK
    {
        class _MemoryDirectory
        {
            private readonly Dictionary<string, Byte[]> _Files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            private readonly Object _Lock = new object();

            public void AddEntry(string name, byte[] data) { lock (_Lock) { _Files[name] = data; } }
        }

        class _MemoryDirectoryWriter : ExportContext
        {
            private string _DefaultFileName;
            private _MemoryDirectory _Content = new _MemoryDirectory();

            public override string FileName => _DefaultFileName;

            public override string FilePath => throw new NotSupportedException();

            public override string OutputDirectory => throw new NotSupportedException();

            public override Stream OpenFile(string localName)
            {
                return new _DummyStream(localName, (key, data) => _Content.AddEntry(key,data) );
            }
        }

        class _DummyStream : System.IO.MemoryStream
        {
            public _DummyStream(string name, Action<string, Byte[]> onClosingFile)
            {
                _FileName = name;
                _OnClosingFile = onClosingFile;
            }

            private readonly string _FileName;
            private readonly Action<string, Byte[]> _OnClosingFile;

            protected override void Dispose(bool disposing)
            {
                if (disposing && _OnClosingFile != null)
                {
                    _OnClosingFile.Invoke(_FileName, this.ToArray());                    
                }

                base.Dispose(disposing);
            }

        }

    }
}
