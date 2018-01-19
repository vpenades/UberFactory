using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberPlugin
{
    using UberFactory;

    [SDK.Icon(Constants.ICON_LOADFROMFILE), SDK.Title("Copy File")]
    [SDK.ContentNode("FileCopy")]    
    public class FileCopy : SDK.ContentFilter
    {
        [SDK.Title("Source File")]
        [SDK.InputValue("SourceFileName")]        
        [SDK.ViewStyle("FilePicker")]
        [SDK.MetaData("Filter", Constants.OPENFILEDIALOGFILTER_ALLFILES)]
        public String SourceFileName { get; set; }

        [SDK.Title("Target File")]
        [SDK.InputValue("TargetFileName")]        
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
