﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ShaderPluginGUI
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Close();

            MainWindow MainWnd = new MainWindow();
            WindowInteropHelper windowInteropHelper = new WindowInteropHelper(MainWnd)
            {
                Owner = Program.PhotoshopWindowPointer
            };
            MainWnd.ShowDialog();

        }
    }
}
