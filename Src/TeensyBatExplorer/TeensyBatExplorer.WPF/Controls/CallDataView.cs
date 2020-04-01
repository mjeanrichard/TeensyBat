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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using TeensyBatExplorer.Core.Models;

namespace TeensyBatExplorer.WPF.Controls
{
    [TemplatePart(Name = "PART_Image", Type = typeof(Image))]
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

        private const int RowHeight = 4;
        private const int ColWidth = 4;

        private const int FftLowerBound = 128 * ColWidth;
        private const int LoudnessLowerBound = FftLowerBound + 200;

        private WriteableBitmap _canvas;
        private Image _imageControl;

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
                    _imageControl.Source = _canvas;
                    Invalidate();
                }
            }
        }

        private void Invalidate()
        {
            Draw();
        }

        private void Draw()
        {
            if (_imageControl == null || BatCall == null)
            {
                return;
            }

            using (_canvas.GetBitmapContext())
            {
                _canvas.Clear();

                IList<FftBlock> fftData = BatCall.FftData;
                for (int iCol = 0; iCol < fftData.Count; iCol++)
                {
                    int columnCenter = iCol * ColWidth;

                    FftBlock fftBlock = fftData[iCol];
                    for (int i = 0; i < fftBlock.Data.Length; i++)
                    {
                        Color pixelColor = ColorPalettes.BlueVioletRed[Math.Min(127, (int)fftBlock.Data[i])];

                        int x = columnCenter - ColWidth / 2;
                        int y = FftLowerBound - i * RowHeight;
                        _canvas.FillRectangle(x, y, x+ColWidth, y+RowHeight, pixelColor);
                    }

                    if (iCol < fftData.Count - 1)
                    {
                        int scaledLoudness = fftData[iCol + 1].Loudness / 21;
                        _canvas.DrawLine(columnCenter, LoudnessLowerBound - fftBlock.Loudness / 21, columnCenter + ColWidth, LoudnessLowerBound - scaledLoudness, Colors.Black);
                    }

                    if (iCol % 2 == 0)
                    {
                        // Draw bottom Tick
                        _canvas.DrawLine(columnCenter, FftLowerBound, columnCenter, FftLowerBound + 5, Colors.Black);
                    }
                }

                float width = 1f;
                for (int i = 0; i < 10; i++)
                {
                    int y = FftLowerBound - i * 10 * RowHeight;
                    _canvas.DrawLine(0, y, fftData.Count * ColWidth, y, Colors.White);
                }

                _canvas.DrawLine(0, LoudnessLowerBound - 2400 / 21, fftData.Count * ColWidth, LoudnessLowerBound - 2400 / 21, Colors.Green);
            }
        }
    }
}