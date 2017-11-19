using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    public static partial class SDK
    {
        
        public abstract class FileReader<TValue> : ContentFilter<TValue> 
        {
            public virtual string GetFileFilter() { return "All Files|*.*"; }

            [SDK.InputValue("FilePath")]
            [SDK.InputMetaData("Title","File")]            
            [SDK.InputMetaData("ViewStyle", "PathPicker")]
            [SDK.InputMetaDataEvaluate("Filter", nameof(GetFileFilter))]
            public String FilePath { get; set; }                  

            protected override TValue Evaluate()
            {
                var s = this.GetImportContext(FilePath);                
                if (s == null) throw new System.IO.FileNotFoundException("Error opening file", FilePath.ToString());

                return ReadFile(s);                
            }

            protected override object EvaluatePreview(PreviewContext previewContext)
            {
                return base.EvaluatePreview(previewContext);
            }

            protected abstract TValue ReadFile(ImportContext stream);
        }
            
        
        public abstract class FileWriter : ContentFilter
        {
            [SDK.InputValue("FileName")]
            [SDK.InputMetaData("Title", "File Name")]
            public String FileName { get; set; }

            protected abstract String GetFileExtension();            

            protected override object EvaluateObject()
            {
                var rpath = System.IO.Path.ChangeExtension(FileName + ".bin", GetFileExtension());

                var absUri = this.BuildContext.GetTargetAbsolutePath(rpath);

                var s = this.GetExportContext(absUri);                
                if (s == null) return null;

                WriteFile(s);                

                return null;
            }

            protected abstract void WriteFile(ExportContext stream);
        }

        
    }
}
