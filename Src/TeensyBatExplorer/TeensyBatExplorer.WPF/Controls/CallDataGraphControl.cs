//// 
//// Teensy Bat Explorer - Copyright(C) 2020 Meinrad Jean-Richard
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
//using System.Diagnostics;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Input;
//using System.Windows.Media;

//using Microsoft.Graphics.Canvas;
//using Microsoft.Graphics.Canvas.Geometry;
//using Microsoft.Graphics.Canvas.UI.Xaml;

//using TeensyBatExplorer.Core.Models;

//namespace TeensyBatExplorer.WPF.Controls
//{
//    public sealed class CallDataGraphControl : UserControl
//    {
//        public static readonly DependencyProperty CallProperty = DependencyProperty.Register(
//            "Call", typeof(BatCall), typeof(CallDataGraphControl), new PropertyMetadata(default(BatCall)));

//        public BatCall Call
//        {
//            get { return (BatCall)GetValue(CallProperty); }
//            set { SetValue(CallProperty, value); }
//        }

//        private const int RowHeight = 4;
//        private const int ColWidth = 4;

//        private const int FftLowerBound = 128 * ColWidth;
//        private const int LoudnessLowerBound = FftLowerBound + 200;

//        private CanvasControl _canvas;

//        private bool _isDirty = true;

//        public CallDataGraphControl()
//        {
//            IsTabStop = true;
//            Loaded += UserControl_Loaded;
//            Unloaded += UserControl_Unloaded;
//            MouseDown += OnMouseDown;
//        }

//        private void OnMouseDown(object sender, MouseButtonEventArgs e)
//        {
//            if (_canvas == null)
//            {
//                return;
//            }

//            Point position = e.GetPosition(_canvas);
//            int x = (int)(position.X / ColWidth);
//            int y = 128 - (int)(position.Y / RowHeight);

//            IList<FftBlock> fftData = Call.FftData;
//            if (fftData.Count <= x || x < 0 || y < 0 || y > 127)
//            {
//                Debug.WriteLine($"{x}/{y} -> XXX");
//                return;
//            }

//            Debug.WriteLine($"{x}/{y} -> {fftData[x].Data[y]}");
//        }

//        private void UserControl_Loaded(object sender, RoutedEventArgs e)
//        {
//            _canvas = new CanvasControl();
//            //_canvas.IsTabStop = false;
//            //_canvas.Draw += CanvasOnDraw;
//            //_canvas.MouseDown += OnPointerPressed;
//            Content = _canvas;
//        }

//        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
//        {
//            // Explicitly remove references to allow the Win2D controls to get garbage collected
//            if (_canvas != null)
//            {
//                //_canvas.RemoveFromVisualTree();
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


//        //protected override void OnPointerPressed(PointerRoutedEventArgs pointerRoutedEventArgs)
//        //{
//        //    Focus(FocusState.Programmatic);

//        //    pointerRoutedEventArgs.Handled = true;
//        //}

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
//            if (Call == null)
//            {
//                return;
//            }

//            CanvasDrawingSession session = args.DrawingSession;
//            session.Antialiasing = CanvasAntialiasing.Aliased;
//            CanvasStrokeStyle hairlineStrokeStyle = new CanvasStrokeStyle()
//            {
//                TransformBehavior = CanvasStrokeTransformBehavior.Hairline
//            };
            
//            IList<FftBlock> fftData = Call.FftData;
//            for (int iCol = 0; iCol < fftData.Count; iCol++)
//            {
//                int columnCenter = iCol * ColWidth;

//                FftBlock fftBlock = fftData[iCol];
//                for (int i = 0; i < fftBlock.Data.Length; i++)
//                {
//                    Color pixelColor = ColorPalettes.BlueVioletRed[Math.Min(127, (int)fftBlock.Data[i])];
//                    session.FillRectangle(columnCenter - ColWidth / 2, FftLowerBound - i * RowHeight, ColWidth, RowHeight, pixelColor);
//                }

//                if (iCol < fftData.Count - 1)
//                {
//                    int scaledLoudness = fftData[iCol + 1].Loudness / 21;
//                    session.DrawLine(columnCenter, LoudnessLowerBound - fftBlock.Loudness / 21, columnCenter + ColWidth, LoudnessLowerBound - scaledLoudness, Colors.Black);
//                }

//                if (iCol % 2 == 0)
//                {
//                    // Draw bottom Tick
//                    session.DrawLine(columnCenter, FftLowerBound, columnCenter, FftLowerBound + 5, Colors.Black);
//                }
//            }

//            float width = 1f;
//            for (int i = 0; i < 10; i++)
//            {
//                int y = FftLowerBound - i * 10 * RowHeight;
//                session.DrawLine(0f, y, fftData.Count * ColWidth, y, Colors.White, 1f, hairlineStrokeStyle);
//            }

//            session.DrawLine(0f, LoudnessLowerBound - 2400 / 21, fftData.Count * ColWidth, LoudnessLowerBound - 2400 / 21, Colors.Green, width);
//        }
//    }
//}