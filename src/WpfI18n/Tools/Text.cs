using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using WpfI18n.Sources;

namespace WpfI18n.Tools
{
    public static class Text
    {
        #region Key
        public static string? GetKey(DependencyObject obj)
        {
            return obj.GetValue(KeyProperty) as string;
        }

        public static void SetKey(DependencyObject obj, string? value)
        {
            obj.SetValue(KeyProperty, value);
        }

        /// <summary>
        /// 对应语言文件(.xml)中的key属性
        /// </summary>
        public static readonly DependencyProperty KeyProperty =
            DependencyProperty.RegisterAttached("Key", typeof(string), typeof(Text), new PropertyMetadata(null, KeyChanged));

        private static void KeyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is TextBlock
                || obj is Run
                || obj is ContentControl
                || obj is HeaderedContentControl
                || obj is Span
                || obj is Paragraph))
                throw new NotSupportedException("Text.Key only supports TextBlock, Run, ContentControl, HeaderedContentControl, Span, and Paragraph.");

            var textInstance = GetTextInstance(obj);
            if (textInstance == null)
            {
                textInstance = new TextInstance(obj);
                SetTextInstance(obj, textInstance);
            }

            textInstance.Invalid();
        }
        #endregion

        #region Case
        public static TextCases GetCase(DependencyObject obj)
        {
            return (TextCases)obj.GetValue(CaseProperty);
        }

        public static void SetCase(DependencyObject obj, TextCases value)
        {
            obj.SetValue(CaseProperty, value);
        }

        /// <summary>
        /// 转换语言文本大小写，默认为不转换(Original)
        /// </summary>
        public static readonly DependencyProperty CaseProperty =
            DependencyProperty.RegisterAttached("Case", typeof(TextCases), typeof(Text), new FrameworkPropertyMetadata(TextCases.Original, FrameworkPropertyMetadataOptions.Inherits, CaseChanged));

        private static void CaseChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            GetTextInstance(obj)?.Invalid();
        }
        #endregion

        #region Count
        public static double? GetCount(DependencyObject obj)
        {
            return (double?)obj.GetValue(CountProperty);
        }

        public static void SetCount(DependencyObject obj, double? value)
        {
            obj.SetValue(CountProperty, value);
        }

        /// <summary>
        /// 用于设置复数格式化模式下复数的值，以便决定使用哪一种复数文字
        /// </summary>
        public static readonly DependencyProperty CountProperty =
            DependencyProperty.RegisterAttached("Count", typeof(double?), typeof(Text), new PropertyMetadata(null, CountChanged));

        private static void CountChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            GetTextInstance(obj)?.Invalid();
        }
        #endregion

        #region ParamName
        public static string? GetParamName(DependencyObject obj)
        {
            return obj.GetValue(ParamNameProperty) as string;
        }

        public static void SetParamName(DependencyObject obj, string? value)
        {
            obj.SetValue(ParamNameProperty, value);
        }

        /// <summary>
        /// 语言格式化参数的参数名，用以将某个UI元素标记为语言格式化参数，例如：将Run元素标记为TextBlock的语言格式化参数
        /// </summary>
        public static readonly DependencyProperty ParamNameProperty =
            DependencyProperty.RegisterAttached("ParamName", typeof(string), typeof(Text), new PropertyMetadata(null, ParamNameChanged));

        private static void ParamNameChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is Inline))
                throw new NotSupportedException("Text.ParamName only supports Inline.");
        }
        #endregion

        #region TextInstance
        internal static TextInstance? GetTextInstance(DependencyObject obj)
        {
            return (TextInstance)obj.GetValue(TextInstanceProperty);
        }

        internal static void SetTextInstance(DependencyObject obj, TextInstance? value)
        {
            obj.SetValue(TextInstancePropertyKey, value);
        }

        private static readonly DependencyPropertyKey TextInstancePropertyKey =
            DependencyProperty.RegisterAttachedReadOnly("TextInstance", typeof(TextInstance), typeof(Text), new PropertyMetadata(null, TextInstancePropertyChanged));

        /// <summary>
        /// 语言格式化管理对象，负责在合适的时候获取语言格式化结果并设置到其所附加的UI元素
        /// </summary>
        internal static readonly DependencyProperty TextInstanceProperty = TextInstancePropertyKey.DependencyProperty;

        private static void TextInstancePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }
        #endregion
    }
}
