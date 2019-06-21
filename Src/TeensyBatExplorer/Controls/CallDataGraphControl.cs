// 
// Teensy Bat Explorer - Copyright(C) 2018 Meinrad Jean-Richard
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

using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;

using TeensyBatExplorer.Services;

namespace TeensyBatExplorer.Controls
{
    public sealed class CallDataGraphControl : UserControl
    {
        public static readonly DependencyProperty CallDataProperty = DependencyProperty.Register("CallData", typeof(BatLog2), typeof(CallDataGraphControl), new PropertyMetadata(default(BatLog2)));

        public BatLog2 CallData
        {
            get { return (BatLog2)GetValue(CallDataProperty); }
            set
            {
                SetValue(CallDataProperty, value);
                Invalidate(true);
            }
        }

        private CanvasControl _canvas;

        private bool _isDirty = true;

        public CallDataGraphControl()
        {
            IsTabStop = true;
            Loaded += UserControl_Loaded;
            Unloaded += UserControl_Unloaded;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _canvas = new CanvasControl();
            _canvas.IsTabStop = false;
            _canvas.Draw += CanvasOnDraw;
            Content = _canvas;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // Explicitly remove references to allow the Win2D controls to get garbage collected
            if (_canvas != null)
            {
                _canvas.RemoveFromVisualTree();
                _canvas = null;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (double.IsInfinity(availableSize.Width))
            {
                availableSize.Width = 6000;
            }

            if (double.IsInfinity(availableSize.Height))
            {
                availableSize.Height = 6000;
            }

            _canvas?.Measure(availableSize);
            return availableSize;
        }

        protected override void OnPointerPressed(PointerRoutedEventArgs pointerRoutedEventArgs)
        {
            Focus(FocusState.Programmatic);

            pointerRoutedEventArgs.Handled = true;
        }

        private void Invalidate(bool setDirty = false)
        {
            if (setDirty)
            {
                _isDirty = true;
            }

            if (_canvas != null)
            {
                _canvas.Invalidate();
            }
        }

        private void CanvasOnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (CallData == null)
            {
                return;
            }

            CanvasDrawingSession session = args.DrawingSession;
            session.Antialiasing = CanvasAntialiasing.Aliased;

            const int rowHeight = 4;
            const int colWidth = 4;
            
            const int fftLowerBound = 128 * colWidth;
            const int loudnessLowerBound = fftLowerBound + 200;
            
            
            List<FftBlock> fftData = CallData.FftData;
            for (int iCol = 0; iCol < fftData.Count; iCol++)
            {
                int columnCenter = iCol * colWidth;

                FftBlock fftBlock = fftData[iCol];
                for (int i = 0; i < fftBlock.Data.Length; i++)
                {
                    Color pixelColor = ColorPalettes.BlueVioletRed[Math.Min(127, fftBlock.Data[i] * 2)];
                    session.FillRectangle(columnCenter - colWidth / 2, fftLowerBound - i * rowHeight, colWidth, rowHeight, pixelColor);
                }

                if (iCol < fftData.Count - 1)
                {
                    int scaledLoudness = fftData[iCol + 1].Loudness / 21;
                    session.DrawLine(columnCenter, loudnessLowerBound - (fftBlock.Loudness/21), columnCenter + colWidth, loudnessLowerBound - scaledLoudness, Colors.Black);
                }

                if (iCol % 2 == 0)
                {
                    // Draw bottom Tick
                    session.DrawLine(columnCenter, fftLowerBound, columnCenter, fftLowerBound + 5, Colors.Black);
                }
            }
        }
    }
}