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

namespace Epsylon.UberFactory
{    
    public partial class AppWindow : Window
    {
        public AppWindow()
        {
            InitializeComponent();

            this.DataContext = new AppView();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var appv = this.DataContext as AppView; if (appv == null) return;
            var prjv = appv.DocumentView as ProjectVIEW.Project; if (prjv == null) return;

            if (prjv.IsDirty)
            {
                if (!appv.CloseDocument()) { e.Cancel = true; return; }
            }
        }
    }
    
}
