using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfI18n.Exceptions
{
    /// <summary>
    /// 语言文件内容格式错误异常
    /// </summary>
    public class LanguageFileFormatException : Exception
    {
        /// <summary>
        /// 初始化函数
        /// </summary>
        /// <param name="msg">异常提示信息</param>
        public LanguageFileFormatException(string msg)
            : base(msg)
        { }
    }
}
