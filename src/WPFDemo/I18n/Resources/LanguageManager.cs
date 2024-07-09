using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WPFDemo.I18n.Resources
{
    public class LanguageManager : ILanguageManager
    {
        #region fields
        /// <summary>
        /// 抽象 IO，用于单元测试
        /// </summary>
        private System.IO.Abstractions.IFileSystem _fileSys;
        /// <summary>
        /// 实际存储多语言
        /// </summary>
        private ILanguageLibrary _langLib = new LanguageLibrary();
        /// <summary>
        /// 多语言文件夹列表
        /// </summary>
        private List<string> _dirs = new List<string>();
        /// <summary>
        /// 默认语言管理实例
        /// </summary>
        private static LanguageManager? _default;
        #endregion

        public LanguageManager()
            : this(new System.IO.Abstractions.FileSystem())
        { }

        public LanguageManager(System.IO.Abstractions.IFileSystem fileSys)
        {
            _fileSys = fileSys;
        }

        #region properties
        /// <summary>
        /// 当前语言的 Ietf tag
        /// </summary>
        public string? IetfTag { get; private set; }
        /// <summary>
        /// 已经注册的语言文件夹
        /// </summary>
        public IReadOnlyList<string> Directories { get { return _dirs; } }
        /// <summary>
        /// 默认语言管理实例
        /// </summary>
        public static LanguageManager Default
        {
            get
            {
                if (_default == null)
                {
                    var instance = new LanguageManager();
                    Interlocked.CompareExchange(ref _default, instance, null);
                }

                return _default;
            }
        }
        #endregion

        #region events
        public event EventHandler<LanguageChangedArgs>? LanguageChanged;
        #endregion

        #region public methods
        public string? GetText(string key, TextCases textCase = TextCases.Original, double? count = null, IEnumerable<KeyValuePair<string, string?>>? formatArgs = null)
        {
            return _langLib.GetText(key, textCase, count, formatArgs);
        }

        public List<TextPart>? GetTextParts(string key, TextCases textCase = TextCases.Original, double? count = null)
        {
            return _langLib.GetTextParts(key, textCase, count);
        }

        public void Load(string langIetfTag, string? defaultTag = null)
        {
            if (string.IsNullOrWhiteSpace(langIetfTag))
                throw new ArgumentException("langIetfTag can not be null or empty", "langIetfTag");

            List<string> langFiles = GetFilesByTag(langIetfTag, defaultTag);
            LanguageLibrary langLibrary = new LanguageLibrary(_fileSys);
            foreach (var langFile in langFiles)
            {
                langLibrary.Register(langFile);
            }

            IetfTag = langIetfTag;
            _langLib = langLibrary;

            LanguageChanged?.Invoke(this, new LanguageChangedArgs(langIetfTag));
        }

        public void Register(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir))
                throw new ArgumentException("dir can not be null or empty.", "dir");

            var dir2 = dir.Replace('/', '\\').Trim();
            if (!dir2.EndsWith(@":\"))
            {
                dir2 = dir2.TrimEnd('\\');
            }

            string fullPath = Path.GetFullPath(dir2).ToLowerInvariant();

            if (!_dirs.Contains(fullPath))
                _dirs.Add(fullPath);
        }
        #endregion

        #region private methods
        /// <summary>
        /// 获取指定 Ietf tag 的需要加载的所有文件
        /// </summary>
        /// <param name="tag">需要加载的 Ietf tag</param>
        /// <returns>文件列表</returns>
        private List<string> GetFilesByTag(string tag, string? defaultTag)
        {
            tag = tag.Trim().ToLowerInvariant();
            defaultTag = (defaultTag ?? string.Empty).ToLowerInvariant();

            List<string> tagFiles = new List<string>();
            List<string> allDirs = new List<string>();
            foreach (var dir in _dirs)
            {
                if (_fileSys.Directory.Exists(dir) && !allDirs.Contains(dir))
                {
                    allDirs.Add(dir);

                    var subDirs = _fileSys.Directory.GetDirectories(dir, "*", SearchOption.AllDirectories);
                    foreach (var subDir in subDirs)
                    {
                        var lowerSubDir = subDir.ToLowerInvariant();
                        if (!allDirs.Contains(lowerSubDir))
                        {
                            allDirs.Add(lowerSubDir);
                        }
                    }
                }
            }

            foreach (var dir in allDirs)
            {
                var groups = GetFilesWithGroup(dir);
                foreach (var g in groups)
                {
                    var tagFile = GetOneFileByTag(g.Value, tag);
                    if (!string.IsNullOrEmpty(tagFile))
                        tagFiles.Add(tagFile);
                    else if (!string.IsNullOrEmpty(defaultTag))
                    {
                        tagFile = GetOneFileByTag(g.Value, defaultTag);
                        if (!string.IsNullOrEmpty(tagFile))
                            tagFiles.Add(tagFile);
                    }
                }
            }

            return tagFiles;
        }

        /// <summary>
        /// 将同一个文件夹下（不包括子文件夹）的多语言文件按文件名格式 {YourCustomName}.{IetfTag}.xml 中的 
        /// {YourCustomName} 进行分组
        /// </summary>
        /// <param name="dir">多语言文件夹</param>
        /// <returns>key: 组名，value: 同一个组的多语言文件</returns>
        private Dictionary<string, List<string>> GetFilesWithGroup(string dir)
        {
            string[] allFiles = _fileSys.Directory.GetFiles(dir, "*.xml", SearchOption.TopDirectoryOnly).Select(f => f.ToLowerInvariant()).ToArray();
            Dictionary<string, List<string>> groupsTmp = new Dictionary<string, List<string>>();
            Regex groupRegex = new Regex(@"^((?<group>.*)\.)?.+\.xml$");
            string emptyGroupName = Guid.NewGuid().ToString();
            foreach (var file in allFiles)
            {
                string fileName = Path.GetFileName(file);
                var mh = groupRegex.Match(fileName);
                if (!mh.Success)
                    continue;

                string groupName = emptyGroupName;
                var group = mh.Groups["group"];
                if (group != null && group.Length > 0)
                {
                    groupName = group.Value;
                }

                if (!groupsTmp.ContainsKey(groupName))
                    groupsTmp.Add(groupName, new List<string>());

                groupsTmp[groupName].Add(file);
            }

            return groupsTmp;
        }

        /// <summary>
        /// 只获取一个 Ietf tag 匹配度最高的文件
        /// </summary>
        /// <param name="files">可用的文件列表</param>
        /// <param name="tag">要获取文件的 Ietf tag</param>
        /// <returns>获取成功则返回文件路径，失败返回 null</returns>
        private string? GetOneFileByTag(IEnumerable<string> files, string tag)
        {
            var sortedFiles = new List<KeyValuePair<string, int>>();
            while (true)
            {
                sortedFiles.Clear();

                var regex = new Regex($@"^(.*[.\\/])?{tag}(?<sub>-.*)?\.xml$");
                foreach (var file in files)
                {
                    var mh = regex.Match(file);
                    if (mh.Success)
                    {
                        var subTag = mh.Groups["sub"];
                        var subTagLen = subTag != null ? subTag.Length : 0;
                        sortedFiles.Add(new KeyValuePair<string, int>(file, subTagLen));
                    }
                }

                if (sortedFiles.Count > 1)
                {
                    sortedFiles.Sort((x, y) => x.Value.CompareTo(y.Value));
                }

                if (sortedFiles.Count > 0)
                {
                    return sortedFiles[0].Key;
                }

                if (tag.Contains('-'))
                    tag = tag.Substring(0, tag.LastIndexOf('-'));
                else
                    break;
            }

            return null;
        }
        #endregion
    }
}
