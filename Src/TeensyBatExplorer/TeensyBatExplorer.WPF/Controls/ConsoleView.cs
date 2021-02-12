// 
// Teensy Bat Explorer - Copyright(C) 2020 Meinrad Jean-Richard
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace TeensyBatExplorer.WPF.Controls
{
    [TemplatePart(Name = "PART_FlowDocumentScrollViewer", Type = typeof(Border))]
    public class ConsoleView : Control
    {
        public static readonly DependencyProperty AutoScrollProperty = DependencyProperty.Register(
            "AutoScroll", typeof(bool), typeof(ConsoleView), new PropertyMetadata(true, OnAutoScrollChanged));

        private static void OnAutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ConsoleView)d).UpdateAutoScroll((bool)e.NewValue);
        }

        public bool AutoScroll
        {
            get => (bool)GetValue(AutoScrollProperty);
            set => SetValue(AutoScrollProperty, value);
        }

        public static readonly DependencyProperty MaxLineCountProperty = DependencyProperty.Register(
            "MaxLineCount", typeof(int), typeof(ConsoleView), new PropertyMetadata(200));

        public int MaxLineCount
        {
            get => (int)GetValue(MaxLineCountProperty);
            set => SetValue(MaxLineCountProperty, value);
        }

        public static readonly DependencyProperty TextProviderProperty = DependencyProperty.Register(
            "TextProvider", typeof(IConsoleTextProvider), typeof(ConsoleView), new PropertyMetadata(default(IConsoleTextProvider), ConsoleTextProviderChanged));

        private static void ConsoleTextProviderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ConsoleView)d).UpdateTextProvider((IConsoleTextProvider)e.OldValue, (IConsoleTextProvider)e.NewValue);
        }

        public IConsoleTextProvider TextProvider
        {
            get => (IConsoleTextProvider)GetValue(TextProviderProperty);
            set => SetValue(TextProviderProperty, value);
        }

        private readonly FlowDocument _flowDocument;
        private FlowDocumentScrollViewer? _flowDocumentViewer;
        private ScrollViewer? _scrollViewer;
        private bool _appendToLastLine;

        static ConsoleView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ConsoleView), new FrameworkPropertyMetadata(typeof(ConsoleView)));
        }

        public ConsoleView()
        {
            _flowDocument = new FlowDocument();
        }

        private void UpdateAutoScroll(bool autoScroll)
        {
            if (autoScroll && _scrollViewer != null)
            {
                _scrollViewer.ScrollToEnd();
            }
        }

        private void UpdateTextProvider(IConsoleTextProvider? oldValue, IConsoleTextProvider? newValue)
        {
            if (oldValue != null)
            {
                oldValue.LineAvailable -= OnLineAvailable;
            }

            if (newValue != null)
            {
                newValue.LineAvailable += OnLineAvailable;
            }
        }

        private void OnLineAvailable(object? sender, string e)
        {
            string trimed = e.Trim('\0');

            bool newAppendToLastLine = true;
            if (trimed.EndsWith('\n') || trimed.EndsWith('\r'))
            {
                newAppendToLastLine = false;
                trimed = trimed.Trim('\n', '\r');
            }

            Paragraph? paragraph = null;
            if (_appendToLastLine)
            {
                paragraph = _flowDocument.Blocks.LastBlock as Paragraph;
            }

            if (paragraph == null)
            {
                paragraph = new Paragraph();
                _flowDocument.Blocks.Add(paragraph);
            }

            paragraph.Inlines.Add(trimed);

            _appendToLastLine = newAppendToLastLine;

            int maxLineCount = MaxLineCount;

            while (_flowDocument.Blocks.Count > maxLineCount)
            {
                _flowDocument.Blocks.Remove(_flowDocument.Blocks.FirstBlock);
            }

            if (AutoScroll)
            {
                _scrollViewer?.ScrollToEnd();
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (Template != null)
            {
                FlowDocumentScrollViewer? flowDocumentViewer = Template.FindName("PART_FlowDocumentScrollViewer", this) as FlowDocumentScrollViewer;
                if (flowDocumentViewer != null && flowDocumentViewer != _flowDocumentViewer)
                {
                    _flowDocumentViewer = flowDocumentViewer;
                    _flowDocumentViewer.Document = _flowDocument;
                    _flowDocumentViewer.ApplyTemplate();

                    ScrollViewer? scrollViewer = _flowDocumentViewer.Template?.FindName("PART_ContentHost", _flowDocumentViewer) as ScrollViewer;
                    if (scrollViewer != _scrollViewer)
                    {
                        if (_scrollViewer != null)
                        {
                            _scrollViewer.ScrollChanged -= OnScrollChanged;
                        }

                        _scrollViewer = scrollViewer;

                        if (_scrollViewer != null)
                        {
                            _scrollViewer.ScrollChanged += OnScrollChanged;
                            _scrollViewer.VerticalAlignment = VerticalAlignment.Stretch;
                        }
                    }
                }
            }
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_scrollViewer == null)
            {
                return;
            }

            if (e.ExtentHeightChange == 0 && e.ViewportHeightChange == 0)
            {
                if (_scrollViewer.VerticalOffset >= _scrollViewer.ScrollableHeight)
                {
                    AutoScroll = true;
                }
                else
                {
                    AutoScroll = false;
                }
            }
            else
            {
                if (AutoScroll)
                {
                    _scrollViewer?.ScrollToEnd();
                }
            }
        }
    }
}