﻿using System;
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

            [SDK.InputValue("FileName")]
            [SDK.InputMetaData("Title","File")]            
            [SDK.InputMetaData("ViewStyle", "FilePicker")]
            [SDK.InputMetaDataEvaluate("Filter", nameof(GetFileFilter))]
            public String FileName { get; set; }                  

            protected override TValue Evaluate()
            {
                var s = this.GetImportContext(FileName);                
                if (s == null) throw new System.IO.FileNotFoundException("Error opening file", FileName.ToString());

                return ReadFile(s);                
            }

            protected abstract TValue ReadFile(ImportContext stream);

            protected override object EvaluatePreview(PreviewContext previewContext)
            {
                return base.EvaluatePreview(previewContext);
            }            
        }

        


        public abstract class FileWriter : ContentFilter
        {
            [SDK.InputValue("FileName")]
            [SDK.InputMetaData("Title", "File Name")]
            public String FileName { get; set; }

            protected abstract String GetFileExtension();

            protected override object EvaluateObject()
            {
                var relPath = System.IO.Path.ChangeExtension(FileName + ".bin", GetFileExtension());                

                var s = this.GetExportContext(relPath);                
                if (s == null) return null;

                WriteFile(s);                

                return null;
            }

            protected abstract void WriteFile(ExportContext stream);
        }





        public abstract class BatchProcessor<TValueIn,TValueOut> : ContentFilter
        {
            // unfortunately, we can't simply create a "BatchReader" that returns a collections, because we must ensure files are read one at a time.

            [SDK.InputValue("DirectoryName")]
            [SDK.InputMetaData("Group", "Source Directory")]
            [SDK.InputMetaData("Title", "Path")]
            [SDK.InputMetaData("ViewStyle", "DirectoryPicker")]
            public String DirectoryName { get; set; }

            [SDK.InputValue("FileMask")]
            [SDK.InputMetaData("Group", "Source Directory")]
            [SDK.InputMetaData("Title", "Mask")]
            [SDK.InputMetaData("Default", "*")]
            public String FileMask { get; set; }

            [SDK.InputValue("AllDirectories")]
            [SDK.InputMetaData("Group", "Source Directory")]
            [SDK.InputMetaData("Title", "All Directories")]
            [SDK.InputMetaData("Default", false)]
            public Boolean AllDirectories { get; set; }

            protected override Object EvaluateObject()
            {
                var fileInMask = System.IO.Path.ChangeExtension(FileMask, GetFileInExtension());

                var importers = this.GetImportContextBatch(DirectoryName, fileInMask, AllDirectories).ToArray();

                foreach(var importer in importers)
                {
                    var valIn = ReadFile(importer); if (valIn == null) continue;

                    var valOut = Process(valIn); if (valOut == null) continue;

                    var fileOutName = System.IO.Path.ChangeExtension(importer.FileName, GetFileOutExtension());

                    var exporter = this.GetExportContext(fileOutName);

                    WriteFile(exporter, valOut);
                }

                return null;
            }

            protected abstract String GetFileInExtension();

            protected abstract TValueIn ReadFile(ImportContext stream);

            protected abstract TValueOut Process(TValueIn value);

            protected abstract String GetFileOutExtension();

            protected abstract void WriteFile(ExportContext stream, TValueOut value);
        }

    }
}
