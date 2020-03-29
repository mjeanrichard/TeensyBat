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

namespace TeensyBatExplorer.Core.Models
{
    public class BatCall
    {
        public int Id { get; set; }
        public int FftCount { get; set; }
        public long StartTimeMS { get; set; }
        public double MaxPeakFrequency { get; set; }
        public double AvgPeakFrequency { get; set; }
        public DateTime StartTime { get; set; }
        public bool IsBat { get; set; }

        public int HighFreqSampleCount { get; set; }
        public int HighPowerSampleCount { get; set; }
        public int MaxLevel { get; set; }

        public IList<FftBlock> FftData { get; set; } = new List<FftBlock>();

        public BatNode Node { get; set; }
        public BatLog Log { get; set; }
    }
}