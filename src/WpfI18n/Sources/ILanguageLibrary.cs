using System;
using System.IO;

namespace WpfI18n.Sources
{
    /// <summary>
    /// 多语言接口，加载多语言文件，获取多语言
    /// </summary>
    public interface ILanguageLibrary: ILanguageProvider
    {
        /// <summary>
        /// 注册语言键值数据，可多次调用，如果数据中有多语言键与已经加载的多语言重复，则会替换掉之前的数据
        /// </summary>
        /// <param name="stream">包含多语言数据的文件流</param>
        void Register(Stream stream);
        /// <summary>
        /// 注册语言键值数据，可多次调用，如果数据中有多语言键与已经加载的多语言重复，则会替换掉之前的数据
        /// </summary>
        /// <param name="path">多语言数据文件路径</param>
        void Register(string path);
    }
}
