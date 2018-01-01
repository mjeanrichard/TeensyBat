// 
// Teensy Bat Explorer - Copyright(C) 2017 Meinard Jean-Richard
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

using TeensyBatExplorer.Core.BatLog;

namespace TeensyBatExplorer.Models.Bins
{
    public class UintBinCollection : BinCollectionBase<UintBin, BatCall>
    {
        private Range<uint> _range;

        public UintBinCollection(int binCount, Func<BatCall, uint> valueAccessor, Func<BatCall, bool> filter) : base(binCount, filter)
        {
            ValueAccessor = valueAccessor;
        }

        protected Func<BatCall, uint> ValueAccessor { get; private set; }
        protected uint BinSize { get; set; }

        public Range<uint> Range
        {
            get { return _range; }
            protected set
            {
                _range = value;
                OnPropertyChanged();
            }
        }

        public void LoadBins(IEnumerable<BatCall> calls)
        {
            uint minValue = uint.MaxValue;
            uint maxValue = uint.MinValue;
            foreach (BatCall call in calls)
            {
                uint value = ValueAccessor(call);
                if (minValue > value)
                {
                    minValue = value;
                }
                if (maxValue < value)
                {
                    maxValue = value;
                }
            }
            uint range = maxValue - minValue;
            BinSize = Math.Max((uint)Math.Ceiling(range / (double)MaxBinCount), 1);

            ActualBinCount = Math.Min(MaxBinCount, (int)Math.Ceiling(range / (double)BinSize) + 1);

            Range = new Range<uint>(minValue, maxValue);
            LoadBinsInternal(calls);
        }

        public UintBin GetBin(uint millisecond)
        {
            uint index = (millisecond - Range.Minimum) / BinSize;
            return GetBinByIndex(index);
        }

        protected override UintBin CreateBin(uint binNumber)
        {
            string label = (Range.Minimum + binNumber * BinSize).ToString();
            return new UintBin(label, Filter);
        }

        protected override uint GetBinNumber(BatCall element)
        {
            return (ValueAccessor(element) - Range.Minimum) / BinSize;
        }
    }
}