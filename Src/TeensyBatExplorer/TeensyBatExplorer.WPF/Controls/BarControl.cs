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
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TeensyBatExplorer.WPF.Controls
{
    public class BarItemModel
    {
        public BarItemModel(long time, int value)
        {
            Time = time;
            Value = value;
        }

        public long Time { get; set; }
        public int Value { get; set; }
        public int Id { get; set; }
    }

    public class BarControl : Control
    {
        public static readonly DependencyProperty StartTimeProperty = DependencyProperty.Register(
            "StartTime", typeof(DateTime), typeof(BarControl), new PropertyMetadata(default(DateTime)));

        public static readonly DependencyProperty ShowDetailsProperty = DependencyProperty.Register(
            "ShowDetails", typeof(bool), typeof(BarControl), new PropertyMetadata(default(bool), OnLayoutChanged));

        public static readonly DependencyProperty BarPaddingProperty = DependencyProperty.Register(
            "BarPadding", typeof(int), typeof(BarControl), new PropertyMetadata(1));

        public int BarPadding
        {
            get => (int)GetValue(BarPaddingProperty);
            set => SetValue(BarPaddingProperty, value);
        }

        public static readonly DependencyProperty CurrentDetailBarDurationProperty = DependencyProperty.Register(
            "CurrentDetailBarDuration", typeof(long), typeof(BarControl), new PropertyMetadata(default(long)));

        public long CurrentDetailBarDuration
        {
            get => (long)GetValue(CurrentDetailBarDurationProperty);
            set => SetValue(CurrentDetailBarDurationProperty, value);
        }

        public static readonly DependencyProperty CurrentPositionProperty = DependencyProperty.Register(
            "CurrentPosition", typeof(long), typeof(BarControl), new PropertyMetadata(default(long), OnPositionChanged));

        private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BarControl)d).OnPositionChanged((long)e.NewValue);
        }

        public long CurrentPosition
        {
            get => (long)GetValue(CurrentPositionProperty);
            set => SetValue(CurrentPositionProperty, Math.Max(0, value));
        }

        public static readonly DependencyProperty DetailBarPaddingProperty = DependencyProperty.Register(
            "DetailBarPadding", typeof(int), typeof(BarControl), new PropertyMetadata(1));

        public int DetailBarPadding
        {
            get => (int)GetValue(DetailBarPaddingProperty);
            set => SetValue(DetailBarPaddingProperty, value);
        }

        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
            nameof(Items), typeof(IEnumerable<BarItemModel>), typeof(BarControl), new PropertyMetadata(Enumerable.Empty<BarItemModel>(), OnItemsChanged));

        private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BarControl)d).SetDirty();
        }

        public IEnumerable<BarItemModel> Items
        {
            get => (IEnumerable<BarItemModel>)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public static readonly DependencyProperty MinBarWidthProperty = DependencyProperty.Register(
            "MinBarWidth", typeof(int), typeof(BarControl), new PropertyMetadata(5));

        public int MinBarWidth
        {
            get => (int)GetValue(MinBarWidthProperty);
            set => SetValue(MinBarWidthProperty, value);
        }

        public static readonly DependencyProperty MinDetailBarWidthProperty = DependencyProperty.Register(
            "MinDetailBarWidth", typeof(int), typeof(BarControl), new PropertyMetadata(5));

        public int MinDetailBarWidth
        {
            get => (int)GetValue(MinDetailBarWidthProperty);
            set => SetValue(MinDetailBarWidthProperty, value);
        }




        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BarControl)d).InvalidateVisual();
        }

        public bool ShowDetails
        {
            get => (bool)GetValue(ShowDetailsProperty);
            set => SetValue(ShowDetailsProperty, value);
        }


        public DateTime StartTime
        {
            get => (DateTime)GetValue(StartTimeProperty);
            set => SetValue(StartTimeProperty, value);
        }

        private const int LegendHeight = 12;
        private const int LegendPadding = 2;
        private const int BottomLinePadding = 2;

        private static readonly long[] BarDurations =
        {
            2, 5, 10, 20, 30,
            60, 2 * 60, 5 * 60, 10 * 60, 20 * 60, 30 * 60
        };

        private readonly Image _imageControl;
        private readonly VisualCollection _visualChildren;
        private readonly DrawingVisual _startTimeDrawingVisual;


        private bool _isDirty;
        private WriteableBitmap? _canvas;
        private BarModel? _barModel;
        private BarModel? _detailModel;

        public BarControl()
        {
            Focusable = true;
            _imageControl = new Image();
            _imageControl.SnapsToDevicePixels = true;
            _imageControl.Focusable = false;
            RenderOptions.SetBitmapScalingMode(_imageControl, BitmapScalingMode.NearestNeighbor);

            _startTimeDrawingVisual = new DrawingVisual();

            _visualChildren = new VisualCollection(this)
            {
                _imageControl,
                _startTimeDrawingVisual
            };
        }

        protected override int VisualChildrenCount => _visualChildren.Count;

        private void UpdateLegendVisuals()
        {
            if (_canvas == null)
            {
                return;
            }

            using (DrawingContext drawingContext = _startTimeDrawingVisual.RenderOpen())
            {
                if (_barModel != null && _barModel.Bars.Length > 0)
                {
                    FormattedText formattedText = new(StartTime.ToString("dd.MM. HH:mm:ss"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Verdana"), LegendHeight, Brushes.Black, 1);
                    drawingContext.DrawText(formattedText, new Point(0, _canvas.PixelHeight - LegendHeight));

                    int width = _canvas.PixelWidth / _barModel.Bars.Length * _barModel.Bars.Length;
                    DateTime endTime = StartTime.AddMilliseconds(_barModel.TotalDuration);
                    formattedText = new FormattedText(endTime.ToString("dd.MM. HH:mm:ss"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Verdana"), LegendHeight, Brushes.Black, 1);
                    drawingContext.DrawText(formattedText, new Point(width - formattedText.Width, _canvas.PixelHeight - LegendHeight));

                    DateTime currentTime = StartTime.AddMilliseconds(CurrentPosition);
                    formattedText = new FormattedText(currentTime.ToString("dd.MM. HH:mm:ss.ff"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Verdana"), LegendHeight, Brushes.Black, 1);
                    drawingContext.DrawText(formattedText, new Point((width - formattedText.Width) / 2, 0));
                }
            }
        }

        public event EventHandler<PositionEventArgs>? PositionChanged;

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            int height = (int)arrangeBounds.Height;
            int width = (int)arrangeBounds.Width;

            if (_canvas == null || (int)_canvas.Width != width)
            {
                _isDirty = true;
                _canvas = BitmapFactory.New(width, height);
                _imageControl.Source = _canvas;
                _imageControl.Arrange(new Rect(0, 0, width, height));
                UpdateLegendVisuals();
            }

            return base.ArrangeOverride(arrangeBounds);
        }

        protected override void OnKeyDown(KeyEventArgs keyEventArgs)
        {
            if (_detailModel == null || _barModel == null)
            {
                return;
            }

            if (keyEventArgs.Key == Key.Left)
            {
                CurrentPosition -= _detailModel.BarDuration;
                keyEventArgs.Handled = true;
                return;
            }

            if (keyEventArgs.Key == Key.Right)
            {
                CurrentPosition += _detailModel.BarDuration;
                keyEventArgs.Handled = true;
                return;
            }

            bool isShiftDown = Keyboard.IsKeyDown(Key.RightShift) || Keyboard.IsKeyDown(Key.LeftShift);
            if (keyEventArgs.Key == Key.PageDown || keyEventArgs.Key == Key.Right && isShiftDown)
            {
                CurrentPosition += _barModel.BarDuration;
                keyEventArgs.Handled = true;
            }

            if (keyEventArgs.Key == Key.PageUp || keyEventArgs.Key == Key.Left && isShiftDown)
            {
                CurrentPosition -= _barModel.BarDuration;
                keyEventArgs.Handled = true;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (_canvas == null)
            {
                return;
            }

            if (_isDirty || _barModel == null || _detailModel == null)
            {
                RebuildBars();
                UpdateLegendVisuals();
            }

            if (_barModel == null || _detailModel == null)
            {
                return;
            }

            using (BitmapContext context = _canvas.GetBitmapContext())
            {
                context.Clear();

                if (_barModel.Bars.Length <= 0)
                {
                    return;
                }

                if (ShowDetails && _detailModel.Bars.Any())
                {
                    int barHeight = (_canvas.PixelHeight - LegendHeight - LegendPadding) / 2 - BottomLinePadding;
                    DrawBars(_barModel, barHeight + 4, barHeight, Colors.Red, Colors.Green, _canvas);
                    DrawBars(_detailModel, 0, barHeight, Colors.Green, Colors.Blue, _canvas);
                }
                else
                {
                    int barHeight = _canvas.PixelHeight - LegendHeight - LegendPadding - BottomLinePadding;
                    DrawBars(_barModel, 4, barHeight, Colors.Red, Colors.Green, _canvas);
                }
            }
        }

        private void DrawBars(BarModel model, int top, int height, Color color, Color highlightColor, WriteableBitmap canvas)
        {
            if (_canvas == null)
            {
                return;
            }

            int totalBarWidth = _canvas.PixelWidth / model.Bars.Length;
            int barWidth = totalBarWidth - model.BarPadding;
            int barAreaHeight = height - 3;
            double barHeightFactor = barAreaHeight / (double)model.MaxValue;
            int barAreaBottom = top + barAreaHeight;

            int highlightIndex = model.GetIndex(CurrentPosition);

            int left = 2;
            for (int i = 0; i < model.Bars.Length; i++)
            {
                int barCenter = left + barWidth / 2;
                int bar = model.Bars[i];
                if (bar > 0)
                {
                    int barHeight = (int)Math.Max(Math.Round(bar * barHeightFactor), 1);
                    if (i == highlightIndex)
                    {
                        canvas.DrawLine(barCenter, top, barCenter, barAreaBottom, highlightColor);
                        canvas.FillRectangle(left, barAreaBottom - barHeight, left + barWidth, barAreaBottom, highlightColor);
                    }
                    else
                    {
                        canvas.FillRectangle(left, barAreaBottom - barHeight, left + barWidth, barAreaBottom, color);
                    }
                }
                else if (i == highlightIndex)
                {
                    canvas.DrawLine(barCenter, top, barCenter, barAreaBottom, highlightColor);
                }

                // Tick
                canvas.DrawLine(barCenter, barAreaBottom, barCenter, barAreaBottom + 2, Colors.Black);

                left += totalBarWidth;
            }

            canvas.DrawLine(1, top, 1, barAreaBottom + 1, Colors.Black);
            canvas.DrawLine(1, barAreaBottom + 1, left, barAreaBottom + 1, Colors.Black);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            Point position = e.GetPosition(_imageControl);
            if (ShowDetails && _detailModel?.Bars.Length > 0 && position.Y < _imageControl.ActualHeight / 2)
            {
                // Detail Region
                ProcessBarClick(position, _detailModel);
            }
            else if (_barModel != null)
            {
                // Bar Region
                ProcessBarClick(position, _barModel);
            }

            Focus();

            e.Handled = true;
        }

        private void ProcessBarClick(Point position, BarModel model)
        {
            if (_canvas == null || model.Bars.Length <= 0)
            {
                return;
            }

            int barWidth = _canvas.PixelWidth / model.Bars.Length;
            int barIndex = (int)(position.X / barWidth);
            CurrentPosition = model.BarDuration * barIndex + model.Start;
        }


        private void RebuildBars()
        {
            BarItemModel[] items = Items.ToArray();

            if (items.Length <= 0)
            {
                return;
            }

            (long Start, long Stop) barData = AnalyzeBars(items);
            long duration = barData.Stop - barData.Start;

            BarModel barModel = new(duration, barData.Start, MinBarWidth, BarPadding);
            SetBarDuration(barModel);

            int currentBarIndex = barModel.GetIndex(CurrentPosition);
            BarModel detailModel;
            if (currentBarIndex >= 0)
            {
                long detailStart = barModel.Start + barModel.BarDuration * (uint)currentBarIndex;
                detailModel = new BarModel(barModel.BarDuration, detailStart, MinDetailBarWidth, DetailBarPadding);
                SetBarDuration(detailModel);
            }
            else
            {
                detailModel = new BarModel(0, 0, MinDetailBarWidth, DetailBarPadding);
                detailModel.BarDuration = 0;
            }

            barModel.InitBars();
            detailModel.InitBars();

            foreach (BarItemModel item in items)
            {
                barModel.AddItem(item);
                detailModel.AddItem(item);
            }

            _barModel = barModel;
            _detailModel = detailModel;
            _isDirty = false;
            CurrentDetailBarDuration = detailModel.BarDuration;
        }

        private void SetBarDuration(BarModel model)
        {
            double maxBars = _imageControl.ActualWidth / (model.MinBarWidth + model.BarPadding);
            long minDurationPerBar = (long)Math.Ceiling(model.TotalDuration / maxBars);

            if (minDurationPerBar <= 1000)
            {
                model.BarDuration = (long)Math.Ceiling(minDurationPerBar / 100d) * 100;
                return;
            }

            for (int i = 0; i < BarDurations.Length; i++)
            {
                if (minDurationPerBar < BarDurations[i] * 1000)
                {
                    model.BarDuration = BarDurations[i] * 1000;
                    return;
                }
            }

            // More that one Hour -> 1 Hour intervals
            model.BarDuration = (long)Math.Ceiling(minDurationPerBar / (60 * 60 * 1000d)) * (60 * 60 * 1000);
        }

        private (long Start, long Stop) AnalyzeBars(BarItemModel[] items)
        {
            long start = long.MaxValue;
            long stop = 0;

            for (int i = 0; i < items.Length; i++)
            {
                long time = items[i].Time;
                if (time < start)
                {
                    start = time;
                }

                if (time > stop)
                {
                    stop = time;
                }
            }

            return (start, stop);
        }

        private void SetDirty()
        {
            _isDirty = true;
            InvalidateVisual();
        }

        protected virtual void OnPositionChanged(long newPosition)
        {
            SetDirty();
            PositionChanged?.Invoke(this, new PositionEventArgs(newPosition));
        }

        protected override Visual GetVisualChild(int index)
        {
            return _visualChildren[index];
        }

        private class BarModel
        {
            public BarModel(long totalDuration, long start, int minBarWidth, int barPadding)
            {
                TotalDuration = totalDuration;
                Start = start;
                MinBarWidth = minBarWidth;
                BarPadding = barPadding;
                Bars = new int[0];
            }

            public long TotalDuration { get; }
            public long Start { get; }
            public int MinBarWidth { get; }
            public int BarPadding { get; }

            public int MaxValue { get; set; }
            public int[] Bars { get; private set; }

            public long BarDuration { get; set; }

            public void InitBars()
            {
                if (BarDuration <= 0)
                {
                    return;
                }

                int barCount = (int)(TotalDuration / BarDuration + (TotalDuration % BarDuration > 0 ? 1 : 0));
                Bars = new int[barCount];
            }

            public void AddItem(BarItemModel item)
            {
                if (Bars.Length <= 0)
                {
                    return;
                }

                long time = item.Time;
                if (time >= Start && time < Start + TotalDuration)
                {
                    int index = (int)((time - Start) / BarDuration);
                    if (index >= Bars.Length)
                    {
                        index = Bars.Length - 1;
                    }

                    Bars[index] += item.Value;

                    if (Bars[index] > MaxValue)
                    {
                        MaxValue = Bars[index];
                    }
                }
            }

            public int GetIndex(long time)
            {
                if (time >= Start && time <= Start + TotalDuration)
                {
                    return (int)((time - Start) / BarDuration);
                }

                return -1;
            }
        }
    }

    public class PositionEventArgs : EventArgs
    {
        public PositionEventArgs(long newPosition)
        {
            NewPosition = newPosition;
        }

        public long NewPosition { get; private set; }
    }
}