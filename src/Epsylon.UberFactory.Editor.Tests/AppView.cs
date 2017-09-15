using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Epsylon.UberFactory.Editor.Tests
{
    [TestClass]
    public class AppViewTest
    {
        private static string _GetAbsolutePath(string documentName)
        {
            var probeDir = System.Environment.CurrentDirectory;

            while (probeDir.Length > 3)
            {
                var absPath = System.IO.Path.Combine(probeDir, documentName);
                if (System.IO.File.Exists(absPath)) return absPath;

                probeDir = System.IO.Path.GetDirectoryName(probeDir);
            }

            return null;
        }

        [TestMethod]
        public void CreateNewProject()
        {
            var detectedPropertyChanged = false;

            var av = new AppView();
            av.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(AppView.DocumentView)) detectedPropertyChanged = true;
            };

            var docPath = _GetAbsolutePath("Test1.uberfactory");

            av.OpenKnownDocumentCmd.Execute(docPath);

            Assert.IsTrue(detectedPropertyChanged);
        }
    }
}
