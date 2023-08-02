using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUI3Demo
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        #region native
        public const int SM_CYCAPTION = 4;
        public const int SM_CYSIZE = 31;
        public const int SM_CYSMCAPTION = 51;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetSystemMetrics([In] int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetSystemMetricsForDpi([In] int nIndex, [In] uint dpi);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetDpiForWindow([In] IntPtr hwnd);
        #endregion

        public MainWindow()
        {
            this.InitializeComponent();

            var handle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var dpi = GetDpiForWindow(handle);
            var dpiRate = dpi / 96d;
            AppWindow.Resize(new SizeInt32((int)(1024 * dpiRate), (int)(600 * dpiRate)));

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(CustomTitleBar);
        }

        private void Root_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
