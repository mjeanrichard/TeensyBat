//// 
//// Teensy Bat Explorer - Copyright(C) 2018 Meinard Jean-Richard
////  
//// This program is free software: you can redistribute it and/or modify
//// it under the terms of the GNU General Public License as published by
//// the Free Software Foundation, either version 3 of the License, or
//// (at your option) any later version.
////  
//// This program is distributed in the hope that it will be useful,
//// but WITHOUT ANY WARRANTY; without even the implied warranty of
//// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//// GNU General Public License for more details.
////  
//// You should have received a copy of the GNU General Public License
//// along with this program.  If not, see <http://www.gnu.org/licenses/>.

//using System;
//using System.Collections.Generic;
//using System.Linq;

//using Windows.Foundation;
//using Windows.System;
//using Windows.UI;
//using Windows.UI.Xaml;
//using Windows.UI.Xaml.Controls;
//using Windows.UI.Xaml.Input;

//using Microsoft.Graphics.Canvas;
//using Microsoft.Graphics.Canvas.UI.Xaml;
//using Microsoft.Toolkit.Uwp.Helpers;

//using TeensyBatExplorer.Views.Project;

//namespace TeensyBatExplorer.Controls
//{
//    public class BarItemModel
//    {
//        public BarItemModel(uint time, int value)
//        {
//            Time = time;
//            Value = value;
//        }

//        public uint Time { get; set; }
//        public int Value { get; set; }
//    }

//    public sealed class TimeBarControl : UserControl
//    {
//        public static readonly DependencyProperty BarPaddingProperty = DependencyProperty.Register(
//            "BarPadding", typeof(double), typeof(TimeBarControl), new PropertyMetadata(1d));

//        public double BarPadding
//        {
//            get { return (double)GetValue(BarPaddingProperty); }
//            set { SetValue(BarPaddingProperty, value); }
//        }

//        public static readonly DependencyProperty CurrentDetailBarDurationProperty = DependencyProperty.Register(
//            "CurrentDetailBarDuration", typeof(long), typeof(TimeBarControl), new PropertyMetadata(default(long)));

//        public long CurrentDetailBarDuration
//        {
//            get { return (long)GetValue(CurrentDetailBarDurationProperty); }
//            set { SetValue(CurrentDetailBarDurationProperty, value); }
//        }

//        public static readonly DependencyProperty CurrentPositionProperty = DependencyProperty.Register(
//            "CurrentPosition", typeof(long), typeof(TimeBarControl), new PropertyMetadata(default(long)));

//        public long CurrentPosition
//        {
//            get { return (long)GetValue(CurrentPositionProperty); }
//            set
//            {
//                SetValue(CurrentPositionProperty, Math.Max(0, value));
//                CurrentPositionChanged();
//            }
//        }

//        public static readonly DependencyProperty DetailBarPaddingProperty = DependencyProperty.Register(
//            "DetailBarPadding", typeof(double), typeof(TimeBarControl), new PropertyMetadata(1d));

//        public double DetailBarPadding
//        {
//            get { return (double)GetValue(DetailBarPaddingProperty); }
//            set { SetValue(DetailBarPaddingProperty, value); }
//        }

//        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
//            "Items", typeof(IEnumerable<BarItemModel>), typeof(TimeBarControl), new PropertyMetadata(Enumerable.Empty<BarItemModel>()));

//        public IEnumerable<BarItemModel> Items
//        {
//            get { return (IEnumerable<BarItemModel>)GetValue(ItemsProperty); }
//            set
//            {
//                SetValue(ItemsProperty, value);
//                Invalidate(true);
//            }
//        }

//        public static readonly DependencyProperty MinBarWidthProperty = DependencyProperty.Register(
//            "MinBarWidth", typeof(double), typeof(TimeBarControl), new PropertyMetadata(5d));

//        public double MinBarWidth
//        {
//            get { return (double)GetValue(MinBarWidthProperty); }
//            set { SetValue(MinBarWidthProperty, value); }
//        }

//        public static readonly DependencyProperty MinDetailBarWidthProperty = DependencyProperty.Register(
//            "MinDetailBarWidth", typeof(double), typeof(TimeBarControl), new PropertyMetadata(5d));

//        public double MinDetailBarWidth
//        {
//            get { return (double)GetValue(MinDetailBarWidthProperty); }
//            set { SetValue(MinDetailBarWidthProperty, value); }
//        }

//        private static readonly int[] BarDurations = new int[]
//        {
//            2, 5, 10, 20, 30,
//            60, 2 * 60, 5 * 60, 10 * 60, 20 * 60, 30 * 60
//        };

//        private CanvasControl _canvas;

//        private bool _isDirty = true;
//        private BarModel _barModel;
//        private BarModel _detailModel;

//        public TimeBarControl()
//        {
//            IsTabStop = true;
//            Loaded += UserControl_Loaded;
//            Unloaded += UserControl_Unloaded;
//        }

//        private void UserControl_Loaded(object sender, RoutedEventArgs e)
//        {
//            _canvas = new CanvasControl();
//            _canvas.IsTabStop = false;
//            _canvas.Draw += CanvasOnDraw;
//            Content = _canvas;
//        }

//        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
//        {
//            // Explicitly remove references to allow the Win2D controls to get garbage collected
//            if (_canvas != null)
//            {
//                _canvas.RemoveFromVisualTree();
//                _canvas = null;
//            }
//        }

//        protected override Size MeasureOverride(Size availableSize)
//        {
//            if (double.IsInfinity(availableSize.Width))
//            {
//                availableSize.Width = 6000;
//            }

//            if (double.IsInfinity(availableSize.Height))
//            {
//                availableSize.Height = 6000;
//            }

//            _canvas?.Measure(availableSize);
//            return availableSize;
//        }

//        protected override void OnKeyDown(KeyRoutedEventArgs keyRoutedEventArgs)
//        {
//            if (keyRoutedEventArgs.Key == VirtualKey.Left)
//            {
//                CurrentPosition -= _detailModel.BarDuration;
//                keyRoutedEventArgs.Handled = true;
//            }
//            else if (keyRoutedEventArgs.Key == VirtualKey.Right)
//            {
//                CurrentPosition += _detailModel.BarDuration;
//                keyRoutedEventArgs.Handled = true;
//            }
//            else if (keyRoutedEventArgs.Key == VirtualKey.PageDown || keyRoutedEventArgs.Key == (VirtualKey.Right | VirtualKey.Shift))
//            {
//                CurrentPosition += _barModel.BarDuration;
//                keyRoutedEventArgs.Handled = true;
//            }
//            else if (keyRoutedEventArgs.Key == VirtualKey.PageUp || keyRoutedEventArgs.Key == (VirtualKey.Left | VirtualKey.Shift))
//            {
//                CurrentPosition -= _barModel.BarDuration;
//                keyRoutedEventArgs.Handled = true;
//            }
//        }

//        protected override void OnPointerPressed(PointerRoutedEventArgs pointerRoutedEventArgs)
//        {
//            Focus(FocusState.Programmatic);

//            Point position = pointerRoutedEventArgs.GetCurrentPoint(_canvas).Position;
//            if (position.Y < _canvas.ActualHeight / 2)
//            {
//                // Detail Region
//                ProcessBarClick(position, _detailModel);
//            }
//            else
//            {
//                // Bar Region
//                ProcessBarClick(position, _barModel);
//            }
//            pointerRoutedEventArgs.Handled = true;
//        }

//        private void CurrentPositionChanged()
//        {
//            long currentPosition = CurrentPosition;
//            if (_detailModel == null)
//            {
//                return;
//            }
//            if (_detailModel.GetIndex(currentPosition) < 0)
//            {
//                Invalidate(true);
//            }
//            else
//            {
//                Invalidate(false);
//            }
//        }

//        private void Invalidate(bool setDirty = false)
//        {
//            if (setDirty)
//            {
//                _isDirty = true;
//            }

//            if (_canvas != null)
//            {
//                _canvas.Invalidate();
//            }
//        }

//        private void CanvasOnDraw(CanvasControl sender, CanvasDrawEventArgs args)
//        {
//            if (_isDirty)
//            {
//                RebuildBars();
//            }

//            CanvasDrawingSession session = args.DrawingSession;
//            session.Antialiasing = CanvasAntialiasing.Aliased;

//            float bottom = (float)Math.Round(_canvas.ActualHeight);
//            float barHeight = (float)Math.Round(_canvas.ActualHeight / 2 - 2);
//            DrawBars(_barModel, barHeight+4, barHeight, Colors.Red, Colors.Green, session);
//            DrawBars(_detailModel, 0, barHeight, Colors.Green, Colors.Blue, session);
//        }

//        private void ProcessBarClick(Point position, BarModel model)
//        {
//            double barWidth = _canvas.ActualWidth / model.Bars.Length;
//            int barIndex = (int)(position.X / barWidth);
//            CurrentPosition = model.BarDuration * barIndex + model.Start;
//        }

//        private void DrawBars(BarModel model, float top, float height, Color color, Color highlightColor, CanvasDrawingSession session)
//        {
//            float totalBarWidth = (float)(_canvas.ActualWidth / model.Bars.Length);
//            float barWidth = (float)(Math.Round(totalBarWidth) - model.BarPadding);
//            float barAreaHeight = height - 3;
//            float barHeightFactor = barAreaHeight / model.MaxValue;
//            float barAreaBottom = top + barAreaHeight;

//            int highlightIndex = model.GetIndex(CurrentPosition);
            
//            float left = 0;
//            for (int i = 0; i < model.Bars.Length; i++)
//            {
//                float roundedLeft = (float)Math.Round(left);
//                float barCenter = roundedLeft + barWidth/2f;
//                int bar = model.Bars[i];
//                if (bar > 0)
//                {
//                    float barHeight = Math.Max(bar * barHeightFactor, 1);
//                    Rect rect = new Rect(roundedLeft, barAreaBottom - barHeight, barWidth, barHeight);
//                    if (i == highlightIndex)
//                    {
//                        session.DrawLine(barCenter, top, barCenter, barAreaBottom, highlightColor, 1);
//                        session.FillRectangle(rect, highlightColor);
//                    }
//                    else
//                    {
//                        session.FillRectangle(rect, color);
//                    }
//                }
//                else if (i == highlightIndex)
//                {
//                    //Rect rect = new Rect(roundedLeft, barAreaBottom - 20, totalBarWidth - model.BarPadding, 20);
//                    //session.DrawRectangle(rect, highlightColor);
//                    session.DrawLine(barCenter, top, barCenter, barAreaBottom, highlightColor, 2);
//                }

//                // Tick
//                session.DrawLine(barCenter, barAreaBottom, barCenter, barAreaBottom + 2, Colors.Black);

//                left += totalBarWidth;
//            }

//            left = (float)Math.Ceiling(left);
//            session.DrawLine(1, top, 1, barAreaBottom+1, Colors.Black);
//            session.DrawLine(1, barAreaBottom+1, left, barAreaBottom+1, Colors.Black, 1);
//        }

//        private void RebuildBars()
//        {
//            BarItemModel[] items = Items.ToArray();
//            (long Start, long Stop) barData = AnalyzeBars(items);
//            long duration = barData.Stop - barData.Start;

//            BarModel barModel = new BarModel(duration, barData.Start, MinBarWidth, BarPadding);
//            SetBarDuration(barModel);

//            int currentBarIndex = barModel.GetIndex(CurrentPosition);
//            BarModel detailModel;
//            if (currentBarIndex >= 0)
//            {
//                long detailStart = barModel.Start + barModel.BarDuration * currentBarIndex;
//                detailModel = new BarModel(barModel.BarDuration, detailStart, MinDetailBarWidth, DetailBarPadding);
//                SetBarDuration(detailModel);
//            }
//            else
//            {
//                detailModel = new BarModel(0, 0, MinDetailBarWidth, DetailBarPadding);
//                detailModel.BarDuration = 0;
//            }

//            barModel.InitBars();
//            detailModel.InitBars();

//            foreach (BarItemModel item in items)
//            {
//                barModel.AddItem(item);
//                detailModel.AddItem(item);
//            }

//            _barModel = barModel;
//            _detailModel = detailModel;
//            _isDirty = false;
//            CurrentDetailBarDuration = detailModel.BarDuration;
//        }

//        private void SetBarDuration(BarModel model)
//        {
//            double maxBars = _canvas.ActualWidth / (model.MinBarWidth + model.BarPadding);
//            int minDurationPerBar = (int)Math.Ceiling(model.TotalDuration / maxBars);

//            if (minDurationPerBar <= 1000)
//            {
//                model.BarDuration = (long)Math.Ceiling(minDurationPerBar / 100d) * 100;
//                return;
//            }

//            for (int i = 0; i < BarDurations.Length; i++)
//            {
//                if (minDurationPerBar < BarDurations[i] * 1000)
//                {
//                    model.BarDuration = BarDurations[i] * 1000;
//                    return;
//                }
//            }
//            // More that one Hour -> 1 Hour intervals
//            model.BarDuration = (long)Math.Ceiling(minDurationPerBar / (60 * 60 * 1000d)) * (60 * 60 * 1000);
//        }


//        private (long Start, long Stop) AnalyzeBars(BarItemModel[] items)
//        {
//            long start = long.MaxValue;
//            long stop = 0;

//            for (int i = 0; i < items.Length; i++)
//            {
//                long time = items[i].Time;
//                if (time < start)
//                {
//                    start = time;
//                }

//                if (time > stop)
//                {
//                    stop = time;
//                }
//            }

//            return (start, stop);
//        }

//        private class BarModel
//        {
//            public BarModel(long totalDuration, long start, double minBarWidth, double barPadding)
//            {
//                TotalDuration = totalDuration;
//                Start = start;
//                MinBarWidth = minBarWidth;
//                BarPadding = barPadding;
//                Bars = new int[0];
//            }

//            public long TotalDuration { get; }
//            public long Start { get; }
//            public double MinBarWidth { get; }
//            public double BarPadding { get; }

//            public int MaxValue { get; set; }
//            public int[] Bars { get; private set; }

//            public long BarDuration { get; set; }

//            public void InitBars()
//            {
//                if (BarDuration <= 0)
//                {
//                    return;
//                }
//                int barCount = (int)(TotalDuration / BarDuration + (TotalDuration % BarDuration > 0 ? 1 : 0));
//                Bars = new int[barCount];
//            }

//            public void AddItem(BarItemModel item)
//            {
//                if (Bars.Length <= 0)
//                {
//                    return;
//                }

//                uint time = item.Time;
//                if (time >= Start && time < Start + TotalDuration)
//                {
//                    int index = (int)((time - Start) / BarDuration);
//                    if (index >= Bars.Length)
//                    {
//                        index = Bars.Length - 1;
//                    }
//                    Bars[index] += item.Value;

//                    if (Bars[index] > MaxValue)
//                    {
//                        MaxValue = Bars[index];
//                    }
//                }
//            }

//            public int GetIndex(long time)
//            {
//                if (time >= Start && time < Start + TotalDuration)
//                {
//                    return (int)((time - Start) / BarDuration);
//                }
//                return -1;
//            }
//        }
//    }
//}