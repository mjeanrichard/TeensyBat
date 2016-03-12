using System;
using System.Globalization;

namespace TeensyBatMap.Domain.Bins
{
    public class TimeCallBinCollection : UintBinCollection
    {
        private readonly DateTime _baseTime;

        public TimeCallBinCollection(int binCount, DateTime baseTime, Func<BatCall, bool> filter)
            : base(binCount, c => c.StartTimeMs, filter)
        {
            _baseTime = baseTime;
        }

        protected override UintBin CreateBin(uint binNumber)
        {
            string label = _baseTime.AddMilliseconds(binNumber * BinSize).ToString("t", CultureInfo.CurrentCulture);
            return new UintBin(label, Filter);
        }
    }
}