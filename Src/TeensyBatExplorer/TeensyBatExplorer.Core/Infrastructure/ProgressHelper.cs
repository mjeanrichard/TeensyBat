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

namespace TeensyBatExplorer.Core.Infrastructure
{
    public static class ProgressHelper
    {
        public static void Report(this IProgress<CountProgress> progress, string message, int current, int total)
        {
            progress.Report(new CountProgress { Current = current, Total = total, Text = message });
        }

        public static void Report(this IProgress<CountProgress> progress, int current, int total)
        {
            progress.Report(new CountProgress { Current = current, Total = total });
        }

        public static StackableProgress Stack(this StackableProgress progress, int progressSpan)
        {
            return new(progress, progressSpan);
        }
    }
}