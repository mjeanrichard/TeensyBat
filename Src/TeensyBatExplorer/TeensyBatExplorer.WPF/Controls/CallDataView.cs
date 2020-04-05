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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using TeensyBatExplorer.Core.Models;

namespace TeensyBatExplorer.WPF.Controls
{
    [TemplatePart(Name = "PART_Image", Type = typeof(Image))]
    [TemplatePart(Name = "PART_Scroll", Type = typeof(ScrollBar))]
    [TemplatePart(Name = "PART_Tooltip", Type = typeof(Popup))]
    [TemplatePart(Name = "PART_TooltipText", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_HorizontalCursor1", Type = typeof(Line))]
    [TemplatePart(Name = "PART_VerticalCursor1", Type = typeof(Line))]
    [TemplatePart(Name = "PART_HorizontalCursor2", Type = typeof(Line))]
    [TemplatePart(Name = "PART_VerticalCursor2", Type = typeof(Line))]
    public class CallDataView : Control
    {
        public static readonly DependencyProperty BatCallProperty = DependencyProperty.Register(
            "BatCall", typeof(BatCall), typeof(CallDataView), new PropertyMetadata(default(BatCall), OnBatCallChanged));

        private static void OnBatCallChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CallDataView)d).Invalidate();
        }

        public BatCall BatCall
        {
            get { return (BatCall)GetValue(BatCallProperty); }
            set { SetValue(BatCallProperty, value); }
        }

        public static readonly DependencyProperty CollapsePausesProperty = DependencyProperty.Register(
            "CollapsePauses", typeof(bool), typeof(CallDataView), new PropertyMetadata(default(bool)));

        public bool CollapsePauses
        {
            get { return (bool)GetValue(CollapsePausesProperty); }
            set { SetValue(CollapsePausesProperty, value); }
        }

        private const int RowHeight = 3;
        private const int ColWidth = 3;

        private const int FftLowerBound = 128 * RowHeight;
        private const int CollapsedWidth = 5;
        private const int LoudnessLowerBound = FftLowerBound + 15 + 200;


        private WriteableBitmap _canvas;
        private Image _imageControl;
        private ScrollBar _scrollBar;
        private BatDataFileEntry[] _entries;
        private Popup _tooltip;
        private TextBlock _tooltipText;
        private Line _horizontalCursor1;
        private Line _verticalCursor1;
        private Line _horizontalCursor2;
        private Line _verticalCursor2;

        static CallDataView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CallDataView), new FrameworkPropertyMetadata(typeof(CallDataView)));
        }


        public CallDataView()
        {
            _canvas = BitmapFactory.New(1024, 1024);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (Template != null)
            {
                if (Template.FindName("PART_Image", this) is Image image)
                {
                    _imageControl = image;
                    _imageControl.SnapsToDevicePixels = true;
                    _imageControl.Source = _canvas;
                    _imageControl.Focusable = false;
                    RenderOptions.SetBitmapScalingMode(_imageControl, BitmapScalingMode.NearestNeighbor);
                }

                if (Template.FindName("PART_Scroll", this) is ScrollBar scrollBar)
                {
                    _scrollBar = scrollBar;
                    _scrollBar.ValueChanged += OnScrollBarChanged;
                }

                if (Template.FindName("PART_Tooltip", this) is Popup tooltip)
                {
                    _tooltip = tooltip;
                }

                if (Template.FindName("PART_TooltipText", this) is TextBlock tooltipText)
                {
                    _tooltipText = tooltipText;
                }

                if (Template.FindName("PART_HorizontalCursor1", this) is Line horizontalCursor1)
                {
                    _horizontalCursor1 = horizontalCursor1;
                }

                if (Template.FindName("PART_VerticalCursor1", this) is Line verticalCursor1)
                {
                    _verticalCursor1 = verticalCursor1;
                }

                if (Template.FindName("PART_HorizontalCursor2", this) is Line horizontalCursor2)
                {
                    _horizontalCursor2 = horizontalCursor2;
                }

                if (Template.FindName("PART_VerticalCursor2", this) is Line verticalCursor2)
                {
                    _verticalCursor2 = verticalCursor2;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            Point pos = e.GetPosition(_imageControl);

            if (pos.Y >= LoudnessLowerBound || pos.X >= ActualWidth)
            {
                HideCursor();
                return;
            }

            if (_horizontalCursor1 != null)
            {
                _horizontalCursor1.Visibility = Visibility.Visible;
                _horizontalCursor1.X1 = 0;
                _horizontalCursor1.X2 = ActualWidth;
                _horizontalCursor1.Y1 = Math.Floor(pos.Y / RowHeight) * RowHeight + 2;
                _horizontalCursor1.Y2 = _horizontalCursor1.Y1;

                if (_horizontalCursor2 != null)
                {
                    _horizontalCursor2.Visibility = _horizontalCursor1.Visibility;
                    _horizontalCursor2.X1 = _horizontalCursor1.X1;
                    _horizontalCursor2.X2 = _horizontalCursor1.X2;
                    _horizontalCursor2.Y1 = _horizontalCursor1.Y1;
                    _horizontalCursor2.Y2 = _horizontalCursor1.Y2;
                }
            }

            if (_verticalCursor1 != null)
            {
                _verticalCursor1.Visibility = Visibility.Visible;
                _verticalCursor1.Y1 = 0;
                _verticalCursor1.Y2 = LoudnessLowerBound;
                _verticalCursor1.X1 = Math.Floor(pos.X / ColWidth) * ColWidth + 2;
                _verticalCursor1.X2 = _verticalCursor1.X1;

                if (_verticalCursor2 != null)
                {
                    _verticalCursor2.Visibility = _verticalCursor1.Visibility;
                    _verticalCursor2.X1 = _verticalCursor1.X1;
                    _verticalCursor2.X2 = _verticalCursor1.X2;
                    _verticalCursor2.Y1 = _verticalCursor1.Y1;
                    _verticalCursor2.Y2 = _verticalCursor1.Y2;
                }
            }

            if (_tooltip != null && _tooltipText != null)
            {
                PointInfo info = GetInfo(pos);
                if (info.GapSize.HasValue)
                {
                    _tooltipText.Text = $"{info.GapSize}ms Pause";
                }
                else if (info.Frequency.HasValue)
                {
                    _tooltipText.Text = $"{info.Time:0.0}ms, {info.Frequency}kHz";
                }
                else if (info.Loudness.HasValue)
                {
                    _tooltipText.Text = $"{info.Time:0.0}ms, {info.Loudness}";
                }

                _tooltip.HorizontalOffset = pos.X + _tooltipText.ActualWidth + 20;
                _tooltip.VerticalOffset = pos.Y + _tooltipText.ActualHeight + 20;
                _tooltip.IsOpen = true;
            }
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            if (_tooltip != null)
            {
                _tooltip.IsOpen = true;
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            HideCursor();
        }

        private PointInfo GetInfo(Point position)
        {
            if (_entries == null)
            {
                return new PointInfo();
            }

            PointInfo info = new PointInfo();

            int selectedCol = (int)Math.Floor(position.X / ColWidth);
            int col = 0;
            for (int i = 0; i < _entries.Length; i++)
            {
                BatDataFileEntry entry = _entries[i];
                if (selectedCol < col)
                {
                    // Pointed at a gap
                    info.GapSize = (entry.StartTime - BatCall.StartTime).TotalMilliseconds;
                    return info;
                }

                if (col + entry.FftCount >= selectedCol)
                {
                    double delta = (_scrollBar.Value + selectedCol - col) / 2d;
                    info.Time = (entry.StartTime - BatCall.StartTime).TotalMilliseconds + delta;
                    break;
                }

                col += entry.FftCount + CollapsedWidth;
            }


            if (position.Y <= FftLowerBound)
            {
                info.Frequency = (128 - (int)Math.Floor(position.Y / RowHeight)) * 2 - 1;
            }
            else
            {
                info.Loudness = (int)(LoudnessLowerBound - position.Y) * 21;
            }

            return info;
        }

        private void OnScrollBarChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            InvalidateVisual();
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            int height = (int)arrangeBounds.Height;
            int width = (int)arrangeBounds.Width;

            if ((int)_canvas.Width != width)
            {
                _canvas = BitmapFactory.New(width, 1024);
                if (_imageControl != null)
                {
                    _imageControl.InvalidateArrange();
                    _imageControl.Source = _canvas;
                }
            }

            return base.ArrangeOverride(arrangeBounds);
        }

        private void Invalidate()
        {
            if (BatCall != null)
            {
                _entries = BatCall.Entries.OrderBy(e => e.StartTime).ToArray();
            }
            else
            {
                _entries = null;
            }

            if (_scrollBar != null && BatCall != null && _entries != null)
            {
                BatDataFileEntry lastEntry = _entries.Last();
                int sum = (int)((lastEntry.StartTime - BatCall.StartTime).TotalMilliseconds * 2 + lastEntry.FftCount) - (int)Math.Ceiling(_canvas.Width / ColWidth);
                _scrollBar.Maximum = sum;
                _scrollBar.SmallChange = 1;
                _scrollBar.LargeChange = 10;
                _scrollBar.Value = 0;
            }

            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            Draw();
        }

        private void Draw()
        {
            BatCall batCall = BatCall;
            if (_imageControl == null || batCall == null)
            {
                return;
            }

            Debug.WriteLine("Invalidate");
            bool collapse = CollapsePauses;

            using (BitmapContext context = _canvas.GetBitmapContext())
            {
                context.Clear();

                int startOffsetCol = 0;
                if (_scrollBar != null)
                {
                    startOffsetCol = (int)_scrollBar.Value;
                }

                DateTime startTime = batCall.StartTime;

                int endCol = (int)Math.Ceiling(_canvas.Width / ColWidth) + startOffsetCol;

                int iCol = 0;
                int lastLoudness = 0;
                for (int iEntry = 0; iEntry < _entries.Length; iEntry++)
                {
                    BatDataFileEntry entry = _entries[iEntry];
                    if (iEntry > 0)
                    {
                        if (collapse)
                        {
                            if (iCol + CollapsedWidth >= startOffsetCol)
                            {
                                int x1 = iCol - startOffsetCol;
                                int x2 = x1 + CollapsedWidth;
                                if (iCol <= startOffsetCol)
                                {
                                    x1 = 0;
                                }

                                _canvas.FillRectangle(x1 * ColWidth, 0, x2 * ColWidth, FftLowerBound, Color.FromRgb(0x40, 0x40, 0x40));
                                iCol += CollapsedWidth;
                            }
                        }
                        else
                        {
                            int entryStartCol = (int)(entry.StartTime - startTime).TotalMilliseconds * 2;
                            if (entryStartCol > iCol)
                            {
                                if (entryStartCol >= startOffsetCol)
                                {
                                    int x1 = iCol - startOffsetCol;
                                    int x2 = entryStartCol - startOffsetCol;

                                    if (iCol <= startOffsetCol)
                                    {
                                        x1 = 0;
                                    }

                                    _canvas.FillRectangle(x1 * ColWidth, 0, x2 * ColWidth, FftLowerBound, Color.FromRgb(0x40, 0x40, 0x40));
                                }

                                iCol = entryStartCol;
                            }
                        }
                    }

                    IList<FftBlock> fftBlocks = entry.FftData;
                    int tickPos = 0;
                    for (int fftIndex = 0; fftIndex < fftBlocks.Count; fftIndex++)
                    {
                        if (iCol < startOffsetCol)
                        {
                            iCol++;
                            continue;
                        }

                        if (iCol > endCol)
                        {
                            return;
                        }

                        int columnStart = (iCol - startOffsetCol) * ColWidth;

                        FftBlock fftBlock = fftBlocks[fftIndex];
                        for (int i = 0; i < fftBlock.Data.Length; i++)
                        {
                            Color pixelColor = ColorPalettes.BlueVioletRed[Math.Min(127, (int)fftBlock.Data[i])];

                            int x = columnStart;
                            int y = FftLowerBound - RowHeight - i * RowHeight;
                            _canvas.FillRectangle(x, y, x + ColWidth, y + RowHeight, pixelColor);
                        }

                        int currentLoudness = fftBlock.Loudness / 21;
                        if (iCol > 0)
                        {
                            _canvas.FillRectangle(columnStart, LoudnessLowerBound - lastLoudness, columnStart + ColWidth, LoudnessLowerBound, Colors.CornflowerBlue);
                        }

                        lastLoudness = currentLoudness;

                        // Draw bottom Tick
                        if (tickPos % 2 == 0)
                        {
                            if (tickPos % 20 == 0)
                            {
                                _canvas.DrawLine(columnStart + 1, FftLowerBound, columnStart + 1, FftLowerBound + 15, Colors.Black);
                            }
                            else
                            {
                                _canvas.DrawLine(columnStart + 1, FftLowerBound, columnStart + 1, FftLowerBound + 5, Colors.Black);
                            }
                        }

                        tickPos++;
                        iCol++;
                    }
                }

                //float width = 1f;
                //for (int i = 0; i < 10; i++)
                //{
                //    int y = FftLowerBound - i * 10 * RowHeight;
                //    _canvas.DrawLine(0, y, fftData.Count * ColWidth, y, Colors.White);
                //}

                //_canvas.DrawLine(0, LoudnessLowerBound - 2400 / 21, fftData.Count * ColWidth, LoudnessLowerBound - 2400 / 21, Colors.Green);
            }
        }


        private void HideCursor()
        {
            if (_horizontalCursor1 != null)
            {
                _horizontalCursor1.Visibility = Visibility.Hidden;
            }

            if (_horizontalCursor2 != null)
            {
                _horizontalCursor2.Visibility = Visibility.Hidden;
            }

            if (_verticalCursor1 != null)
            {
                _verticalCursor1.Visibility = Visibility.Hidden;
            }

            if (_verticalCursor2 != null)
            {
                _verticalCursor2.Visibility = Visibility.Hidden;
            }

            if (_tooltip != null)
            {
                _tooltip.IsOpen = false;
            }
        }

        private class PointInfo
        {
            public double? Time { get; set; }
            public int? Frequency { get; set; }
            public double? GapSize { get; set; }
            public int? Loudness { get; set; }
        }
    }
}