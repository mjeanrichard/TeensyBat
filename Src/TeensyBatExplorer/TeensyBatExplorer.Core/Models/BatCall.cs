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

        public int NodeId { get; set; }
        public BatNode? Node { get; set; }

        public DateTime StartTime { get; set; }
        public long StartTimeMicros { get; set; }

        public long DurationMicros { get; set; }
        public int PeakFrequency { get; set; }
        public bool IsBat { get; set; }

        public IList<BatDataFileEntry> Entries { get; set; } = new List<BatDataFileEntry>();
    }
}