using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Epsylon.UberFactory
{
    public static partial class SDK
    {

        // si dejo que el usuario me envie cualquier "import context", me puede enviar cualquier cosa.

        class _MemoryContextWriter : ExportContext
        {
            #region data

            private string _DefaultFileName;
            private readonly Dictionary<String, Byte[]> _Files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

            #endregion

            #region API

            public IReadOnlyDictionary<String, Byte[]> Content => _Files;

            public override string FileName => _DefaultFileName;

            public override string FilePath => throw new NotSupportedException();

            public override string OutputDirectory => throw new NotSupportedException();

            public override Stream OpenFile(string localName)
            {
                return new _MemoryStream(localName, (key, val) => _Files[key] = val);
            }

            #endregion

            #region helper class

            class _MemoryStream : MemoryStream
            {
                public _MemoryStream(string name, Action<string, Byte[]> onClosingFile)
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

            #endregion
        }

        

    }
}
