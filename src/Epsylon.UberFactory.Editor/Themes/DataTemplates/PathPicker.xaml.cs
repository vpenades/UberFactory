﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Epsylon.UberFactory.Themes.DataTemplates
{
    
    public partial class PathPicker : UserControl
    {
        public PathPicker()
        {
            InitializeComponent();
        }

        private void _OnClick_PickFile(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is Bindings.InputValueBinding<String> vbinding)
            {
                var startDir = new PathString(vbinding.DataContext.BuildContext.GetSourceAbsolutePath("dummy.txt")).DirectoryPath;

                var path = PathString.Empty;

                if (vbinding.IsFilePicker) path = _Dialogs.ShowOpenFileDialog(vbinding.GetFileFilter(), startDir);
                if (vbinding.IsDirectoryPicker) path = _Dialogs.ShowBrowseDirectoryDialog(startDir);

                if (path.IsEmpty) return;

                Bindings.InputValueBinding<String>.SetAbsoluteSourcePath(vbinding, path);
            }
        }
    }
}
