using System;
using System.Collections.Generic;
using WinRtLib;

namespace TeensyBatMap.Domain.Bins
{
    public class UintBinCollection : BinCollectionBase<UintBin, BatCall>
    {
        private Range<uint> _range;
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

        public UintBinCollection(int binCount, Func<BatCall, uint> valueAccessor, Func<BatCall, bool> filter) : base(binCount, filter)
        {
            ValueAccessor = valueAccessor;
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

            ActualBinCount = Math.Min(MaxBinCount, (int)Math.Ceiling(range / (double)BinSize)+1);

            Range = new Range<uint>(minValue, maxValue);
            LoadBinsInternal(calls);
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