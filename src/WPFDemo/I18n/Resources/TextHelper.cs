using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WPFDemo.I18n.Resources
{
    /// <summary>
    /// Help to dealing text
    /// </summary>
    public static class TextHelper
    {
        #region fields
        /// <summary>
        /// 提取格式化参数的正则表达式
        /// User to extract format parameter
        /// </summary>
        private static readonly Regex ParameterRegex = new Regex(@"\{\s*(?<ParamName>\w+)\s*\}");
        #endregion

        #region public methods
        /// <summary>
        /// 对字符串格式化，即使格式化参数为null或缺少格式化参数时也不会报错，
        /// 这也是为什么不使用 <see cref="String.Format(string, object[])"/> 的原因
        /// </summary>
        /// <param name="originalString">要格式化的字符串</param>
        /// <param name="textCase">大小写类型</param>
        /// <param name="count">如果formatString是复数形式的语言则需要设置这个参数，如果是负数则会被转化为正数处理</param>
        /// <param name="parameters">格式化参数</param>
        /// <returns>格式化后的字符串</returns>
        public static string? FormatString(string? originalString, TextCases textCase = TextCases.Original, double? count = null, IEnumerable<KeyValuePair<string, string?>>? parameters = null)
        {
            if (string.IsNullOrEmpty(originalString)) return originalString;

            if (parameters == null)
            {
                string? result;
                if (count.HasValue) result = ExtractCountText(originalString, count.Value);
                else result = originalString;

                result = ApplyTextCase(result, textCase);
                return result;
            }

            StringBuilder sb = new StringBuilder();
            var parts = SplitOriginalString(originalString, textCase, count);
            foreach (var part in parts)
            {
                if (part.IsParameter)
                {
                    if (TryGetParameter(parameters, part.ParameterName, out var parameterText))
                    {
                        sb.Append(parameterText);
                    }
                    else
                    {
                        // 如果参数为null或参数缺失，就用 {x} 代替
                        sb.Append($"{{{part.ParameterName}}}");
                    }
                }
                else
                {
                    sb.Append(part.Content);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 将格式化语言分割为参数和非参数组成的片段列表
        /// </summary>
        /// <param name="originalString">要分割的语言</param>
        /// <param name="textCase">大小写类型</param>
        /// <param name="count">如果formatString是复数形式的语言则需要设置这个参数，如果是负数则会被转化为正数处理</param>
        public static List<TextPart> SplitOriginalString(string? originalString, TextCases textCase = TextCases.Original, double? count = null)
        {
            var parts = new List<TextPart>();
            if (string.IsNullOrEmpty(originalString)) return parts;

            var tmpOriginalString = originalString;
            if (count.HasValue)
                tmpOriginalString = ExtractCountText(originalString, Math.Abs(count.Value));

            if (string.IsNullOrEmpty(tmpOriginalString)) return parts;

            var mhs = ParameterRegex.Matches(tmpOriginalString); // Get format parameter
            int startIndex = 0;
            foreach (Match mh in mhs)
            {
                if (mh.Index > startIndex)
                {
                    // Dealing with text that before format parameter
                    var text = tmpOriginalString.Substring(startIndex, mh.Index - startIndex);
                    text = ApplyTextCase(text, textCase);
                    parts.Add(new TextPart { Content = text });
                }

                // Dealing with format parameter
                string parameterName = mh.Groups["ParamName"].Value;
                parts.Add(new TextPart { IsParameter = true, ParameterName = parameterName });
                startIndex = mh.Index + mh.Length;
            }// foreach

            // Dealing with last text
            if (tmpOriginalString.Length > startIndex)
            {
                if (startIndex == 0)
                {
                    tmpOriginalString = ApplyTextCase(tmpOriginalString, textCase);
                    parts.Add(new TextPart { Content = tmpOriginalString });
                }
                else
                {
                    var text = tmpOriginalString.Substring(startIndex, tmpOriginalString.Length - startIndex);
                    text = ApplyTextCase(text, textCase);
                    parts.Add(new TextPart { Content = text });
                }
            }// if

            return parts;
        }
        #endregion

        #region private methods
        /// <summary>
        /// 转换文字大小写
        /// </summary>
        /// <param name="s">原始文字</param>
        /// <param name="textCase">大小写类型</param>
        /// <returns>改变了大小写的字符串</returns>
        private static string? ApplyTextCase(string? s, TextCases textCase)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;

            if (textCase == TextCases.Original) return s;

            if (textCase == TextCases.Upper) return s.ToUpperInvariant();

            if (textCase == TextCases.Lower) return s.ToLowerInvariant();

            if (textCase == TextCases.Title) return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s);

            return s;
        }

        /// <summary>
        /// 从以|符号分隔的复数字符串中提取count所表示的字符串
        /// </summary>
        /// <param name="value">要格式化的字符串</param>
        /// <param name="count">格式化时所需的数字</param>
        /// <returns></returns>
        private static string ExtractCountText(string value, double count)
        {
            string[] subValues = value.Split('|');
            string finalValue;

            if (count <= 0) finalValue = subValues[0].Trim();
            else if (count == 1)
            {
                if (subValues.Length >= 3) finalValue = subValues[1].Trim();
                else finalValue = subValues[0].Trim();
            }
            else
            {
                if (subValues.Length >= 3) finalValue = subValues[2].Trim();
                else if (subValues.Length >= 2) finalValue = subValues[1].Trim();
                else finalValue = subValues[0].Trim();
            }

            return finalValue;
        }

        private static bool TryGetParameter(IEnumerable<KeyValuePair<string, string?>>? parameters, string? paramName, out string? value)
        {
            value = null;
            if (parameters == null || string.IsNullOrEmpty(paramName)) return false;

            foreach (var item in parameters)
            {
                if (string.Equals(item.Key, paramName, StringComparison.OrdinalIgnoreCase))
                {
                    value = item.Value;
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
