using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WPFFeatures.CustomUrl
{
    internal class CustomUrlFeature
    {
        #region fields
        /// <summary>
        /// 本程序的启动路径
        /// </summary>
        private readonly string _launcherPath;
        #endregion

        public CustomUrlFeature(string? customScheme = null, string? launcherPath = null)
        {
            Assembly asm = Assembly.GetEntryAssembly()!;
            string appName = asm.GetName().Name!;
            CustomScheme = customScheme ?? appName.ToLower();
            _launcherPath = launcherPath ?? asm.Location;
            if (_launcherPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                // 注意asm.Location在应用程序为.Net Core类型时路径会以.dll结尾
                _launcherPath = $"{_launcherPath.Substring(0, _launcherPath.Length - 4)}.exe";
            }
        }

        #region properties
        /// <summary>
        /// 自定义url协议名称
        /// </summary>
        public string CustomScheme { get; private set; }
        #endregion

        #region public methods
        /// <summary>
        /// 注册自定义Url协议到注册表
        /// </summary>
        /// <returns></returns>
        public bool TryRegisterUriScheme()
        {
            try
            {
                using (var classRoot = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default))
                using (var schemeKey = classRoot.CreateSubKey(CustomScheme))
                {
                    if (schemeKey.GetValue("URL Protocol", null) as string != string.Empty)
                        schemeKey.SetValue("URL Protocol", string.Empty);

                    string iconPath = $"\"{_launcherPath}\",0";
                    using (var defIconKey = schemeKey.CreateSubKey("DefaultIcon"))
                    {
                        if (defIconKey.GetValue(string.Empty, null) as string != iconPath)
                            defIconKey.SetValue(string.Empty, iconPath);
                    }

                    using (var cmdKey = schemeKey.CreateSubKey(@"Shell\Open\Command"))
                    {
                        string cmd = $"\"{_launcherPath}\" \"%1\"";
                        if (cmdKey.GetValue(string.Empty, null) as string != cmd)
                            cmdKey.SetValue(string.Empty, cmd);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 创建.url快捷方式
        /// </summary>
        /// <param name="filePath">文件路径，后缀名应该为.url</param>
        /// <param name="url">自定义url</param>
        public void CreateShortcut(string filePath, string url)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[InternetShortcut]");
            sb.Append($"URL={url}");

            File.WriteAllText(filePath, sb.ToString());
        }
        #endregion
    }
}
