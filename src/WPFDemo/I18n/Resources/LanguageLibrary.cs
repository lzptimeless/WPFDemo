using WPFDemo.I18n.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WPFDemo.I18n.Resources
{
    /// <summary>
    /// 多语言接口的实现类
    /// </summary>
    public class LanguageLibrary : ILanguageLibrary
    {
        #region fields
        /// <summary>
        /// 抽象 IO 接口，方便单元测试
        /// </summary>
        private System.IO.Abstractions.IFileSystem _fileSystem;
        /// <summary>
        /// 多语言数据，key:多语言键,value:多语言值
        /// </summary>
        private Dictionary<string, string> _langs = new Dictionary<string, string>();
        #endregion

        public LanguageLibrary()
            : this(new System.IO.Abstractions.FileSystem())
        { }

        public LanguageLibrary(System.IO.Abstractions.IFileSystem fileSys)
        {
            _fileSystem = fileSys;
        }

        #region methods
        public void Register(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            XDocument doc = XDocument.Load(stream);
            if (doc.Root == null)
                throw new LanguageFileFormatException("Language file lack a root node.");

            foreach (var textNode in doc.Root.Elements("text"))
            {
                var keyAttr = textNode.Attribute("key");
                if (keyAttr == null)
                    throw new LanguageFileFormatException("text node lack a key attribute.");

                if (string.IsNullOrWhiteSpace(keyAttr.Value))
                    throw new LanguageFileFormatException("The text.key can not be null or empty.");

                string key = keyAttr.Value.Trim().ToLowerInvariant();
                if (_langs.ContainsKey(key))
                    _langs[key] = textNode.Value;
                else
                    _langs.Add(key, textNode.Value);
            }
        }

        public void Register(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("path can not be null or empty.");

            using (var s = _fileSystem.File.OpenRead(path))
            {
                Register(s);
            }
        }

        public string? GetText(string key, TextCases textCase = TextCases.Original, params object[] formatArgs)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("key", "key can not be null or empty");

            key = key.Trim().ToLowerInvariant();
            if (!_langs.ContainsKey(key))
                return null;

            string result = _langs[key];
            if (formatArgs.Length != 0)
                result = TextHelper.FormatString(result, formatArgs);

            if (textCase != TextCases.Original)
                result = TextHelper.ApplyTextCase(result, textCase);

            return result;
        }

        public string? GetCountText(string key, double count, TextCases textCase = TextCases.Original, params object[] formatArgs)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("key", "key can not be null or empty");

            key = key.Trim().ToLowerInvariant();
            if (!_langs.ContainsKey(key))
                return null;

            string result = _langs[key];

            var finalArgs = formatArgs;
            if (finalArgs == null)
            {
                finalArgs = new object[] { count };
            }
            result = TextHelper.FormatCountString(result, count, finalArgs);

            if (textCase != TextCases.Original)
                result = TextHelper.ApplyTextCase(result, textCase);

            return result;
        }

        public string? GetText(string key, TextCases textCase = TextCases.Original, Dictionary<string, object>? formatArgs = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("key", "key can not be null or empty");

            key = key.Trim().ToLowerInvariant();
            if (!_langs.ContainsKey(key))
                return null;

            string result = _langs[key];
            if (formatArgs != null)
                result = TextHelper.FormatString(result, formatArgs);

            if (textCase != TextCases.Original)
                result = TextHelper.ApplyTextCase(result, textCase);

            return result;
        }

        public string? GetCountText(string key, double count, TextCases textCase = TextCases.Original, Dictionary<string, object>? formatArgs = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("key", "key can not be null or empty");

            key = key.Trim().ToLowerInvariant();
            if (!_langs.ContainsKey(key))
                return null;

            string result = _langs[key];
            result = TextHelper.FormatCountString(result, count, formatArgs);

            if (textCase != TextCases.Original)
                result = TextHelper.ApplyTextCase(result, textCase);

            return result;
        }

        public List<TextPart>? GetTextParts(string key, TextCases textCase = TextCases.Original, Dictionary<string, object>? formatArgs = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("key", "key can not be null or empty");

            key = key.Trim().ToLowerInvariant();
            if (!_langs.ContainsKey(key))
                return null;

            string orginalLang = _langs[key];
            var parts = TextHelper.FormatStringToParts(orginalLang, formatArgs);

            if (textCase != TextCases.Original)
            {
                foreach (var part in parts)
                {
                    if (!string.IsNullOrEmpty(part.Content))
                        part.Content = TextHelper.ApplyTextCase(part.Content, textCase);
                }
            }

            return parts;
        }

        public List<TextPart>? GetCountTextParts(string key, double count, TextCases textCase = TextCases.Original, Dictionary<string, object>? formatArgs = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("key", "key can not be null or empty");

            key = key.Trim().ToLowerInvariant();
            if (!_langs.ContainsKey(key))
                return null;

            string orginalLang = _langs[key];
            var parts = TextHelper.FormatCountStringToParts(orginalLang, count, formatArgs);

            if (textCase != TextCases.Original)
            {
                foreach (var part in parts)
                {
                    if (!string.IsNullOrEmpty(part.Content))
                        part.Content = TextHelper.ApplyTextCase(part.Content, textCase);
                }
            }

            return parts;
        }
        #endregion
    }
}
