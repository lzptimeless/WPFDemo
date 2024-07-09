using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFDemo.I18n.Resources
{
    public interface ILanguageProvider
    {
        /// <summary>
        /// 通过多语言键获取格式化后的多语言
        /// </summary>
        /// <param name="key">要查询的多语言的键，不可以为空，否则抛出异常</param>
        /// <param name="textCase">大小写转换类型，将多语言值按照指定类型进行大小写转换，默认是 <see cref="TextCases.Original"/></param>
        /// <param name="count">多语言对应的复数值，负数会被自动转化为正数，不需要则设置为null</param>
        /// <param name="formatArgs">如果多语言的值包含替换符，则可以传入格式化参数</param>
        /// <returns>如果 key 存在则返回 key 对应的值，如果 key 不存在则返回 null</returns>
        string? GetText(string key, TextCases textCase = TextCases.Original, double? count = null, IEnumerable<KeyValuePair<string, string?>>? formatArgs = null);

        /// <summary>
        /// 通过多语言键获取多语言，结果被拆分为<see cref="TextPart"/>列表
        /// </summary>
        /// <param name="key">要查询的多语言的键，不可以为空，否则抛出异常</param>
        /// <param name="textCase">大小写转换类型，将多语言值按照指定类型进行大小写转换，默认是 <see cref="TextCases.Original"/></param>
        /// <param name="count">多语言对应的复数值，负数会被自动转化为正数，不需要则设置为null</param>
        /// <returns>如果 key 存在则返回 key 对应的值，如果 key 不存在则返回null</returns>
        List<TextPart>? GetTextParts(string key, TextCases textCase = TextCases.Original, double? count = null);
    }
}
