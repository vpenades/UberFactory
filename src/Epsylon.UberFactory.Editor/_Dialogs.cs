using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Epsylon.UberFactory
{
    using TASKFUNC = Func<System.Threading.CancellationToken, IProgress<float>, Object>;

    internal static class _Dialogs
    {
        #region threading

        public static Object RunWithDialog(this TASKFUNC task)
        {
            return Themes.TaskMonitorDialog.RunTask(task);
        }

        public static Object RunWithDialog(this Func<System.Threading.CancellationToken, Object> task)
        {
            TASKFUNC xtask = (c, p) => { p.Report(-1); return task(c); };

            return xtask.RunWithDialog();
        }

        public static void RunWithDialog(this Action<System.Threading.CancellationToken, IProgress<float>> task)
        {
            TASKFUNC xtask = (c, p) => { task(c, p); return null; };

            xtask.RunWithDialog();
        }

        public static void RunWithDialog(this Action<System.Threading.CancellationToken> task)
        {
            TASKFUNC xtask = (c, p) => { p.Report(-1); task(c); return null; };

            xtask.RunWithDialog();
        }

        #endregion

        #region dialogs

        // https://blogs.msdn.microsoft.com/wpfsdk/2006/10/26/uncommon-dialogs-font-chooser-color-picker-dialogs/

        public static bool QueryDeletePermanentlyWarningDialog(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) title = "Item";

            var result = MessageBox.Show("'" + title + "' " + "will be deleted permanently.", System.Windows.Application.Current.MainWindow.Title, MessageBoxButton.OKCancel, MessageBoxImage.Warning);

            return result == MessageBoxResult.OK;
        }

        public static void ShowErrorDialog(string message)
        {
            MessageBox.Show(message, System.Windows.Application.Current.MainWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);            
        }

        public static MessageBoxResult ShowSaveChangesDialog(string item)
        {
            return MessageBox.Show("Save changes to '" + item + "'?", System.Windows.Application.Current.MainWindow.Title, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
        }

        public static PathString ShowOpenFileDialog(string fileFilter, PathString startDir)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog()
            {
                RestoreDirectory = true,
                Filter = fileFilter,
                AddExtension = true,
                DereferenceLinks = true
            };

            if (startDir.DirectoryExists) dlg.InitialDirectory = startDir;

            if (!dlg.ShowDialog().Value) return PathString.Empty;

            return new PathString(dlg.FileName);
        }

        public static PathString ShowBrowseDirectoryDialog(PathString startDir)
        {
            var dlg = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog();

            if (startDir.DirectoryExists) dlg.DefaultDirectory = startDir;

            dlg.IsFolderPicker = true;
            dlg.RestoreDirectory = true;

            var dlgResult = dlg.ShowDialog();

            if (dlgResult != Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok) return PathString.Empty;

            return new PathString(dlg.FileName);
        }


        public static UInt32 ShowColorPickerDialog(UInt32 color)
        {
            var r = color & 255; color >>= 8;
            var g = color & 255; color >>= 8;
            var b = color & 255; color >>= 8;
            var a = color & 255;
            var c = System.Windows.Media.Color.FromArgb((Byte)a, (Byte)r, (Byte)g, (Byte)b);

            if (!ColorPickerWPF.ColorPickerWindow.ShowDialog(out c)) return color;

            color = c.A; color <<= 8;
            color |= c.B; color <<= 8;
            color |= c.G; color <<= 8;
            color |= c.R;

            return color;
        }


        

        public static bool ShowGenericDialog<T>(Window owner,string title, Object data) where T: System.Windows.FrameworkElement
        {
            // AssemblyContext.SetAssemblyResolver(null);

            var dlg = Themes.GenericDialog.Create<T>(owner, title, data);

            dlg.ShowDialog();

            return dlg.DialogResult.Value;
        }        

        
        public static void ShowPluginsManagerDialog(AppView app)
        {
            _Dialogs.ShowGenericDialog<Themes.PluginsPanel>(null, "Plugins Manager", new PluginsCollectionView(app,null,null,null));
        }

        public static void ShowPluginsManagerDialog(AppView app, Func<string,bool> check, Action<string> insert, Action<string> remove)
        {
            _Dialogs.ShowGenericDialog<Themes.PluginsPanel>(null, "Plugins Manager", new PluginsCollectionView(app,check,insert,remove));
        }



        private class _SelectItemFromListData<T>
        {
            public T[] Collection { get; set; }

            public T Selected { get; set; }
        }


        public static Factory.ContentFilterInfo ShowNewNodeDialog(Window owner, IEnumerable<Factory.ContentFilterInfo> filters)
        {
            var data = new _SelectItemFromListData<Factory.ContentFilterInfo>() { Collection = filters.ToArray() };

            if (!ShowGenericDialog<Themes.NewNodeSelector>(owner, "New Node", data)) return null;

            return data.Selected;
        }        


        public static void ShowProductAndDispose(Window owner, Object product)
        {
            if (product == null) return;

            var dlg = Themes.GenericDialog.Create<Themes.PreviewResultPanel>(null, "Product", product);
            dlg.ResizeMode = ResizeMode.CanResizeWithGrip;
            dlg.ShowDialog();

            if (product is IDisposable) ((IDisposable)product).Dispose();
        }

        

        public static void ShowAboutDialog(Window owner) { ShowGenericDialog<Themes.AboutPanel>(owner, "About", null); }

        #endregion

    }
}
