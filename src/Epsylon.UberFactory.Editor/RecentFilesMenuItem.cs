using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

using Microsoft.Win32;

// BASED ON: http://www.codeproject.com/Articles/23731/RecentFileList-a-WPF-MRU

namespace Epsylon.UberFactory
{
    public static class RecentFilesManager
    {
        #region lifecycle

        static RecentFilesManager()
        {
            MaxNumberOfFiles = 9;
        }

        #endregion

        #region types

        public interface IPersist
        {
            IEnumerable<string> RecentFiles(int max);
            void InsertFile(string filepath, int max);
            void RemoveFile(string filepath, int max);
        }

        private static class _ApplicationAttributes
        {
            static readonly Assembly _Assembly = null;

            internal static readonly AssemblyTitleAttribute _Title = null;
            internal static readonly AssemblyCompanyAttribute _Company = null;
            internal static readonly AssemblyCopyrightAttribute _Copyright = null;
            internal static readonly AssemblyProductAttribute _Product = null;

            public static string Title { get; private set; }
            public static string CompanyName { get; private set; }
            public static string Copyright { get; private set; }
            public static string ProductName { get; private set; }

            static Version _Version = null;
            public static string Version { get; private set; }

            static _ApplicationAttributes()
            {
                try
                {
                    Title = String.Empty;
                    CompanyName = String.Empty;
                    Copyright = String.Empty;
                    ProductName = String.Empty;
                    Version = String.Empty;

                    _Assembly = Assembly.GetEntryAssembly();

                    if (_Assembly != null)
                    {
                        object[] attributes = _Assembly.GetCustomAttributes(false);

                        foreach (object attribute in attributes)
                        {
                            Type type = attribute.GetType();

                            if (type == typeof(AssemblyTitleAttribute)) _Title = (AssemblyTitleAttribute)attribute;
                            if (type == typeof(AssemblyCompanyAttribute)) _Company = (AssemblyCompanyAttribute)attribute;
                            if (type == typeof(AssemblyCopyrightAttribute)) _Copyright = (AssemblyCopyrightAttribute)attribute;
                            if (type == typeof(AssemblyProductAttribute)) _Product = (AssemblyProductAttribute)attribute;
                        }

                        _Version = _Assembly.GetName().Version;
                    }

                    if (_Title != null) Title = _Title.Title;
                    if (_Company != null) CompanyName = _Company.Company;
                    if (_Copyright != null) Copyright = _Copyright.Copyright;
                    if (_Product != null) ProductName = _Product.Product;
                    if (_Version != null) Version = _Version.ToString();
                }
                catch { }
            }
        }

        internal class _RegistryPersister : IPersist
        {
            public string RegistryKey { get; set; }

            public _RegistryPersister()
            {
                RegistryKey =
                    "Software\\" +
                    _ApplicationAttributes.CompanyName + "\\" +
                    _ApplicationAttributes.ProductName + "\\" +
                    "RecentFileList";
            }

            public _RegistryPersister(string key)
            {
                RegistryKey = key;
            }

            string Key(int i) { return i.ToString("00"); }

            public IEnumerable<string> RecentFiles(int max)
            {
                var k = Registry.CurrentUser.OpenSubKey(RegistryKey);
                if (k == null) k = Registry.CurrentUser.CreateSubKey(RegistryKey);

                var list = new List<string>(max);

                for (int i = 0; i < max; i++)
                {
                    string filename = (string)k.GetValue(Key(i));

                    if (String.IsNullOrEmpty(filename)) break;

                    list.Add(filename);
                }

                return list;
            }

            public void InsertFile(string filepath, int max)
            {
                var k = Registry.CurrentUser.OpenSubKey(RegistryKey);
                if (k == null) Registry.CurrentUser.CreateSubKey(RegistryKey);
                k = Registry.CurrentUser.OpenSubKey(RegistryKey, true);

                RemoveFile(filepath, max);

                for (int i = max - 2; i >= 0; i--)
                {
                    string sThis = Key(i);
                    string sNext = Key(i + 1);

                    object oThis = k.GetValue(sThis);
                    if (oThis == null) continue;

                    k.SetValue(sNext, oThis);
                }

                k.SetValue(Key(0), filepath);
            }

            public void RemoveFile(string filepath, int max)
            {
                var k = Registry.CurrentUser.OpenSubKey(RegistryKey);
                if (k == null) return;

                for (int i = 0; i < max; i++)
                {
                again:
                    string s = (string)k.GetValue(Key(i));
                    if (s != null && s.Equals(filepath, StringComparison.CurrentCultureIgnoreCase))
                    {
                        RemoveFile(i, max);
                        goto again;
                    }
                }
            }

            void RemoveFile(int index, int max)
            {
                var k = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
                if (k == null) return;

                k.DeleteValue(Key(index), false);

                for (int i = index; i < max - 1; i++)
                {
                    string sThis = Key(i);
                    string sNext = Key(i + 1);

                    object oNext = k.GetValue(sNext);
                    if (oNext == null) break;

                    k.SetValue(sThis, oNext);
                    k.DeleteValue(sNext);
                }
            }
        }

        internal class _XmlPersister : IPersist
        {
            public string Filepath { get; set; }
            public Stream Stream { get; set; }

            public _XmlPersister()
            {
                Filepath =
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        _ApplicationAttributes.CompanyName + "\\" +
                        _ApplicationAttributes.ProductName + "\\" +
                        "RecentFileList.xml");
            }

            public _XmlPersister(string filepath) { Filepath = filepath; }

            public _XmlPersister(Stream stream) { Stream = stream; }

            public IEnumerable<string> RecentFiles(int max) { return Load(max); }

            public void InsertFile(string filepath, int max) { Update(filepath, true, max); }

            public void RemoveFile(string filepath, int max) { Update(filepath, false, max); }

            private void Update(string filepath, bool insert, int max)
            {
                var old = Load(max);

                var list = new List<string>();

                if (insert) list.Add(filepath);

                CopyExcluding(old, filepath, list, max);

                Save(list, max);
            }

            private void CopyExcluding(IEnumerable<string> source, string exclude, List<string> target, int max)
            {
                foreach (string s in source)
                    if (!String.IsNullOrEmpty(s))
                        if (!s.Equals(exclude, StringComparison.OrdinalIgnoreCase))
                            if (target.Count < max)
                                target.Add(s);
            }

            private sealed class SmartStream : IDisposable
            {
                bool _IsStreamOwned = true;
                Stream _Stream = null;

                public Stream Stream { get { return _Stream; } }

                public static implicit operator Stream(SmartStream me) { return me.Stream; }

                public SmartStream(string filepath, FileMode mode)
                {
                    _IsStreamOwned = true;

                    Directory.CreateDirectory(Path.GetDirectoryName(filepath));

                    _Stream = File.Open(filepath, mode);
                }

                public SmartStream(Stream stream)
                {
                    _IsStreamOwned = false;
                    _Stream = stream;
                }

                public void Dispose()
                {
                    if (_IsStreamOwned && _Stream != null) _Stream.Dispose();

                    _Stream = null;
                }
            }

            private SmartStream OpenStream(FileMode mode)
            {
                if (!String.IsNullOrEmpty(Filepath))
                {
                    return new SmartStream(Filepath, mode);
                }
                else
                {
                    return new SmartStream(Stream);
                }
            }

            private IEnumerable<string> Load(int max)
            {
                var list = new List<string>(max);

                using (var ms = new MemoryStream())
                {
                    using (var ss = OpenStream(FileMode.OpenOrCreate))
                    {
                        if (ss.Stream.Length == 0) return list;

                        ss.Stream.Position = 0;

                        var buffer = new byte[1 << 20];
                        for (; ; )
                        {
                            int bytes = ss.Stream.Read(buffer, 0, buffer.Length);
                            if (bytes == 0) break;
                            ms.Write(buffer, 0, bytes);
                        }

                        ms.Position = 0;
                    }

                    using (var x = new XmlTextReader(ms))
                    {
                        while (x.Read())
                        {
                            switch (x.NodeType)
                            {
                                case XmlNodeType.XmlDeclaration:
                                case XmlNodeType.Whitespace:
                                    break;

                                case XmlNodeType.Element:
                                    switch (x.Name)
                                    {
                                        case "RecentFiles": break;

                                        case "RecentFile":
                                            if (list.Count < max) list.Add(x.GetAttribute(0));
                                            break;

                                        default: Debug.Assert(false); break;
                                    }
                                    break;

                                case XmlNodeType.EndElement:
                                    switch (x.Name)
                                    {
                                        case "RecentFiles": return list;
                                        default: Debug.Assert(false); break;
                                    }
                                    break;

                                default:
                                    Debug.Assert(false);
                                    break;
                            }
                        }
                    }                    
                }
                return list;
            }

            private void Save(List<string> list, int max)
            {
                using (var ms = new MemoryStream())
                {
                    var x = new XmlTextWriter(ms, Encoding.UTF8);
                    
                    if (x == null) { Debug.Assert(false); return; }

                    x.Formatting = Formatting.Indented;

                    x.WriteStartDocument();

                    x.WriteStartElement("RecentFiles");

                    foreach (string filepath in list)
                    {
                        x.WriteStartElement("RecentFile");
                        x.WriteAttributeString("Filepath", filepath);
                        x.WriteEndElement();
                    }

                    x.WriteEndElement();

                    x.WriteEndDocument();

                    x.Flush();

                    using (var ss = OpenStream(FileMode.Create))
                    {
                        ss.Stream.SetLength(0);

                        ms.Position = 0;

                        var buffer = new byte[1 << 20];
                        for (; ; )
                        {
                            int bytes = ms.Read(buffer, 0, buffer.Length);
                            if (bytes == 0) break;
                            ss.Stream.Write(buffer, 0, bytes);
                        }
                    }
                }                
            }
        }

        internal class _JumpListPersister : IPersist
        {
            internal _JumpListPersister(IPersist parent) { _Parent = parent; }

            private readonly IPersist _Parent;


            public void InsertFile(string filepath, int max)
            {
                _Parent.InsertFile(filepath, max);

                // now we add it to the shell's jump list
                var jtask = new System.Windows.Shell.JumpTask()
                {
                    Title = System.IO.Path.GetFileName(filepath),

                    ApplicationPath = System.Reflection.Assembly.GetEntryAssembly().Location
                };
                jtask.IconResourcePath = jtask.ApplicationPath;

                if (filepath.ContainsWhiteSpaces()) filepath = filepath.Wrap('"');

                jtask.Arguments = filepath;

                System.Windows.Shell.JumpList.AddToRecentCategory(jtask);                
            }

            public IEnumerable<string> RecentFiles(int max) { return _Parent.RecentFiles(max); }

            public void RemoveFile(string filepath, int max) { _Parent.RemoveFile(filepath, max); }
        }


        #endregion

        #region data

        private static RecentFilesManager.IPersist _Persister;        

        #endregion

        #region properties       

        public static int MaxNumberOfFiles { get; set; }

        public static IEnumerable<string> RecentFiles { get { return _Persister.RecentFiles(MaxNumberOfFiles); } }

        #endregion

        #region API        

        public static void UseRegistryPersister() { _Persister = new _JumpListPersister(new RecentFilesManager._RegistryPersister()); }
        public static void UseRegistryPersister(string key) { _Persister = new _JumpListPersister(new RecentFilesManager._RegistryPersister(key)); }

        public static void UseXmlPersister() { _Persister = new _JumpListPersister(new RecentFilesManager._XmlPersister()); }
        public static void UseXmlPersister(string filepath) { _Persister = new _JumpListPersister(new RecentFilesManager._XmlPersister(filepath)); }
        public static void UseXmlPersister(Stream stream) { _Persister = new _JumpListPersister(new RecentFilesManager._XmlPersister(stream)); }

        public static void RemoveFile(string filepath)
        {
            // if (!IsValidFullPath(filepath)) throw new ArgumentException(filepath);
            _Persister.RemoveFile(filepath, MaxNumberOfFiles);
        }

        public static void InsertFile(string filepath)
        {
            if (!IsValidFullPath(filepath)) throw new ArgumentException(filepath);
            _Persister.InsertFile(filepath, MaxNumberOfFiles);
        }

        #endregion

        #region string utils

        public static bool IsValidFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            if (System.IO.Path.GetInvalidPathChars().Intersect(path.ToCharArray()).Count() > 0) return false;
            if (!System.IO.Path.IsPathRooted(path)) return false;

            return true;
        }

        // This method is taken from Joe Woodbury's article at: http://www.codeproject.com/KB/cs/mrutoolstripmenu.aspx

        /// <summary>
        /// Shortens a pathname for display purposes.
        /// </summary>
        /// <param labelName="pathname">The pathname to shorten.</param>
        /// <param labelName="maxLength">The maximum number of characters to be displayed.</param>
        /// <remarks>Shortens a pathname by either removing consecutive components of a path
        /// and/or by removing characters from the end of the filename and replacing
        /// then with three elipses (...)
        /// <para>In all cases, the root of the passed path will be preserved in it's entirety.</para>
        /// <para>If a UNC path is used or the pathname and maxLength are particularly short,
        /// the resulting path may be longer than maxLength.</para>
        /// <para>This method expects fully resolved pathnames to be passed to it.
        /// (Use Path.GetFullPath() to obtain this.)</para>
        /// </remarks>
        /// <returns></returns>
        public static string ShortenPathName(string pathname, int maxLength)
        {
            if (pathname.Length <= maxLength)
                return pathname;

            string root = Path.GetPathRoot(pathname);
            if (root.Length > 3)
                root += Path.DirectorySeparatorChar;

            string[] elements = pathname.Substring(root.Length).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            int filenameIndex = elements.GetLength(0) - 1;

            if (elements.GetLength(0) == 1) // pathname is just a root and filename
            {
                if (elements[0].Length > 5) // long enough to shorten
                {
                    // if path is a UNC path, root may be rather long
                    if (root.Length + 6 >= maxLength)
                    {
                        return root + elements[0].Substring(0, 3) + "...";
                    }
                    else
                    {
                        return pathname.Substring(0, maxLength - 3) + "...";
                    }
                }
            }
            else if ((root.Length + 4 + elements[filenameIndex].Length) > maxLength) // pathname is just a root and filename
            {
                root += "...\\";

                int len = elements[filenameIndex].Length;
                if (len < 6)
                    return root + elements[filenameIndex];

                if ((root.Length + 6) >= maxLength)
                {
                    len = 3;
                }
                else
                {
                    len = maxLength - root.Length - 3;
                }
                return root + elements[filenameIndex].Substring(0, len) + "...";
            }
            else if (elements.GetLength(0) == 2)
            {
                return root + "...\\" + elements[1];
            }
            else
            {
                int len = 0;
                int begin = 0;

                for (int i = 0; i < filenameIndex; i++)
                {
                    if (elements[i].Length > len)
                    {
                        begin = i;
                        len = elements[i].Length;
                    }
                }

                int totalLength = pathname.Length - len + 3;
                int end = begin + 1;

                while (totalLength > maxLength)
                {
                    if (begin > 0)
                        totalLength -= elements[--begin].Length - 1;

                    if (totalLength <= maxLength)
                        break;

                    if (end < filenameIndex)
                        totalLength -= elements[++end].Length - 1;

                    if (begin == 0 && end == filenameIndex)
                        break;
                }

                // assemble final string

                for (int i = 0; i < begin; i++)
                {
                    root += elements[i] + '\\';
                }

                root += "...\\";

                for (int i = end; i < filenameIndex; i++)
                {
                    root += elements[i] + '\\';
                }

                return root + elements[filenameIndex];
            }
            return pathname;
        }

        #endregion
    }

    
    public class RecentFilesMenuItem : Separator
        {
            #region lifecycle

            static RecentFilesMenuItem()
            {
                DefaultStyleKeyProperty.OverrideMetadata(typeof(RecentFilesMenuItem), new FrameworkPropertyMetadata(typeof(Separator)));
            }

            public RecentFilesMenuItem()
            {
                MaxPathLength = 50;
                MenuItemFormatOneToNine = "_{0}:  {2}";
                MenuItemFormatTenPlus = "{0}:  {2}";

                this.Loaded += (s, e) => _HookFileMenu();
            }

            private void _HookFileMenu()
            {
                var parent = Parent as MenuItem;
                if (parent == null) throw new ApplicationException("Parent must be a MenuItem");

                if (FileMenu == parent) return;

                if (FileMenu != null) FileMenu.SubmenuOpened -= _FileMenu_SubmenuOpened;

                FileMenu = parent;
                FileMenu.SubmenuOpened += _FileMenu_SubmenuOpened;
            }

            void _FileMenu_SubmenuOpened(object sender, RoutedEventArgs e) { SetMenuItems(); }

            #endregion

            #region dependency properties

            public static DependencyProperty LoadCommandProperty = DependencyProperty.Register("LoadCommand", typeof(System.Windows.Input.ICommand), typeof(RecentFilesMenuItem));

            #endregion

            #region data        

            private Separator _Separator = null;

            private sealed class _RecentFile
            {
                #region lifecycle

                public static IEnumerable<_RecentFile> Create(IEnumerable<string> recentFiles)
                {
                    var srcList = recentFiles;
                    var dstList = new List<_RecentFile>();

                    int i = 0;
                    foreach (string filepath in srcList)
                    {
                        if (!System.IO.File.Exists(filepath)) continue;
                        dstList.Add(new _RecentFile(i++, filepath));
                    }

                    return dstList;
                }

                private _RecentFile(int number, string filepath)
                {
                    this.Number = number;
                    this.Filepath = filepath;
                }

                #endregion

                #region data

                public int Number = 0;
                public string Filepath = "";
                public MenuItem MenuItem = null;

                #endregion

                #region properties

                public string DisplayPath
                {
                    get
                    {
                        return Path.Combine(
                            Path.GetDirectoryName(Filepath),
                            Path.GetFileNameWithoutExtension(Filepath));
                    }
                }

                #endregion
            }

            private static _RecentFile[] _RecentFiles = null;

            #endregion

            #region properties

            public System.Windows.Input.ICommand LoadCommand
            {
                get { return (System.Windows.Input.ICommand)GetValue(LoadCommandProperty); }
                set { SetValue(LoadCommandProperty, value); }
            }

            public int MaxPathLength { get; set; }

            public MenuItem FileMenu { get; private set; }

            /// <summary>
            /// Used in: String.Format( MenuItemFormat, index, filepath, displayPath );
            /// Default = "_{0}:  {2}"
            /// </summary>
            public string MenuItemFormatOneToNine { get; set; }

            /// <summary>
            /// Used in: String.Format( MenuItemFormat, index, filepath, displayPath );
            /// Default = "{0}:  {2}"
            /// </summary>
            public string MenuItemFormatTenPlus { get; set; }

            #endregion

            #region API

            public delegate string GetMenuItemTextDelegate(int index, string filepath);
            public GetMenuItemTextDelegate GetMenuItemTextHandler { get; set; }

            void SetMenuItems()
            {
                RemoveMenuItems();

                _RecentFiles = _RecentFile.Create(RecentFilesManager.RecentFiles).ToArray();

                InsertMenuItems();
            }

            void RemoveMenuItems()
            {
                if (_Separator != null) FileMenu.Items.Remove(_Separator);

                if (_RecentFiles != null)
                    foreach (var r in _RecentFiles)
                        if (r.MenuItem != null)
                            FileMenu.Items.Remove(r.MenuItem);

                _Separator = null;
                _RecentFiles = null;
            }

            void InsertMenuItems()
            {
                if (_RecentFiles == null) return;
                if (_RecentFiles.Length == 0) return;


                int iMenuItem = FileMenu.Items.IndexOf(this);
                foreach (var r in _RecentFiles)
                {
                    string header = GetMenuItemText(r.Number + 1, r.Filepath, r.DisplayPath);

                    r.MenuItem = new MenuItem { Header = header };
                    r.MenuItem.Click += MenuItem_Click;

                    FileMenu.Items.Insert(++iMenuItem, r.MenuItem);
                }

                _Separator = new Separator();
                FileMenu.Items.Insert(++iMenuItem, _Separator);
            }

            private void MenuItem_Click(object sender, EventArgs e) { var menuItem = sender as MenuItem; OnMenuClick(menuItem); }

            private string GetMenuItemText(int index, string filepath, string displaypath)
            {
                var delegateGetMenuItemText = GetMenuItemTextHandler;
                if (delegateGetMenuItemText != null) return delegateGetMenuItemText(index, filepath);

                string format = index < 10 ? MenuItemFormatOneToNine : MenuItemFormatTenPlus;

                string shortPath = RecentFilesManager.ShortenPathName(displaypath, MaxPathLength);

                return String.Format(format, index, filepath, shortPath);
            }

            protected virtual void OnMenuClick(MenuItem menuItem)
            {
                string filepath = GetFilepath(menuItem);

                if (String.IsNullOrEmpty(filepath)) return;

                if (LoadCommand == null) return;
                if (LoadCommand.CanExecute(filepath)) LoadCommand.Execute(filepath);
            }

            private string GetFilepath(MenuItem menuItem)
            {
                var rf = _RecentFiles.FirstOrDefault(item => item.MenuItem == menuItem);
                return rf == null ? string.Empty : rf.Filepath;
            }

            #endregion
        }
    
}
