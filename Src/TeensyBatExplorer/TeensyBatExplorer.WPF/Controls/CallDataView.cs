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
        public static readonly DependencyProperty BatNodeProperty = DependencyProperty.Register(
            "BatNode", typeof(BatNode), typeof(CallDataView), new PropertyMetadata(null));

        public static readonly DependencyProperty BatCallProperty = DependencyProperty.Register(
            "BatCall", typeof(BatCall), typeof(CallDataView), new PropertyMetadata(null, OnBatCallChanged));

        private static void OnBatCallChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CallDataView)d).Invalidate();
        }

        public BatCall? BatCall
        {
            get => (BatCall?)GetValue(BatCallProperty);
            set => SetValue(BatCallProperty, value);
        }




        public BatNode? BatNode
        {
            get => (BatNode?)GetValue(BatNodeProperty);
            set => SetValue(BatNodeProperty, value);
        }

        private const int MaxVisibleFreq = 100;
        private const int CollapsedWidth = 9;
        private const int PauseSizeInCols = 9;
        private const int MicrosPerCol = 500;
        private const int LoudnessScale = 45;
        private const int StaticColOffset = 15;
        private const int IntensityLineWidth = 8;

        private static readonly Color PauseColor = Color.FromRgb(0x40, 0x40, 0x40);

        private readonly DrawValues _drawValues = new();

        private WriteableBitmap? _canvas;
        private ScrollBar? _scrollBar;
        private BatDataFileEntry[]? _entries;
        private Popup? _tooltip;
        private TextBlock? _tooltipText;
        private Line? _horizontalCursor1;
        private Line? _verticalCursor1;
        private Line? _horizontalCursor2;
        private Line? _verticalCursor2;

        static CallDataView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CallDataView), new FrameworkPropertyMetadata(typeof(CallDataView)));
        }


        public CallDataView()
        {
            RecreateImage(new Size(1, 1));
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
        }


        private void RecreateImage(Size size)
        {
            DpiScale dpi = VisualTreeHelper.GetDpi(this);

            int scaledWidth = (int)Math.Round(size.Width * dpi.DpiScaleX);
            int scaledHeight = (int)Math.Round(size.Height * dpi.DpiScaleY);
            if (_canvas == null || _canvas.PixelWidth != scaledWidth || _canvas.PixelHeight != scaledHeight)
            {
                _canvas = new WriteableBitmap(scaledWidth, scaledHeight, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32, null);
                _drawValues.Update(scaledHeight);
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (Template != null)
            {
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
            Point pos = e.GetPosition(this);

            DpiScale dpiScale = VisualTreeHelper.GetDpi(this);

            int posY = (int)Math.Round(pos.Y * dpiScale.DpiScaleY);
            int posX = (int)Math.Round(pos.X * dpiScale.DpiScaleX);

            if (posY >= _drawValues.LoudnessGraphBottom || pos.X >= ActualWidth)
            {
                HideCursor();
                return;
            }

            int columnWidth = _drawValues.ColumnWidth;
            int halfColumnWidth = (int)(_drawValues.ColumnWidth / 2) + 1;

            if (_horizontalCursor1 != null)
            {
                double y;
                if (posY < _drawValues.FftLowerBound)
                {
                    y = Math.Floor(posY / (double)columnWidth) * columnWidth + halfColumnWidth;
                }
                else
                {
                    y = posY;
                }

                _horizontalCursor1.Visibility = Visibility.Visible;
                _horizontalCursor1.X1 = 0;
                _horizontalCursor1.X2 = ActualWidth;
                _horizontalCursor1.Y1 = y / dpiScale.DpiScaleY;
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
                _verticalCursor1.Y2 = _drawValues.LoudnessGraphBottom;
                _verticalCursor1.X1 = (Math.Floor((posX - StaticColOffset) / (double)columnWidth) * columnWidth + halfColumnWidth + StaticColOffset) / dpiScale.DpiScaleX;
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
                PointInfo info = GetInfo(posX, posY);
                _tooltip.HorizontalOffset = pos.X + _tooltipText.ActualWidth + 20;
                _tooltip.VerticalOffset = pos.Y + _tooltipText.ActualHeight + 10;
                _tooltip.IsOpen = true;

                if (info.GapSize.HasValue)
                {
                    _tooltipText.Text = $"{info.GapSize}ms Pause";
                }
                else if (info.Time.HasValue && info.Frequency.HasValue)
                {
                    _tooltipText.Text = $"{info.Time:0.0}ms, {info.Frequency}kHz ({info.FreqIntensity?.ToString() ?? "-"})";
                }
                else if (info.Time.HasValue && info.Loudness.HasValue)
                {
                    _tooltipText.Text = $"{info.Time:0.0}ms, {info.Loudness}";
                }
                else
                {
                    _tooltip.IsOpen = false;
                }
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

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (_scrollBar == null)
            {
                return;
            }

            _scrollBar.Value -= e.Delta / 12;
            e.Handled = true;
        }

        private PointInfo GetInfo(int x, int y)
        {
            if (_entries == null || _scrollBar == null || BatCall == null)
            {
                return new PointInfo();
            }

            PointInfo info = new();

            int selectedCol = (int)(Math.Floor((x - StaticColOffset) / (double)_drawValues.ColumnWidth) + _scrollBar.Value) + 1;
            if (selectedCol >= 0)
            {
                int col = 0;
                for (int i = 0; i < _entries.Length; i++)
                {
                    BatDataFileEntry entry = _entries[i];

                    if (entry.PauseFromPrevEntryMicros.HasValue && entry.PauseFromPrevEntryMicros.Value > 0)
                    {
                        col += CollapsedWidth;
                    }

                    if (selectedCol <= col && i > 0)
                    {
                        // Pointed at a gap
                        info.GapSize = entry.PauseFromPrevEntryMicros / 1000d;
                        return info;
                    }

                    if (col + entry.FftCount >= selectedCol)
                    {
                        int yPos = (int)Math.Floor(y / (double)_drawValues.ColumnWidth);
                        int entryColumn = selectedCol - col;
                        if (entryColumn < entry.FftData.Count)
                        {
                            info.FreqIntensity = entry.FftData[entryColumn].Data[yPos];
                        }

                        long delta = entryColumn * MicrosPerCol;
                        info.Time = (entry.StartTimeMicros + delta - BatCall.StartTimeMicros) / 1000d;
                        break;
                    }

                    col += entry.FftCount;
                }
            }


            if (y <= _drawValues.FftLowerBound)
            {
                info.Frequency = MaxVisibleFreq - (int)Math.Floor(y / (double)_drawValues.ColumnWidth) - 1;
            }
            else
            {
                info.Loudness = (int)(_drawValues.LoudnessGraphBottom - y) * LoudnessScale;
            }

            return info;
        }

        private void OnScrollBarChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            InvalidateVisual();
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            RecreateImage(arrangeBounds);

            UpdateScrollBar();

            return base.ArrangeOverride(arrangeBounds);
        }

        private void Invalidate()
        {
            if (BatCall != null)
            {
                _entries = BatCall.Entries.OrderBy(e => e.StartTimeMicros).ToArray();
            }
            else
            {
                _entries = null;
            }

            if (_scrollBar != null)
            {
                _scrollBar.Value = 0;
            }

            InvalidateVisual();
        }

        private void UpdateScrollBar()
        {
            if (_scrollBar != null && BatCall != null && _entries != null)
            {
                int totalFftCount = _entries.Sum(e => e.FftCount);
                int pauseCount = _entries.Count(e => e.PauseFromPrevEntryMicros.HasValue && e.PauseFromPrevEntryMicros.Value > 0);
                int sum = totalFftCount + pauseCount * CollapsedWidth - (int)Math.Floor(ActualWidth / _drawValues.ColumnWidth);
                _scrollBar.Maximum = sum;
                _scrollBar.SmallChange = 1;
                _scrollBar.LargeChange = 10;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (_canvas == null)
            {
                return;
            }

            Draw();
            drawingContext.DrawImage(_canvas, new Rect(new Point(), new Size(_canvas.Width, _canvas.Height)));
        }

        private void Draw()
        {
            BatCall? batCall = BatCall;
            if (_canvas == null || batCall == null || _entries == null)
            {
                return;
            }

            using (BitmapContext context = _canvas.GetBitmapContext())
            {
                context.Clear();

                int columnsToSkip = 0;
                if (_scrollBar != null)
                {
                    columnsToSkip = (int)_scrollBar.Value;
                }

                int lastColumnsPosition = _canvas.PixelWidth - _drawValues.ColumnWidth;

                int skippedColumns = 0;
                int? lastLoudness = null;
                int tickPos = 0;

                int currentColumnLeft = StaticColOffset;

                for (int iEntry = 0; iEntry < _entries.Length; iEntry++)
                {
                    if (currentColumnLeft > _canvas.PixelWidth)
                    {
                        // past the end of the viewport
                        break;
                    }
                    
                    BatDataFileEntry entry = _entries[iEntry];

                    if (iEntry > 0 && entry.PauseFromPrevEntryMicros.HasValue)
                    {
                        if (entry.PauseFromPrevEntryMicros.Value > 0)
                        {
                            int pauseWidth = _drawValues.PauseWidth;
                            // Is a part of this pause visible?
                            if (skippedColumns + PauseSizeInCols >= columnsToSkip)
                            {
                                _canvas.FillRectangle(currentColumnLeft, 0, currentColumnLeft + pauseWidth, _drawValues.FftLowerBound, PauseColor);
                                currentColumnLeft += pauseWidth;
                            }

                            skippedColumns += PauseSizeInCols;
                            tickPos = 0;
                        }
                        else if (currentColumnLeft > StaticColOffset)
                        {
                            // Zero width pause: draw single pixel width line onto last column
                            int x = currentColumnLeft - 1;
                            _canvas.DrawLine(x, 0, x, _drawValues.FftLowerBound, PauseColor);
                        }
                    }

                    if (skippedColumns + entry.FftData.Count < columnsToSkip)
                    {
                        // The whole entry can be skipped
                        skippedColumns += entry.FftData.Count;
                        continue;
                    }


                    IList<FftBlock> fftBlocks = entry.FftData;
                    for (int fftIndex = 0; fftIndex < fftBlocks.Count; fftIndex++)
                    {
                        if (currentColumnLeft > lastColumnsPosition)
                        {
                            // outside of viewport on the right.
                            break;
                        }

                        if (skippedColumns < columnsToSkip)
                        {
                            // not YET in the viewport...
                            skippedColumns++;
                            continue;
                        }

                        FftBlock fftBlock = fftBlocks[fftIndex];
                        DrawFftColumn(fftBlock, currentColumnLeft, _drawValues);

                        int currentLoudness = fftBlock.Loudness / LoudnessScale;
                        if (lastLoudness.HasValue)
                        {
                            _canvas.FillRectangle(currentColumnLeft, _drawValues.LoudnessGraphBottom - lastLoudness.Value, currentColumnLeft + _drawValues.ColumnWidth, _drawValues.LoudnessGraphBottom, Colors.CornflowerBlue);
                        }
                        lastLoudness = currentLoudness;

                        // Draw bottom Tick
                        if (tickPos % 2 == 0)
                        {
                            if (tickPos % 20 == 0)
                            {
                                _canvas.DrawLine(currentColumnLeft, _drawValues.FftLowerBound, currentColumnLeft, _drawValues.FftLowerBound + 15, Colors.Black);
                            }
                            else if (tickPos % 10 == 0)
                            {
                                _canvas.DrawLine(currentColumnLeft, _drawValues.FftLowerBound, currentColumnLeft, _drawValues.FftLowerBound + 10, Colors.Black);
                            }
                            else
                            {
                                _canvas.DrawLine(currentColumnLeft, _drawValues.FftLowerBound, currentColumnLeft, _drawValues.FftLowerBound + 5, Colors.Black);
                            }
                        }

                        tickPos++;
                        currentColumnLeft += _drawValues.ColumnWidth;
                    }
                }

                DrawFftIntensityBar(_drawValues);

                BatNode? batNode = BatNode;
                if (batNode != null)
                {
                    int callStartThreshold = batNode.CallStartThreshold / LoudnessScale;
                    int callEndThreshold = batNode.CallEndThreshold / LoudnessScale;

                    _canvas.DrawLine(0, _drawValues.LoudnessGraphBottom - callStartThreshold, (skippedColumns - columnsToSkip) * _drawValues.ColumnWidth, _drawValues.LoudnessGraphBottom - callStartThreshold, Colors.Green);
                    _canvas.DrawLine(0, _drawValues.LoudnessGraphBottom - callEndThreshold, (skippedColumns - columnsToSkip) * _drawValues.ColumnWidth, _drawValues.LoudnessGraphBottom - callEndThreshold, Colors.Red);
                }
            }
        }

        private void DrawFftIntensityBar(DrawValues opts)
        {
            if (_entries == null)
            {
                return;
            }

            int[] totalIntensity = new int[128];
            foreach (FftBlock fftBlock in _entries.SelectMany(e => e.FftData))
            {
                for (int i = 0; i < fftBlock.Data.Length; i++)
                {
                    totalIntensity[i] += fftBlock.Data[i];
                }
            }

            int localMax = totalIntensity.Skip(10).Max();
            if (localMax > 0)
            {
                for (int i = 0; i < totalIntensity.Length; i++)
                {
                    Color pixelColor = ColorPalettes.BlueVioletRed[Math.Min(127, totalIntensity[i] * 127 / localMax)];

                    int y = opts.FftLowerBound - opts.ColumnWidth - i * opts.ColumnWidth;
                    _canvas.FillRectangle(0, y, IntensityLineWidth, y + opts.ColumnWidth, pixelColor);
                }
            }
        }

        private void DrawFftColumn(FftBlock fftBlock, int columnLeft, DrawValues drawValues)
        {
            int columnHeight = drawValues.ColumnWidth;

            for (int i = 0; i < MaxVisibleFreq; i++)
            {
                Color pixelColor = ColorPalettes.BlueVioletRed[Math.Min(127, (int)fftBlock.Data[i])];

                int y = drawValues.FftLowerBound - columnHeight - i * columnHeight;
                _canvas.FillRectangle(columnLeft, y, columnLeft + columnHeight, y + columnHeight, pixelColor);
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

        private class DrawValues
        {
            public void Update(int height)
            {
                LoudnessGraphHeight = 4095 / LoudnessScale;
                ColumnWidth = (height - LoudnessGraphHeight - 15) / MaxVisibleFreq;
                FftLowerBound = MaxVisibleFreq * ColumnWidth;
                LoudnessGraphBottom = FftLowerBound + 15 + LoudnessGraphHeight;
                PauseWidth = PauseSizeInCols * ColumnWidth;
            }

            public int LoudnessGraphBottom { get; private set; }
            public int LoudnessGraphHeight { get; private set; }
            public int MaxVisibleFreq { get; } = 100;
            public int ColumnWidth { get; private set; }
            public int FftLowerBound { get; private set; }
            public int PauseWidth { get; private set; }
        }

        private class PointInfo
        {
            public double? Time { get; set; }
            public int? Frequency { get; set; }
            public double? GapSize { get; set; }
            public int? Loudness { get; set; }
            public byte? FreqIntensity { get; set; }
        }
    }
}
