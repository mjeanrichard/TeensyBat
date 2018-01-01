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

namespace TeensyBatExplorer.Core.BatLog.Raw
{
    public class RawCall
    {
        public int Number { get; set; }

        public uint StartTimeMs { get; set; }

        public uint EndTimeMs
        {
            get { return StartTimeMs + Duration / 1000; }
        }

        public int ClippedSamples { get; set; }

        public int MissedSamples { get; set; }

        /// <summary>
        /// The Duration of the Call in MircoSeconds.
        /// </summary>
        public uint Duration { get; set; }

        public virtual ushort[] FftData { get; set; }

        public virtual byte[] PowerData { get; set; }
    }
}