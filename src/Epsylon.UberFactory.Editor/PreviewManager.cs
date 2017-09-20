using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    static class PreviewManager
    {
        private static readonly string _TempDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "UberFactory.Previews");        

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
                
                try { System.IO.Directory.Delete(docdir, true); } catch { }                
            }
        }

        private static string _GetDocumentPreviewDirectory()
        {
            _CleanupPreviewFiles(TimeSpan.FromMinutes(30));
            
            var path = System.IO.Path.Combine(_TempDirectory, DateTime.Now.ToBinary().ToString("X"));

            System.IO.Directory.CreateDirectory(path);

            return path;
        }

        public static void ShowPreview(Evaluation.IPreviewResult result)
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

            try { System.Diagnostics.Process.Start(fileName); }
            catch { }            
        }


    }
}
