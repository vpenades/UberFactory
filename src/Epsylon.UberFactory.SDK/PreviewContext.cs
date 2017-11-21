using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberFactory
{
    public static partial class SDK
    {
        public abstract class PreviewContext
        {
            public abstract ExportContext CreateMemoryFile(String fileName);
        }
    }
}
