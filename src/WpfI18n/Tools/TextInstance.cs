using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Shapes;
using WpfI18n.Events;
using WpfI18n.Sources;
using static System.Net.Mime.MediaTypeNames;

namespace WpfI18n.Tools
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
            var element = _element;
            if (!element.Dispatcher.CheckAccess())
                throw new InvalidOperationException("This function must be called on the UI thread.");

            if (IsInvalid) return;

            IsInvalid = true;
            element.Dispatcher.BeginInvoke(Refresh);
        }

        /// <summary>
        /// 刷新多语言
        /// </summary>
        public void Refresh()
        {
            IsInvalid = false;

            var isLoaded = GetIsElementLoaded();
            if (!isLoaded) return;

            var element = _element;
            var key = Text.GetKey(element);

            if (string.IsNullOrEmpty(key)) return;

            var textCase = Text.GetCase(element);
            var count = Text.GetCount(element);
            var hasInlineParam = HasInlineParam();

            if (hasInlineParam)
            {
                var parts = LanguageManager.Default.GetTextParts(key, textCase, count);
                SetTextParts(parts);
            }
            else
            {
                var text = LanguageManager.Default.GetText(key, textCase, count);
                SetText(text);
            }
        }
        #endregion

        #region private methods
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LanguageManager.Default.LanguageChanged += OnLanguageChanged;
            Invalid();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            LanguageManager.Default.LanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(object? sender, LanguageChangedArgs e)
        {
            Invalid();
        }

        private void BindEvents()
        {
            var element = _element;
            if (element is FrameworkElement frameworkElement)
            {
                frameworkElement.Loaded += OnLoaded;
                frameworkElement.Unloaded += OnUnloaded;
            }
            else if (element is FrameworkContentElement fcontentElement)
            {
                fcontentElement.Loaded += OnLoaded;
                fcontentElement.Unloaded += OnUnloaded;
            }
        }

        private bool GetIsElementLoaded()
        {
            var element = _element;
            if (element is FrameworkElement frameworkElement)
            {
                return frameworkElement.IsLoaded;
            }
            else if (element is FrameworkContentElement fcontentElement)
            {
                return fcontentElement.IsLoaded;
            }

            return false;
        }

        private void SetText(string? text)
        {
            var element = _element;
            if (element is TextBlock textBlock)
                textBlock.Text = text;
            else if (element is Run run)
                run.Text = text;
            else if (element is ContentControl contentControl)
                contentControl.Content = text;
            else if (element is HeaderedContentControl headerControl)
                headerControl.Header = text;
            else if (element is Span || element is Paragraph)
            {
                var inlines = GetInlineCollection();
                if (inlines != null)
                {
                    inlines.Clear();
                    if (!string.IsNullOrEmpty(text))
                    {
                        var runInline = new Run();
                        runInline.Text = text;
                        inlines.Add(runInline);
                    }
                }
            }
        }

        private void SetTextParts(List<TextPart>? parts)
        {
            var inlines = GetInlineCollection();
            if (inlines == null) return;

            var inlineParams = new TextInlineCollection();
            // 获取语言格式化参数，注意这里是允许多个相同的ParamName的Inline元素同时存在的，这种规则是为了解决以下问题：
            // 语言格式化字符串中可能存在同一个参数被多次引用的情况，而一个Inline元素是无法被多次添加到TextBlock中的。
            foreach (var inline in inlines.ToArray())
            {
                var paramName = Text.GetParamName(inline);
                if (!string.IsNullOrEmpty(paramName))
                    inlineParams.AddInline(paramName, inline);
                else
                    inlines.Remove(inline); // 移除非格式化参数的元素，这些元素可能是上次SetTextParts生成的。
            }

            if (parts == null || parts.Count == 0) return;

            // 根据语言格式化结果(parts)填充TextBlock.Inlines，注意在这个过程中尽量不要动inlines里面已经存在的元素，
            // 因为可能会影响这些元素的数据绑定导致不可预知的问题。
            for (int i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                if (inlines.Count > i)
                {
                    var inlineParam1 = (Inline)((IList)inlines)[i]!;
                    if (part.IsParameter && !string.IsNullOrEmpty(part.ParameterName))
                    {
                        var inlineParam2 = inlineParams.TakeInline(part.ParameterName);
                        if (object.ReferenceEquals(inlineParam1, inlineParam2))
                            continue;
                        else if (inlineParam2 != null)
                        {
                            inlines.Remove(inlineParam2); // 必须先从inlines中移除，否则InsertBefore会抛异常。
                            inlines.InsertBefore(inlineParam1, inlineParam2);
                            if (inlineParam2 is Run runParam)
                            {
                                // 如果Run元素的Text属性的绑定的数据源是DataContext，在经过Remove+InsertBefore
                                // 之后，即使Text属性不为空也不会被父级TextBlock渲染，需要通过SetCurrentValue+UpdateTarget
                                // 修复。
                                var textBinding = runParam.GetBindingExpression(Run.TextProperty);
                                if (textBinding != null && !string.IsNullOrEmpty(runParam.Text))
                                {
                                    runParam.SetCurrentValue(Run.TextProperty, string.Empty);
                                    textBinding.UpdateTarget();
                                }
                            }
                            continue;
                        }
                    }

                    var run = new Run();
                    run.Text = part.Content ?? $"{{{part.ParameterName}}}";
                    inlines.InsertBefore(inlineParam1, run);
                }
                else
                {
                    var run = new Run();
                    run.Text = part.Content ?? $"{{{part.ParameterName}}}";
                    inlines.Add(run);
                }
            }
        }

        private bool HasInlineParam()
        {
            var inlines = GetInlineCollection();
            if (inlines != null && inlines.Count > 0)
            {
                foreach (var inline in inlines)
                {
                    var paramName = Text.GetParamName(inline);
                    if (!string.IsNullOrEmpty(paramName))
                        return true;
                }
            }

            return false;
        }

        private InlineCollection? GetInlineCollection()
        {
            var element = _element;
            if (element is TextBlock textBlock)
                return textBlock.Inlines;
            else if (element is Span span)
                return span.Inlines;
            else if (element is Paragraph paragraph)
                return paragraph.Inlines;
            else
                return null;
        }
        #endregion
    }

    internal class TextInlineCollection : List<KeyValuePair<string, Inline>>
    {
        public Inline? TakeInline(string paramName)
        {
            var index = -1;
            for (var i = 0; i < Count; i++)
            {
                if (string.Equals(paramName, this[i].Key, StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }

            if (index >= 0)
            {
                var inline = this[index].Value;
                RemoveAt(index);
                return inline;
            }

            return null;
        }

        public void AddInline(string paramName, Inline inline)
        {
            Add(new KeyValuePair<string, Inline>(paramName, inline));
        }
    }
}
