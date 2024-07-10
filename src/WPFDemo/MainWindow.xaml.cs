using System;
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
using WPFDemo.ViewModels;
using WPFFeatures.Systray;
using WpfI18n.Sources;

namespace WPFDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            var systrayFeature = Application.Current.GetSystrayFeature();
            if (systrayFeature != null)
            {
                systrayFeature.OpenClick += SystrayFeature_OpenClick;
                systrayFeature.ExitClick += SystrayFeature_ExitClick;
            }

            var singleInstanceFeature = Application.Current.GetSingleInstanceFeature();
            if (singleInstanceFeature != null)
            {
                singleInstanceFeature.ShowWindow += SingleInstance_ShowWindow;
            }

            Loaded += MainWindow_Loaded;
            DataContext = _viewModel = new MainViewModel();
        }

        private void SystrayFeature_OpenClick(object? sender, EventArgs e)
        {
            Restore();
        }

        private void SystrayFeature_ExitClick(object? sender, EventArgs e)
        {
            Close();
        }

        private void SingleInstance_ShowWindow(object? sender, WPFFeatures.SingleInstance.ShowWindowArgs e)
        {
            Restore();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            HwndSource hwnd = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            hwnd.AddHook(new HwndSourceHook(HandleWindowMessages));

            Application.Current.GetSingleInstanceFeature()?.OnMainWindowLoaded(hwnd.Handle);
            Application.Current.GetSystrayFeature()?.OnMainWindowLoaded();
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

        private void Restore()
        {
            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;

            if (Visibility != Visibility.Visible)
                Show();

            Activate();
        }

        private void LanguageSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lm = LanguageManager.Default;
            if (LanguageSelection.SelectedIndex == 0)
            {
                if (lm.IetfTag != "en") lm.Load("en");
            }
            else
            {
                if (lm.IetfTag != "cn") lm.Load("cn");
            }
        }

        private void AddHoursButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Hours++;
        }
    }
}
