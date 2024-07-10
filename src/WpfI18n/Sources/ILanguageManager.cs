using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfI18n.Events;

namespace WpfI18n.Sources
{
    /// <summary>
    /// 管理多语言文件夹，便捷加载多语言
    /// </summary>
    public interface ILanguageManager : ILanguageProvider
    {
        /// <summary>
        /// 当前多语言的 Ietf tag
        /// </summary>
        string? IetfTag { get; }

        /// <summary>
        /// 多语言发生改变时触发，即<see cref="Load"/>函数调用之后触发
        /// </summary>
        event EventHandler<LanguageChangedArgs>? LanguageChanged;

        /// <summary>
        /// 注册存放多语言的文件夹，可多次调用，注册多个文件夹
        /// </summary>
        /// <param name="dir">要注册的多语言文件夹</param>
        void Register(string dir);
        /// <summary>
        /// 清空当前的多语言，并重新加载指定的多语言，同时更新 <see cref="IetfTag"/> 属性
        /// </summary>
        /// <param name="langIetfTag">要加载的多语言的 Ietf tag</param>
        /// <param name="defaultTag">默认多语言 Ietf tag，如果没有找到 langIetfTag 指定的多语言，则使用 defaultTag 指定的多语言</param>
        void Load(string langIetfTag, string? defaultTag = null);
    }
}
