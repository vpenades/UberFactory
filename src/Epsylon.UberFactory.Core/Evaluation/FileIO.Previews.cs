using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberFactory.Evaluation
{
    class _PreviewContext : SDK.PreviewContext
    {
        public override SDK.ExportContext CreateMemoryFile(string fileName)
        {
            return _DictionaryExportContext.Create(fileName);
        }
    }
}
