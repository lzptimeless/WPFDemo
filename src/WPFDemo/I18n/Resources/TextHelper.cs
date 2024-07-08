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
        /// <param name="value">要格式化的字符串</param>
        /// <param name="count">格式化时所需的数字</param>
        /// <param name="parameters">格式化参数</param>
        /// <returns>格式化后的字符串</returns>
        public static string FormatCountString(string value, double count, params object[] parameters)
        {
            if (string.IsNullOrEmpty(value)) return value;

            string finalValue = ExtractCountText(value, count);
            var finalParameters = parameters;
            if (finalParameters == null)
            {
                finalParameters = new object[] { count };
            }

            return FormatString(finalValue, finalParameters);
        }

        /// <summary>
        /// 对字符串格式化，即使格式化参数为null或缺少格式化参数时也不会报错，
        /// 这也是为什么不使用 <see cref="String.Format(string, object[])"/> 的原因
        /// </summary>
        /// <param name="value">要格式化的字符串</param>
        /// <param name="parameters">格式化参数</param>
        /// <returns>格式化后的字符串</returns>
        public static string FormatString(string value, params object[] parameters)
        {
            if (string.IsNullOrEmpty(value)) return value;

            StringBuilder sb = new StringBuilder();
            AnalyzeFormatString(value, (isParam, paramName, intParamName, text) =>
            {
                if (isParam)
                {
                    if (intParamName >= 0 && parameters.Length > intParamName && parameters[intParamName] != null)
                    {
                        object parameter = parameters[intParamName];
                        string? paramText = (parameter is string) ? parameter as string : parameter.ToString();
                        sb.Append(paramText);
                    }
                    else
                    {
                        // 如果参数为null或参数缺失，就用 {x} 代替
                        string nullText = string.Format("{{{0}}}", paramName);
                        sb.Append(nullText);
                    }
                }
                else
                {
                    sb.Append(text);
                }
            });

            return sb.ToString();
        }

        /// <summary>
        /// 对支持复数的字符串格式化，即使格式化参数为null或缺少格式化参数时也不会报错，
        /// 这也是为什么不使用 <see cref="String.Format(string, object[])"/> 的原因
        /// </summary>
        /// <param name="value">要格式化的字符串</param>
        /// <param name="count">格式化时所需的数字</param>
        /// <param name="parameters">格式化参数</param>
        /// <returns>格式化后的字符串</returns>
        public static string FormatCountString(string value, double count, Dictionary<string, object>? parameters)
        {
            if (string.IsNullOrEmpty(value)) return value;

            string finalValue = ExtractCountText(value, count);
            return FormatString(finalValue, parameters);
        }

        /// <summary>
        /// 对字符串格式化，即使格式化参数为null或缺少格式化参数时也不会报错，
        /// 这也是为什么不使用 <see cref="String.Format(string, object[])"/> 的原因
        /// </summary>
        /// <param name="value">要格式化的字符串</param>
        /// <param name="parameters">格式化参数</param>
        /// <returns>格式化后的字符串</returns>
        public static string FormatString(string value, Dictionary<string, object>? parameters)
        {
            if (string.IsNullOrEmpty(value)) return value;

            StringBuilder sb = new StringBuilder();
            AnalyzeFormatString(value, (isParam, paramName, intParamName, text) =>
            {
                if (isParam)
                {
                    if (parameters != null && !string.IsNullOrEmpty(paramName) && parameters.ContainsKey(paramName) && parameters[paramName] != null)
                    {
                        object parameter = parameters[paramName];
                        string? paramText = (parameter is string) ? parameter as string : parameter.ToString();
                        sb.Append(paramText);
                    }
                    else
                    {
                        // 如果参数为null或参数缺失，就用 {x} 代替
                        string nullText = string.Format("{{{0}}}", paramName);
                        sb.Append(nullText);
                    }
                }
                else
                {
                    sb.Append(text);
                }
            });

            return sb.ToString();
        }

        /// <summary>
        /// 对支持复数的字符串格式化，即使格式化参数为null或缺少格式化参数时也不会报错，
        /// 这也是为什么不使用 <see cref="String.Format(string, object[])"/> 的原因
        /// </summary>
        /// <param name="value">要格式化的字符串</param>
        /// <param name="count">格式化时所需的数字</param>
        /// <param name="parameters">格式化参数</param>
        /// <returns>格式化后的结果</returns>
        public static List<TextPart> FormatCountStringToParts(string value, double count, Dictionary<string, object>? parameters)
        {
            if (string.IsNullOrEmpty(value)) return new List<TextPart>();

            string finalValue = ExtractCountText(value, count);
            return FormatStringToParts(finalValue, parameters);
        }

        /// <summary>
        /// 对字符串格式化，即使格式化参数为null或缺少格式化参数时也不会报错，
        /// 这也是为什么不使用 <see cref="String.Format(string, object[])"/> 的原因
        /// </summary>
        /// <param name="value">要格式化的字符串</param>
        /// <param name="parameters">格式化参数</param>
        /// <returns>格式化后的结果</returns>
        public static List<TextPart> FormatStringToParts(string value, Dictionary<string, object>? parameters)
        {
            var parts = new List<TextPart>();

            if (string.IsNullOrEmpty(value)) return parts;

            AnalyzeFormatString(value, (isParam, paramName, intParamName, text) =>
            {
                if (isParam)
                {
                    if (parameters != null && !string.IsNullOrEmpty(paramName) && parameters.ContainsKey(paramName) && parameters[paramName] != null)
                    {
                        object parameter = parameters[paramName];
                        string? paramText = (parameter is string) ? parameter as string : parameter.ToString();
                        parts.Add(new TextPart
                        {
                            IsParameter = true,
                            ParameterName = paramName,
                            Content = paramText
                        });
                    }
                    else
                    {
                        // 如果参数为null或参数缺失，就用 {x} 代替
                        string nullText = string.Format("{{{0}}}", paramName);
                        parts.Add(new TextPart
                        {
                            IsParameter = true,
                            ParameterName = paramName,
                            Content = nullText
                        });
                    }
                }
                else
                {
                    parts.Add(new TextPart
                    {
                        IsParameter = false,
                        ParameterName = null,
                        Content = text
                    });
                }
            });

            return parts;
        }

        /// <summary>
        /// 从以|符号分隔的复数字符串中提取count所表示的字符串
        /// </summary>
        /// <param name="value">要格式化的字符串</param>
        /// <param name="count">格式化时所需的数字</param>
        /// <returns></returns>
        public static string ExtractCountText(string value, double count)
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

        /// <summary>
        /// 分析包含替换符的字符串，用于辅助格式化字符串
        /// Analyze format string
        /// </summary>
        /// <param name="formatString">要分析的字符串</param>
        public static void AnalyzeFormatString(string formatString, AnalyzeFormatStringAction action)
        {
            if (string.IsNullOrEmpty(formatString)) return;

            var mhs = ParameterRegex.Matches(formatString); // Get format parameter
            int startIndex = 0;
            foreach (Match mh in mhs)
            {
                if (mh.Index > startIndex)
                {
                    // Dealing with text that before format parameter
                    string text = formatString.Substring(startIndex, mh.Index - startIndex);
                    action.Invoke(false, null, -1, text);
                }

                // Dealing with format parameter
                string parameterName = mh.Groups["ParamName"].Value;
                if (!int.TryParse(parameterName, out int intParameterName))
                {
                    intParameterName = -1;
                }

                action.Invoke(true, parameterName, intParameterName, null);
                startIndex = mh.Index + mh.Length;
            }// foreach

            // Dealing with last text
            if (formatString.Length > startIndex)
            {
                if (startIndex == 0)
                {
                    action.Invoke(false, null, -1, formatString);
                }
                else
                {
                    string text = formatString.Substring(startIndex, formatString.Length - startIndex);
                    action.Invoke(false, null, -1, text);
                }
            }// if
        }

        /// <summary>
        /// Convert text with specified case
        /// </summary>
        /// <param name="s">Original text</param>
        /// <param name="textCase">Case</param>
        /// <returns>改变了大小写的字符串</returns>
        public static string ApplyTextCase(string s, TextCases textCase)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;

            if (textCase == TextCases.Original) return s;

            if (textCase == TextCases.Upper) return s.ToUpperInvariant();

            if (textCase == TextCases.Lower) return s.ToLowerInvariant();

            if (textCase == TextCases.Title) return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s);

            return s;
        }
        #endregion
    }

    /// <summary>
    /// Analyze format string
    /// </summary>
    /// <param name="isParameter">Whether current part is format parameter</param>
    /// <param name="parameterName">代表替换符，即{}里面的内容，isParameter为true时这个值才有效</param>
    /// <param name="intParameterName">是参数parameterName尝试转化为int的结果，如果转化失败则为-1</param>
    /// <param name="text">If current part is not format parameter，this argument indicate text，otherwise ignore it</param>
    public delegate void AnalyzeFormatStringAction(bool isParameter, string? parameterName, int intParameterName, string? text);
}
