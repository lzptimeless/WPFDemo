using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace WPFFeatures.Systray
{
    /// <summary>
    /// 添加系统托盘功能
    /// </summary>
    internal class SystrayFeature : IDisposable
    {
        #region fields
        /// <summary>
        /// 本程序名称，会在托盘菜单中显示
        /// </summary>
        private readonly string _appName;
        /// <summary>
        /// 托盘图标路径
        /// </summary>
        private readonly string _iconPath;
        /// <summary>
        /// Windows Forms托盘接口
        /// </summary>
        private NotifyIcon? _notifyIcon;
        #endregion

        /// <summary>
        /// 创建实列
        /// </summary>
        /// <param name="appName">本程序名称，默认为Assembly.GetEntryAssembly().GetName().Name</param>
        /// <param name="iconPath">托盘图标路径，默认为当前程序路径</param>
        public SystrayFeature(string? appName = null, string? iconPath = null)
        {
            Assembly asm = Assembly.GetEntryAssembly()!;
            _appName = appName ?? asm.GetName().Name!;
            if (string.IsNullOrWhiteSpace(iconPath))
            {
                if (asm.Location.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    // 注意asm.Location在应用程序为.Net Core类型时路径会以.dll结尾
                    _iconPath = $"{asm.Location.Substring(0, asm.Location.Length - 4)}.exe";
                }
                else
                    _iconPath = asm.Location;
            }
            else
                _iconPath = iconPath;
        }

        #region events
        /// <summary>
        /// 用户双击托盘，或者用户点击托盘“打开”菜单
        /// </summary>
        public event EventHandler? OpenClick;
        /// <summary>
        /// 用户点击托盘“退出”菜单
        /// </summary>
        public event EventHandler? ExitClick;
        #endregion

        #region public methods
        /// <summary>
        /// 在MainWindow.Loaded事件中调用
        /// </summary>
        public void OnMainWindowLoaded()
        {
            Icon? icon;
            string iconExtension = Path.GetExtension(_iconPath).ToLower();
            if (new string[] { ".ico", ".jpg", ".png", ".bmp" }.Contains(iconExtension))
                icon = new Icon(_iconPath);
            else
                icon = Icon.ExtractAssociatedIcon(_iconPath);

            _notifyIcon = new NotifyIcon
            {
                Icon = icon,
                Visible = true,
                ContextMenuStrip = CreateMenu()
            };
            _notifyIcon.DoubleClick += OnOpenClick;
        }

        public void Dispose()
        {
            _notifyIcon?.Dispose();
        }
        #endregion

        #region private methods
        private ContextMenuStrip CreateMenu()
        {
            var openItem = new ToolStripMenuItem($"打开{_appName}");
            openItem.Click += OnOpenClick;
            var exitItem = new ToolStripMenuItem("退出程序");
            exitItem.Click += OnExitClick;
            var contextMenu = new ContextMenuStrip { Items = { openItem, exitItem } };
            return contextMenu;
        }

        private void OnOpenClick(object? sender, EventArgs e)
        {
            OpenClick?.Invoke(this, e);
        }

        private void OnExitClick(object? sender, EventArgs e)
        {
            ExitClick?.Invoke(this, e);
        }
        #endregion
    }
}
