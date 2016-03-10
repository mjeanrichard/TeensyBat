using System;
using System.Collections.Generic;
using WinRtLib;

namespace TeensyBatMap.Domain.Bins
{
    public class IntBinCollection : BinCollectionBase<IntBin, BatCall>
    {
        private Range<int> _range;
        protected Func<BatCall, int> ValueAccessor { get; private set; }
        protected int BinSize { get; set; }

        public Range<int> Range
        {
            get { return _range; }
            protected set
            {
                _range = value;
                OnPropertyChanged();
            }
        }

        public IntBinCollection(int binCount, Func<BatCall, int> valueAccessor, Func<BatCall, bool> filter) : base(binCount, filter)
        {
            ValueAccessor = valueAccessor;
        }

        public void LoadBins(IList<BatCall> calls)
        {
            int minValue = int.MaxValue;
            int maxValue = int.MinValue;
            foreach (BatCall call in calls)
            {
                int value = ValueAccessor(call);
                if (minValue > value)
                {
                    minValue = value;
                }
                if (maxValue < value)
                {
                    maxValue = value;
                }
            }
            int range = maxValue - minValue;
            BinSize = Math.Max((int)Math.Ceiling(range / (double)MaxBinCount), 1);

            ActualBinCount = Math.Min(MaxBinCount, (int)Math.Ceiling(range / (double)BinSize)+1);

            Range = new Range<int>(minValue, maxValue);
            LoadBinsInternal(calls);
        }

        protected override IntBin CreateBin(int binNumber)
        {
            string label = (Range.Minimum + binNumber * BinSize).ToString();
            return new IntBin(label, Filter);
        }

        protected override int GetBinNumber(BatCall element)
        {
            return (ValueAccessor(element) - Range.Minimum) / BinSize;
        }
    }
}