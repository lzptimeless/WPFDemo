using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WpfI18n.Sources;

namespace WpfI18n.Tools
{
    public class DesignerLanguageInitializer : IDisposable
    {
        private FileSystemWatcher? _languageFolderWatcher;
        private int _isReloading;

        public DesignerLanguageInitializer()
        {
            if (DesignerHelper.IsDesignMode)
            {
                // 使用BeginInvoke确保Initialize执行前FolderPath和Tag属性已经被设置
                Application.Current.Dispatcher.BeginInvoke(Initialize);
            }
        }

        public string? FolderPath { get; set; }
        public string? Tag { get; set; }

        public void Dispose()
        {
            if (_languageFolderWatcher != null)
            {
                if (_languageFolderWatcher.EnableRaisingEvents)
                    _languageFolderWatcher.EnableRaisingEvents = false;

                _languageFolderWatcher.Dispose();
            }
        }

        #region private methods
        private void Initialize()
        {
            if (string.IsNullOrEmpty(FolderPath) || string.IsNullOrEmpty(Tag)) return;

            try
            {
                string languageDir;
                if (Path.IsPathRooted(FolderPath))
                    languageDir = FolderPath;
                else
                    languageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FolderPath);

                if (Directory.Exists(languageDir))
                {
                    var languageManager = LanguageManager.Default;
                    languageManager.Register(languageDir);
                    languageManager.Load(Tag);

                    _languageFolderWatcher = new FileSystemWatcher(languageDir, "*.xml");
                    // 注意：NotifyFilter.CreationTime可用于监控文件被其它文件覆盖，这恰好是Visual Studio修改文件的行为
                    _languageFolderWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime;
                    _languageFolderWatcher.IncludeSubdirectories = true;
                    _languageFolderWatcher.Changed += OnLanguageFolderWatcherChanged;
                    _languageFolderWatcher.EnableRaisingEvents = true;
                }
            }
            catch { }
        }

        private void OnLanguageFolderWatcherChanged(object sender, FileSystemEventArgs e)
        {
            ReloadLanguage();
        }

        private async void ReloadLanguage()
        {
            if (Interlocked.CompareExchange(ref _isReloading, 1, 0) != 0) return;

            await Task.Delay(1000); // 等待一会儿，避免文件被占用导致语言加载失败
            await Application.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(Tag))
                        LanguageManager.Default.Load(Tag);
                }
                catch { }
                finally
                {
                    Volatile.Write(ref _isReloading, 0);
                }
            });
        }
        #endregion
    }
}
