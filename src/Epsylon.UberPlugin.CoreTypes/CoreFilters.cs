using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberPlugin
{
    using UberFactory;

    [SDK.ContentNode("FileCopy")]
    [SDK.Title("Copy File")]
    public class FileCopy : SDK.ContentFilter
    {
        [SDK.InputValue("SourceFileName")]
        [SDK.Title("Source File")]
        [SDK.ViewStyle("FilePicker")]
        [SDK.MetaData("Filter", "*.*")]
        public String SourceFileName { get; set; }

        [SDK.InputValue("TargetFileName")]
        [SDK.Title("Target File")]
        public String TargetFileName { get; set; }

        protected override object EvaluateObject()
        {
            var inPath = SourceFileName;
            var outPath = string.IsNullOrWhiteSpace(TargetFileName) ? System.IO.Path.GetFileName(inPath) : TargetFileName;

            var inCtx = this.GetImportContext(inPath);
            if (inCtx == null) throw new System.IO.FileNotFoundException("Error opening file", inPath.ToString());

            var outCtx = this.GetExportContext(outPath);
            if (outCtx == null) return null;

            var bytes = inCtx.ReadAllBytes();

            outCtx.WriteAllBytes(bytes);

            return bytes;
        }
    }
}
