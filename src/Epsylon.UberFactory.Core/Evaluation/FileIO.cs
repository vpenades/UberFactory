using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Epsylon.UberFactory.Evaluation
{
    /// <summary>
    /// Tells the evaluator which child files depend on which parent files
    /// </summary>
    public interface IFileTracker
    {
        void RegisterInputFile(string filePath, string parentFilePath);
        void RegisterOutputFile(string filePath, string parentFilePath);
    }

    public interface IPreviewResult
    {
        string FileName { get; }

        IReadOnlyDictionary<String, Byte[]> Content { get; }
    }    

    

    

    

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


}
