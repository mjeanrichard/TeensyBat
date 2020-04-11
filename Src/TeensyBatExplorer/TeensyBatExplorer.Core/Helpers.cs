// 
// Teensy Bat Explorer - Copyright(C)  Meinrad Jean-Richard
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

namespace TeensyBatExplorer.Core
{
    public static class Helpers
    {
        public static DateTime AddMicros(this DateTime dateTime, long microseconds)
        {
            return dateTime.AddTicks(microseconds * 10);
        }
        
        public static string ToFormattedString(this DateTime dateTime)
        {
            return dateTime.ToString("dd.MM.yy HH:mm:ss");
        }
    }
}