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

using TeensyBatExplorer.Core.BatLog;

namespace TeensyBatExplorer.Models.Bins
{
    public class IntBin : Bin<BatCall>
    {
        public IntBin(string label, Func<BatCall, bool> filter) : base(filter)
        {
            Label = label;
        }
    }

    public class UintBin : Bin<BatCall>
    {
        public UintBin(string label, Func<BatCall, bool> filter) : base(filter)
        {
            Label = label;
        }

        public override string ToString()
        {
            return $"{Label}: {Count}";
        }
    }
}