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
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

using TeensyBatExplorer.Core.BatLog;

namespace TeensyBatExplorer.Views.NodeDetail
{
    public class CallModel : INotifyPropertyChanged
    {
        private readonly BatNodeData _data;

        public CallModel(BatCall call, BatNodeData data)
        {
            _data = data;
            Call = call;

            StringBuilder warnings = new StringBuilder();
            if (call.MissedSamples > 0)
            {
                warnings.AppendFormat(CultureInfo.CurrentCulture, "Clipped Samples: {0}\n", call.MissedSamples);
            }
            Warnings = warnings.ToString();

            DateTime startTime = _data.LogStart.AddMilliseconds(call.StartTimeMs);
            Date = $"{startTime:d}";
            Time = $"{startTime:HH:mm:ss.ffff}";
            MainFrequency = $"{call.MainFrequency} kHz";
        }

        public string Warnings { get; set; }
        
        public string MainFrequency { get; set; }

        public string Date { get; }

        public BatCall Call { get; }

        public bool HasWarnings => !string.IsNullOrWhiteSpace(Warnings);

        public string Time { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}