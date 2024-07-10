using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfI18n.Events
{
    /// <summary>
    /// 语言改变事件参数
    /// </summary>
    public class LanguageChangedArgs : EventArgs
    {
        /// <summary>
        /// 初始化函数
        /// </summary>
        /// <param name="ietfTag">当前的语言Tag</param>
        public LanguageChangedArgs(string ietfTag)
        {
            CurrentIetfTag = ietfTag;
        }

        public string CurrentIetfTag { get; set; }
    }
}
