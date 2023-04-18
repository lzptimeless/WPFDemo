using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace WPFFeatures.SingleInstance
{
    /// <summary>
    /// 单实例功能
    /// </summary>
    internal class SingleInstanceFeature : IDisposable
    {
        #region win32
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(nint hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern uint RegisterWindowMessage(string lpProcName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ChangeWindowMessageFilterEx(nint hwnd, uint message, ChangeWindowMessageFilterExAction action, nint pChangeFilterStruct = 0);

        public enum ChangeWindowMessageFilterExAction : uint
        {
            Reset = 0, Allow = 1, DisAllow = 2
        };
        #endregion

        #region fields
        /// <summary>
        /// 用以保证单实例的Mutex
        /// </summary>
        private Mutex? _singleInstanceMutex;
        /// <summary>
        /// _singleInstanceMutex的名称
        /// </summary>
        private readonly string _mutexName;
        /// <summary>
        /// 通知已存在的实列打开主窗口的消息名称
        /// </summary>
        private readonly string _wndMsgShowName;
        /// <summary>
        /// 通知已存在的实列打开主窗口的消息Id
        /// </summary>
        private uint _wndMsgShow;
        #endregion
        /// <summary>
        /// 单实例功能
        /// </summary>
        /// <param name="mutexName">用以保证单实例的Mutex的名称，默认为{Assembly.FullName}.mutex</param>
        /// <param name="wndMsgShowName">通知已存在的实列打开主窗口的消息名称，默认为{Assembly.FullName}.show</param>
        public SingleInstanceFeature(string? mutexName = null, string? wndMsgShowName = null)
        {
            var appName = Assembly.GetEntryAssembly()!.GetName().Name!;
            _mutexName = mutexName ?? $"{appName}.mutex";
            _wndMsgShowName = wndMsgShowName ?? $"{appName}.show";
        }

        #region public methods
        /// <summary>
        /// 在Application.OnStartup中调用
        /// </summary>
        /// <returns>true：当前实例为单实例，false：已经存在一个实例，本实例需要退出</returns>
        public bool OnStartup()
        {
            if (!CreateSingleInstanceMutex())
            {
                // 本次启动属于重复启动，直接退出
                // 通知已经存在的实例打开窗口，注意这个操作需要发送窗口消息给已经存在的实例，需要admin权限，否则会发送失败
                PostMessage(new IntPtr(65535), GetWndMsgShow(), 0, 0);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 在Application.OnExit中调用
        /// </summary>
        public void OnExit()
        {
            // 释放_singleInstanceMutex
            ReleaseSingleInstanceMutex();
        }

        /// <summary>
        /// 在MainWindow.Loaded事件中调用
        /// </summary>
        /// <param name="mainWindow">MainWindow对象</param>
        public void OnMainWindowLoaded(Window mainWindow)
        {
            nint mainWindowHandle = new WindowInteropHelper(mainWindow).Handle;
            // 设置本进程（即使为高级权限）的主窗口能够接收来自低级权限进程的_wndMsgShow消息
            if (!ChangeWindowMessageFilterEx(mainWindowHandle, GetWndMsgShow(), ChangeWindowMessageFilterExAction.Allow))
                throw new Win32Exception();
        }

        /// <summary>
        /// 在MainWindow消息处理函数中调用
        /// </summary>
        /// <param name="mainWindow">MainWindow对象</param>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="msg">消息</param>
        /// <param name="wParam">消息参数</param>
        /// <param name="lParam">消息参数</param>
        /// <param name="handled">此消息是否被处理</param>
        /// <returns>消息处理后的返回值</returns>
        public nint HandleWindowMessages(Window mainWindow, nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            if (msg == GetWndMsgShow())
            {
                // 收到来自重复程序实例的消息，打开窗口
                if (mainWindow.WindowState == WindowState.Minimized)
                    mainWindow.WindowState = WindowState.Normal;

                if (mainWindow.Visibility != Visibility.Visible)
                    mainWindow.Show();

                mainWindow.Activate();
                handled = true;
            }

            return 0;
        }

        public void Dispose()
        {
            ReleaseSingleInstanceMutex();
        }
        #endregion

        #region private methods
        /// <summary>
        /// 尝试确保本次启动为单实例
        /// </summary>
        /// <returns>成功返回 true，失败返回 false</returns>
        private bool CreateSingleInstanceMutex()
        {
            _singleInstanceMutex = new Mutex(true, _mutexName, out bool mutexCreated);

            if (!mutexCreated)
            {
                _singleInstanceMutex.Close();
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
            }

            return mutexCreated;
        }

        private void ReleaseSingleInstanceMutex()
        {
            if (_singleInstanceMutex != null)
            {
                _singleInstanceMutex.Close();
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
            }
        }

        /// <summary>
        /// 获取通知已经存在实例主窗口显示的消息Id
        /// </summary>
        /// <returns></returns>
        private uint GetWndMsgShow()
        {
            if (_wndMsgShow == 0)
                _wndMsgShow = RegisterWindowMessage(_wndMsgShowName);

            return _wndMsgShow;
        }
        #endregion
    }
}
