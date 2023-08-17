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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

        #region properties
        public TimespanPickerData TimespanPickerData { get; } = new TimespanPickerData();
        #endregion

        private void Root_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void Flyout_Opened(object sender, object e)
        {
            TimespanListBoxHours.ScrollIntoView(TimespanPickerData.Hours);
            TimespanListBoxMinutes.ScrollIntoView(TimespanPickerData.Minutes);
            TimespanListBoxSeconds.ScrollIntoView(TimespanPickerData.Seconds);
        }
    }

    public class TimespanPickerData : INotifyPropertyChanged
    {
        public TimespanPickerData()
        {
            for (int i = 0; i < 24; i++)
            {
                AllHours.Add(i);
            }

            for (int i = 0; i < 60; i++)
            {
                AllMinutes.Add(i);
            }

            for (int i = 0; i < 60; i++)
            {
                AllSeconds.Add(i);
            }

            _hours = 12;
            _minutes = 16;
            _seconds = 3;
        }

        public List<int> AllHours { get; } = new List<int>();
        public List<int> AllMinutes { get; } = new List<int>();
        public List<int> AllSeconds { get; } = new List<int>();
        private int _hours;
        public int Hours
        {
            get { return _hours; }
            set
            {
                if (value == _hours) return;

                _hours = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Text));
            }
        }
        private int _minutes;
        public int Minutes
        {
            get { return _minutes; }
            set
            {
                if (value == _minutes) return;

                _minutes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Text));
            }
        }
        private int _seconds;
        public int Seconds
        {
            get { return _seconds; }
            set
            {
                if (value == _seconds) return;

                _seconds = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Text));
            }
        }
        public string Text
        {
            get
            {
                return $"{Hours:00}:{Minutes:00}:{Seconds:00}";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            // Raise the PropertyChanged event, passing the name of the property whose value has changed.
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
