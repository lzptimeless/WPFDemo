using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFDemo.I18n.Resources
{
    /// <summary>
    /// 语言文本格式化结果的一部分
    /// </summary>
    public struct TextPart
    {
        /// <summary>
        /// 这部分语言是否是参数
        /// </summary>
        public bool IsParameter { get; set; }
        /// <summary>
        /// 如果<see cref="IsParameter"/>==true，则代表参数名，否则为null
        /// </summary>
        public string? ParameterName { get; set; }
        /// <summary>
        /// 这部分语言的内容
        /// </summary>
        public string? Content { get; set; }

        public override string ToString()
        {
            if (IsParameter)
                return $"{{{ParameterName}}}";
            else
                return Content ?? string.Empty;
        }
    }
}
