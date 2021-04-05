// 
// Teensy Bat Explorer - Copyright(C) 2021 Meinrad Jean-Richard
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
using System.Windows;
using System.Windows.Media;

namespace TeensyBatExplorer.WPF.Controls
{
    public class GlyphText
    {
        private readonly GlyphTypeface _glyphTypeface;

        private string _text = string.Empty;
        private double _size = 20;
        private bool _isDirty = true;
        private GlyphRun? _glyphRun;
        private float _pixelsPerDip = 1;
        private double _totalWidth;

        public GlyphText(GlyphTypeface glyphTypeface)
        {
            _glyphTypeface = glyphTypeface;
        }

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    _isDirty = true;
                }
            }
        }

        public double Size
        {
            get => _size;
            set
            {
                if (Math.Abs(_size - value) > 0.01)
                {
                    _size = value;
                    _isDirty = true;
                }
            }
        }

        public double Height => _pixelsPerDip * Size;
        public double Width => _totalWidth;

        private void UpdateGlyphRun()
        {
            ushort[] glyphIndexes = new ushort[_text.Length];
            double[] advanceWidths = new double[_text.Length];

            _totalWidth = 0;

            for (int n = 0; n < _text.Length; n++)
            {
                ushort glyphIndex = _glyphTypeface.CharacterToGlyphMap[_text[n]];
                glyphIndexes[n] = glyphIndex;

                double width = _glyphTypeface.AdvanceWidths[glyphIndex] * _size;
                advanceWidths[n] = width;

                _totalWidth += width;
            }

            _glyphRun = new GlyphRun(_glyphTypeface, 0, false, _size, _pixelsPerDip, glyphIndexes, new Point(), advanceWidths, null, null, null, null, null, null);
            _isDirty = false;
        }

        public GlyphRun GetGlyphRun(Visual visual)
        {
            float pixelsPerDip = (float)VisualTreeHelper.GetDpi(visual).PixelsPerDip;
            if (_isDirty || Math.Abs(pixelsPerDip - _pixelsPerDip) > 0.001)
            {
                _pixelsPerDip = pixelsPerDip;
                UpdateGlyphRun();
            }

            if (_glyphRun == null)
            {
                throw new InvalidOperationException();
            }

            return _glyphRun;
        }
    }
}