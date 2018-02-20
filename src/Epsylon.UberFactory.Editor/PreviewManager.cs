using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    static class PreviewManager
    {
        #region data

        private static readonly string _TempDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "UberFactory.Previews");

        private static readonly Object _Lock = new object();
        private static readonly Dictionary<string, int> _OpenDocuments = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region core

        private static void _CleanupPreviewFiles(TimeSpan lifespan)
        {
            var dir = _TempDirectory;
            if (!System.IO.Directory.Exists(dir)) return;

            var docdirs = System.IO.Directory.GetDirectories(dir);

            foreach(var docdir in docdirs)
            {
                var docName = System.IO.Path.GetFileName(docdir);

                if (!long.TryParse(docName, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out long dt)) continue;

                var dtdt = DateTime.FromBinary(dt);

                if (DateTime.Now - dtdt < lifespan) continue;

                _CleanupPreviewDirectory(docdir);
            }
        }

        private static void _CleanupPreviewFiles()
        {
            var dir = _TempDirectory;
            if (!System.IO.Directory.Exists(dir)) return;

            var docdirs = System.IO.Directory.GetDirectories(dir);

            foreach (var docdir in docdirs)
            {
                _CleanupPreviewDirectory(docdir);
            }
        }

        private static void _CleanupPreviewDirectory(string docdir)
        {
            lock (_Lock)
            {
                var docName = System.IO.Path.GetFileName(docdir);

                try
                {
                    if (_OpenDocuments.TryGetValue(docName, out int pid))
                    {
                        var p = System.Diagnostics.Process.GetProcessById(pid);

                        if (!p.HasExited) return;
                    }
                }
                catch { }

                try
                {
                    System.IO.Directory.Delete(docdir, true);

                    _OpenDocuments.Remove(docName);
                }
                catch { }
            }
        }

        private static string _GetDocumentPreviewDirectory()
        {
            _CleanupPreviewFiles();
            
            var path = System.IO.Path.Combine(_TempDirectory, DateTime.Now.ToBinary().ToString("X"));

            System.IO.Directory.CreateDirectory(path);

            return path;
        }

        #endregion

        #region API

        public static void ShowPreview(Evaluation.IMultiFileContent result)
        {
            if (result == null) return;            

            var dirPath = _GetDocumentPreviewDirectory();

            // copy all the contents to the temp directory
            foreach(var f in result.Content)
            {
                var ff = System.IO.Path.Combine(dirPath, f.Key);
                System.IO.File.WriteAllBytes(ff, f.Value);
            }

            var fileName = System.IO.Path.Combine(dirPath, result.FileName);

            try
            {
                var pinfo = new System.Diagnostics.ProcessStartInfo(fileName);
                
                // setup verb
                var verbIndex = pinfo.Verbs.IndexOf(item => item.ToLower() == "open");
                if (verbIndex >= 0) pinfo.Verb = pinfo.Verbs[verbIndex];                

                // run
                using (var process = System.Diagnostics.Process.Start(pinfo))
                {
                    _OpenDocuments[System.IO.Path.GetFileName(dirPath)] = process.Id;
                }
                    
            }
            catch(Exception ex) { }            
        }

        #endregion

    }
}
