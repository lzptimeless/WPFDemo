using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFDemo.I18n.Resources
{
    /// <summary>
    /// 多语言值大小写转换类型
    /// </summary>
    public enum TextCases
    {
        /// <summary>
        /// 保持原样，不改变大小写
        /// </summary>
        Original,
        /// <summary>
        /// 全部大写
        /// </summary>
        Upper,
        /// <summary>
        /// 全部小写
        /// </summary>
        Lower,
        /// <summary>
        /// 驼峰式大小写
        /// </summary>
        Title
    }
}
