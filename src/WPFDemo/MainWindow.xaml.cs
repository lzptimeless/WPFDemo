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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPFFeatures.Systray;

namespace WPFDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var systrayFeature = Application.Current.GetSystrayFeature();
            systrayFeature.OpenClick += SystrayFeature_OpenClick;
            systrayFeature.ExitClick += SystrayFeature_ExitClick;

            Loaded += MainWindow_Loaded;
        }

        private void SystrayFeature_OpenClick(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;

            if (Visibility != Visibility.Visible)
                Show();

            Activate();
        }

        private void SystrayFeature_ExitClick(object? sender, EventArgs e)
        {
            Close();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.GetSingleInstanceFeature()?.OnMainWindowLoaded(this);
            Application.Current.GetSystrayFeature()?.OnMainWindowLoaded();

            HwndSource hwnd = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            hwnd.AddHook(new HwndSourceHook(HandleWindowMessages));
        }

        private IntPtr HandleWindowMessages(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            nint? result = Application.Current.GetSingleInstanceFeature()?.HandleWindowMessages(this, hwnd, msg, wParam, lParam, ref handled);
            if (handled) return result ?? IntPtr.Zero;

            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            Application.Current.GetSystrayFeature()?.Dispose();

            base.OnClosed(e);
        }
    }
}
