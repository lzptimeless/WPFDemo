using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WPFDemo.I18n.Resources;

namespace WPFDemo.I18n.Tools
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
        }
        #endregion

        #region Count
        public static double GetCount(DependencyObject obj)
        {
            return (double)obj.GetValue(CountProperty);
        }

        public static void SetCount(DependencyObject obj, double value)
        {
            obj.SetValue(CountProperty, value);
        }

        /// <summary>
        /// 用于设置复数格式化模式下复数的值，以便决定使用哪一种复数文字
        /// </summary>
        public static readonly DependencyProperty CountProperty =
            DependencyProperty.RegisterAttached("Count", typeof(double), typeof(Text), new PropertyMetadata(0d, CountChanged));

        private static void CountChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
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
        }
        #endregion

        #region I18nGenerated
        public static bool GetI18nGenerated(DependencyObject obj)
        {
            return (bool)obj.GetValue(I18nGeneratedProperty);
        }

        public static void SetI18nGenerated(DependencyObject obj, bool value)
        {
            obj.SetValue(I18nGeneratedProperty, value);
        }

        /// <summary>
        /// 标记某个UI元素是否是I18n自动生成的
        /// </summary>
        public static readonly DependencyProperty I18nGeneratedProperty =
            DependencyProperty.RegisterAttached("I18nGenerated", typeof(bool), typeof(Text), new PropertyMetadata(false, I18nGeneratedChanged));

        private static void I18nGeneratedChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
        }
        #endregion
    }
}
