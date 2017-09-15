using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Epsylon.UberFactory.Editor.Tests
{
    [TestClass]
    public class AppViewTest
    {
        [TestMethod]
        public void CreateNewProject()
        {
            var detectedPropertyChanged = false;

            var av = new AppView();
            av.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(AppView.DocumentView)) detectedPropertyChanged = true;
            };

            av.NewDocumentCmd.Execute(null);

            Assert.IsTrue(detectedPropertyChanged);
        }
    }
}
