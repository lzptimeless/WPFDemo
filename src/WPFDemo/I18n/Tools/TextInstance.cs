using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using WPFDemo.I18n.Resources;
using static System.Net.Mime.MediaTypeNames;

namespace WPFDemo.I18n.Tools
{
    internal class TextInstance
    {
        #region fields
        private readonly DependencyObject _element;
        #endregion

        public TextInstance(DependencyObject element)
        {
            _element = element;
            BindEvents();
        }

        #region properties
        /// <summary>
        /// 当前多语言为无效状态，会在下一个UI Dispatch任务里面自动刷新
        /// </summary>
        public bool IsInvalid { get; private set; }
        #endregion

        #region public methods
        /// <summary>
        /// 将多语言设置为无效状态，让其在下一个UI Dispatch任务里面自动刷新
        /// </summary>
        public void Invalid()
        {

        }

        /// <summary>
        /// 刷新多语言
        /// </summary>
        public void Refresh()
        {

        }
        #endregion

        #region private methods
        private void OnLoaded(object sender, RoutedEventArgs e)
        {

        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {

        }

        private void BindEvents()
        {
            var element = _element;
            if (element is FrameworkElement)
            {
                var fe = (FrameworkElement)element;
                fe.Loaded += OnLoaded;
                fe.Unloaded += OnUnloaded;
            }
            else if (element is FrameworkContentElement)
            {
                var fce = (FrameworkContentElement)element;
                fce.Loaded += OnLoaded;
                fce.Unloaded += OnUnloaded;
            }
        }

        private string? GetText()
        {
            var element = _element;
            if (element is TextBlock)
                return ((TextBlock)element).Text;
            else if (element is Run)
                return ((Run)element).Text;
            else if (element is HeaderedContentControl)
                return ((HeaderedContentControl)element).Header as string;
            else if (element is ContentControl)
                return ((ContentControl)element).Content as string;

            return null;
        }

        private void SetText(string? text)
        {
            var element = _element;
            if (element is TextBlock)
                ((TextBlock)element).Text = text;
            else if (element is Run)
                ((Run)element).Text = text;
            else if (element is HeaderedContentControl)
                ((HeaderedContentControl)element).Header = text;
            else if (element is ContentControl)
                ((ContentControl)element).Content = text;
        }

        private void SetTextParts(List<TextPart> parts)
        {
            var element = _element as TextBlock;
            if (element == null) return;

            var inlines = element.Inlines;
            
        }
        #endregion
    }
}
