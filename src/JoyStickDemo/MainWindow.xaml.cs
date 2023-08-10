using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using Windows.ApplicationModel.VoiceCommands;
using Windows.Gaming.Input;

namespace JoyStickDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region fields
        private readonly object _lock = new object();
        private readonly List<Gamepad> _myGamepads = new List<Gamepad>();
        private Gamepad? _mainGamepad;
        private Timer? _gamepadReadTimer;
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            Unloaded += MainWindow_Unloaded;
            Gamepad.GamepadAdded += Gamepad_GamepadAdded;
            Gamepad.GamepadRemoved += Gamepad_GamepadRemoved;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GetGamepads();
            _gamepadReadTimer = new Timer(GamepadReadTimerCallback, null, 0, 100);
        }

        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            _gamepadReadTimer?.Dispose();
        }

        private void Read_Click(object sender, RoutedEventArgs e)
        {
            GamepadReadTimerCallback(null);
        }

        private void GamepadReadTimerCallback(object? state)
        {
            if (!IsVisible) return;

            var gamepad = _mainGamepad;
            if (gamepad == null) return;

            GamepadReading reading = gamepad.GetCurrentReading();
            Dispatcher.Invoke(() =>
            {
                SetTriggers(reading);
                SetButtons(reading);
                SetLeftThumb(reading);
                SetRightThumb(reading);
            });
        }

        private void SetTriggers(GamepadReading reading)
        {
            double leftTrigger = reading.LeftTrigger;
            double rightTrigger = reading.RightTrigger;
            LeftTriggerTextBox.Text = leftTrigger.ToString();
            RightTriggerTextBox.Text = rightTrigger.ToString();
        }

        private void SetButtons(GamepadReading reading)
        {
            var normalF = Brushes.White;
            var pressedF = Brushes.Black;
            XButton.Foreground = (reading.Buttons & GamepadButtons.X) == GamepadButtons.X ? pressedF : normalF;
            YButton.Foreground = (reading.Buttons & GamepadButtons.Y) == GamepadButtons.Y ? pressedF : normalF;
            AButton.Foreground = (reading.Buttons & GamepadButtons.A) == GamepadButtons.A ? pressedF : normalF;
            BButton.Foreground = (reading.Buttons & GamepadButtons.B) == GamepadButtons.B ? pressedF : normalF;

            DirectionUp.Foreground = (reading.Buttons & GamepadButtons.DPadUp) == GamepadButtons.DPadUp ? pressedF : normalF;
            DirectionDown.Foreground = (reading.Buttons & GamepadButtons.DPadDown) == GamepadButtons.DPadDown ? pressedF : normalF;
            DirectionLeft.Foreground = (reading.Buttons & GamepadButtons.DPadLeft) == GamepadButtons.DPadLeft ? pressedF : normalF;
            DirectionRight.Foreground = (reading.Buttons & GamepadButtons.DPadRight) == GamepadButtons.DPadRight ? pressedF : normalF;

            LeftShoulderText.Foreground = (reading.Buttons & GamepadButtons.LeftShoulder) == GamepadButtons.LeftShoulder ? pressedF : normalF;
            RightShoulderText.Foreground = (reading.Buttons & GamepadButtons.RightShoulder) == GamepadButtons.RightShoulder ? pressedF : normalF;

            LeftThumbstickText.Foreground = (reading.Buttons & GamepadButtons.LeftThumbstick) == GamepadButtons.LeftThumbstick ? pressedF : normalF;
            RightThumbstickText.Foreground = (reading.Buttons & GamepadButtons.RightThumbstick) == GamepadButtons.RightThumbstick ? pressedF : normalF;

            MenuText.Foreground = (reading.Buttons & GamepadButtons.Menu) == GamepadButtons.Menu ? pressedF : normalF;
            ViewText.Foreground = (reading.Buttons & GamepadButtons.View) == GamepadButtons.View ? pressedF : normalF;
        }

        private void SetLeftThumb(GamepadReading reading)
        {
            double leftStickX = reading.LeftThumbstickX;   // returns a value between -1.0 and +1.0
            double leftStickY = reading.LeftThumbstickY;   // returns a value between -1.0 and +1.0

            // choose a deadzone -- readings inside this radius are ignored.
            const double deadzoneRadius = 0.1;
            const double deadzoneSquared = deadzoneRadius * deadzoneRadius;

            // Pythagorean theorem -- for a right triangle, hypotenuse^2 = (opposite side)^2 + (adjacent side)^2
            double oppositeSquared = leftStickY * leftStickY;
            double adjacentSquared = leftStickX * leftStickX;

            // accept and process input if true; otherwise, reject and ignore it.
            if ((oppositeSquared + adjacentSquared) > deadzoneSquared)
            {
                // input accepted, process it
                LeftThumbLeft.Text = (leftStickX < 0 ? Math.Abs(leftStickX) : 0).ToString();
                LeftThumbRight.Text = (leftStickX > 0 ? Math.Abs(leftStickX) : 0).ToString();
                LeftThumbUp.Text = (leftStickY > 0 ? Math.Abs(leftStickY) : 0).ToString();
                LeftThumbDown.Text = (leftStickY < 0 ? Math.Abs(leftStickY) : 0).ToString();
            }
            else
            {
                string zeroS = "0";
                LeftThumbLeft.Text = zeroS;
                LeftThumbRight.Text = zeroS;
                LeftThumbUp.Text = zeroS;
                LeftThumbDown.Text = zeroS;
            }
        }

        private void SetRightThumb(GamepadReading reading)
        {
            double rightStickX = reading.RightThumbstickX;   // returns a value between -1.0 and +1.0
            double rightStickY = reading.RightThumbstickY;   // returns a value between -1.0 and +1.0

            // choose a deadzone -- readings inside this radius are ignored.
            const double deadzoneRadius = 0.1;
            const double deadzoneSquared = deadzoneRadius * deadzoneRadius;

            // Pythagorean theorem -- for a right triangle, hypotenuse^2 = (opposite side)^2 + (adjacent side)^2
            double oppositeSquared = rightStickY * rightStickY;
            double adjacentSquared = rightStickX * rightStickX;

            // accept and process input if true; otherwise, reject and ignore it.
            if ((oppositeSquared + adjacentSquared) > deadzoneSquared)
            {
                // input accepted, process it
                RightThumbLeft.Text = (rightStickX < 0 ? Math.Abs(rightStickX) : 0).ToString();
                RightThumbRight.Text = (rightStickX > 0 ? Math.Abs(rightStickX) : 0).ToString();
                RightThumbUp.Text = (rightStickY > 0 ? Math.Abs(rightStickY) : 0).ToString();
                RightThumbDown.Text = (rightStickY < 0 ? Math.Abs(rightStickY) : 0).ToString();
            }
            else
            {
                string zeroS = "0";
                RightThumbLeft.Text = zeroS;
                RightThumbRight.Text = zeroS;
                RightThumbUp.Text = zeroS;
                RightThumbDown.Text = zeroS;
            }
        }

        private void GetGamepads()
        {
            bool exists = false;
            lock (_lock)
            {
                foreach (var gamepad in Gamepad.Gamepads)
                {
                    // Check if the gamepad is already in myGamepads; if it isn't, add it.
                    bool gamepadInList = _myGamepads.Contains(gamepad);

                    if (!gamepadInList)
                    {
                        // This code assumes that you're interested in all gamepads.
                        _myGamepads.Add(gamepad);
                    }
                }

                if (_mainGamepad == null && _myGamepads.Count > 0) _mainGamepad = _myGamepads[0];

                exists = _mainGamepad != null;
            }

            SetGamepadExists(exists);
        }

        private void Gamepad_GamepadAdded(object? sender, Gamepad e)
        {
            bool exists = false;
            // Check if the just-added gamepad is already in myGamepads; if it isn't, add
            // it.
            lock (_lock)
            {
                bool gamepadInList = _myGamepads.Contains(e);
                if (!gamepadInList)
                {
                    _myGamepads.Add(e);
                    if (_mainGamepad == null) _mainGamepad = e;
                }

                exists = _mainGamepad != null;
            }

            SetGamepadExists(exists);
        }

        private void Gamepad_GamepadRemoved(object? sender, Gamepad e)
        {
            bool exists = false;
            lock (_lock)
            {
                int indexRemoved = _myGamepads.IndexOf(e);

                if (indexRemoved > -1)
                {
                    if (_mainGamepad == _myGamepads[indexRemoved])
                    {
                        _mainGamepad = null;
                    }

                    _myGamepads.RemoveAt(indexRemoved);
                    if (_mainGamepad == null && _myGamepads.Count > 0) _mainGamepad = _myGamepads[0];
                }
                exists = _mainGamepad != null;
            }

            SetGamepadExists(exists);
        }

        private void SetGamepadExists(bool gamepadExists)
        {
            Dispatcher.Invoke(() =>
            {
                GamepadExistsTextBox.Text = gamepadExists ? "游戏手柄已插入" : "游戏手柄未插入";
                GamepadExistsTextBox.Foreground = gamepadExists ? Brushes.Green : Brushes.Black;
            });
        }
    }
}
