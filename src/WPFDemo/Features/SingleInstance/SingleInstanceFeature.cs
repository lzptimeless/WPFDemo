using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using WPFDemo;

namespace WPFFeatures.SingleInstance
{
    /// <summary>
    /// 单实例功能
    /// </summary>
    internal class SingleInstanceFeature : IDisposable
    {
        #region win32
        /// <summary>
        /// 将消息发送到系统中的所有顶级窗口，包括已禁用或不可见的未拥有窗口
        /// </summary>
        private const uint HWND_BROADCAST = 0xffff;
        /// <summary>
        /// 本次显示窗口的消息不携带任何参数
        /// </summary>
        private const uint MSG_SHOW_LPARAM_EMPTY = 0;
        /// <summary>
        /// 本次显示窗口的消息需要从剪切板获取额外的参数
        /// </summary>
        private const uint MSG_SHOW_LPARAM_CLIPBOARD = 1;
        /// <summary>
        /// 本次显示窗口的消息需要从临时文件获取额外的参数
        /// </summary>
        private const uint MSG_SHOW_LPARAM_TMP_FILE = 2;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(nint hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint RegisterWindowMessage(string lpProcName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ChangeWindowMessageFilterEx(nint hwnd, uint message, ChangeWindowMessageFilterExAction action, nint pChangeFilterStruct = 0);

        public enum ChangeWindowMessageFilterExAction : uint
        {
            Reset = 0, Allow = 1, DisAllow = 2
        };

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint RegisterClipboardFormat(string lpszFormat);
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
        private readonly string _msgShowName;
        /// <summary>
        /// 通知已存在的实列打开主窗口的消息Id
        /// </summary>
        private uint _msgShow;
        /// <summary>
        /// 用以进程间传输数据的剪切板数据格式名
        /// </summary>
        private readonly string _msgClipboardFormatName;
        /// <summary>
        /// 用以进程间传输数据的临时文件名
        /// </summary>
        private readonly string _msgTempFileName;
        #endregion
        /// <summary>
        /// 单实例功能
        /// </summary>
        /// <param name="mutexName">用以保证单实例的Mutex的名称，默认为{Assembly.FullName}Mutex</param>
        /// <param name="wndMsgShowName">通知已存在的实列打开主窗口的消息名称，默认为{Assembly.FullName}Show</param>
        public SingleInstanceFeature(string? mutexName = null, string? wndMsgShowName = null)
        {
            var appName = Assembly.GetEntryAssembly()!.GetName().Name!;
            _mutexName = mutexName ?? $"{appName}Mutex";
            _msgShowName = wndMsgShowName ?? $"{appName}Show";
            _msgClipboardFormatName = $"{appName}Message";
            _msgTempFileName = $"{appName}Message.txt";
        }

        #region events
        /// <summary>
        /// 收到来自重复实例显示窗口的消息，或收到其它程序发送的显示窗口的消息
        /// </summary>
        public event EventHandler<ShowWindowArgs>? ShowWindow;
        #endregion

        #region public methods
        /// <summary>
        /// 在Application.OnStartup中调用
        /// </summary>
        /// <param name="e">程序启动参数</param>
        /// <returns>true：当前实例为单实例，false：已经存在一个实例，本实例需要退出</returns>
        public bool OnStartup(StartupEventArgs e)
        {
            if (!CreateSingleInstanceMutex())
            {
                // 本次启动属于重复启动，直接退出
                // 通知已经存在的实例打开窗口，注意这个操作需要发送窗口消息给已经存在的实例
                uint lParam = MSG_SHOW_LPARAM_EMPTY;
                if (e.Args.Length > 0)
                {
                    // Try to set process startup args
                    string args = string.Join(' ', e.Args);
                    try
                    {
                        Clipboard.SetData(_msgClipboardFormatName, args);
                        if ((string)Clipboard.GetData(_msgClipboardFormatName) == args)
                            lParam = MSG_SHOW_LPARAM_CLIPBOARD;
                    }
                    catch { }

                    if (lParam == MSG_SHOW_LPARAM_EMPTY)
                    {
                        // Set clipboard failed, use tmp file mode
                        try
                        {
                            string filePath = Path.Combine(Path.GetTempPath(), _msgTempFileName);
                            File.WriteAllText(filePath, args);
                            lParam = MSG_SHOW_LPARAM_TMP_FILE;
                        }
                        catch { }
                    }
                }

                PostMessage(new IntPtr(HWND_BROADCAST), GetMsgShow(), IntPtr.Zero, new IntPtr(lParam));

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
        /// <param name="hwnd">MainWindow handle</param>
        public void OnMainWindowLoaded(IntPtr hwnd)
        {
            // 设置本进程（高级权限）的主窗口能够接收来自低级权限进程的消息
            if (!ChangeWindowMessageFilterEx(hwnd, GetMsgShow(), ChangeWindowMessageFilterExAction.Allow))
                throw new Win32Exception();

            // 注册自用的剪切板数据类型
            if (RegisterClipboardFormat(_msgClipboardFormatName) == 0)
            {
                // Failed, do nothing
            }
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
            if (msg == GetMsgShow())
            {
                string? args = null;
                try
                {
                    if (lParam == MSG_SHOW_LPARAM_CLIPBOARD)
                    {
                        args = Clipboard.GetData(_msgClipboardFormatName) as string;
                        Clipboard.SetData(_msgClipboardFormatName, string.Empty);
                    }
                    else if (lParam == MSG_SHOW_LPARAM_TMP_FILE)
                    {
                        string filePath = Path.Combine(Path.GetTempPath(), _msgTempFileName);
                        args = File.ReadAllText(filePath);
                        File.Delete(filePath);
                    }
                }
                catch { }

                ShowWindow?.Invoke(this, new ShowWindowArgs(args));
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
        private uint GetMsgShow()
        {
            if (_msgShow == 0)
                _msgShow = RegisterWindowMessage(_msgShowName);

            return _msgShow;
        }
        #endregion
    }

    internal class ShowWindowArgs : EventArgs
    {
        public ShowWindowArgs(string? args)
        {
            Args = args;
        }

        public string? Args { get; }
    }
}
