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

using TeensyBatExplorer.Core.BatLog.Raw;

using UniversalMapControl.Interfaces;
using UniversalMapControl.Projections;

namespace TeensyBatExplorer.Core.BatLog
{
    public class BatNodeData
    {
        public BatNodeData()
        {
            Calls = new List<BatCall>();
            Infos = new List<InfoData>();
        }

        public DateTime LogStart { get; set; }

        public List<BatCall> Calls { get; private set; }

        public List<InfoData> Infos { get; }

        public uint ProjectOffset { get; set; }

        public uint Duration
        {
            get
            {
                if (Calls.Count == 0)
                {
                    return 0;
                }
                return Calls[Calls.Count - 1].EndTimeMs;
            }
        }
    }
}