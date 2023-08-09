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
            Gamepad.GamepadAdded += Gamepad_GamepadAdded;
            Gamepad.GamepadRemoved += Gamepad_GamepadRemoved;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GetGamepads();
            _gamepadReadTimer = new Timer(GamepadReadTimerCallback, null, 0, 100);
        }

        private void GamepadReadTimerCallback(object? state)
        {
            var gamepad = _mainGamepad;
            if (gamepad == null) return;

            GamepadReading reading = gamepad.GetCurrentReading();
            SetTriggers(reading);
            SetButtons(reading);
            SetLeftThumb(reading);
            SetRightThumb(reading);
        }

        private void SetTriggers(GamepadReading reading)
        {
            double leftTrigger = reading.LeftTrigger;
            double rightTrigger = reading.RightTrigger;
            LeftTriggerTextBox.Text = (leftTrigger > 0.1 ? leftTrigger : 0).ToString();
            RightTriggerTextBox.Text = (rightTrigger > 0.1 ? rightTrigger : 0).ToString();
        }

        private void SetButtons(GamepadReading reading)
        {
            var normalF = Brushes.White;
            var pressedF = Brushes.Black;
            XButton.Foreground = (reading.Buttons & GamepadButtons.X) == GamepadButtons.X ? pressedF : normalF;
            YButton.Foreground = (reading.Buttons & GamepadButtons.Y) == GamepadButtons.Y ? pressedF : normalF;
            AButton.Foreground = (reading.Buttons & GamepadButtons.A) == GamepadButtons.A ? pressedF : normalF;
            BButton.Foreground = (reading.Buttons & GamepadButtons.B) == GamepadButtons.B ? pressedF : normalF;
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
            GamepadExistsTextBox.Text = gamepadExists ? "游戏手柄已插入" : "游戏手柄未插入";
            GamepadExistsTextBox.Foreground = gamepadExists ? Brushes.Green : Brushes.Black;
        }
    }
}
