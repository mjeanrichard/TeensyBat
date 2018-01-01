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
using System.Globalization;

using TeensyBatExplorer.Core.BatLog;

namespace TeensyBatExplorer.Models.Bins
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
            string label = _baseTime.AddMilliseconds(binNumber * BinSize).ToString("HH:mm", CultureInfo.CurrentCulture);
            return new UintBin(label, Filter);
        }
    }
}