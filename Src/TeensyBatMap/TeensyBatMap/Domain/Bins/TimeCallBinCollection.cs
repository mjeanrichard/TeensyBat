using System;
using System.Globalization;

namespace TeensyBatMap.Domain.Bins
{
    public class TimeCallBinCollection : IntBinCollection
    {
        private readonly DateTime _baseTime;

        public TimeCallBinCollection(int binCount, DateTime baseTime, Func<BatCall, bool> filter)
            : base(binCount, c => c.StartTimeMs, filter)
        {
            _baseTime = baseTime;
        }

        protected override IntBin CreateBin(int binNumber)
        {
            string label = _baseTime.AddMilliseconds(binNumber * BinSize).ToString("t", CultureInfo.CurrentCulture);
            return new IntBin(label, Filter);
        }
    }
}