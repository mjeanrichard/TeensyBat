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

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using TeensyBatExplorer.Core.Models;
using TeensyBatExplorer.WPF.Annotations;

namespace TeensyBatExplorer.WPF.Controls
{
    public class CallFilter : INotifyPropertyChanged
    {

        private int _minFrequency;

        private int _maxFrequency = 100;

        public int MinFrequency
        {
            get => _minFrequency;
            set
            {
                if (value != _minFrequency)
                {
                    _minFrequency = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FrequencyFilterTitle));
                }
            }
        }

        public int MaxFrequency
        {
            get => _maxFrequency;
            set
            {
                if (value != _maxFrequency)
                {
                    _maxFrequency = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FrequencyFilterTitle));
                }
            }
        }

        public string FrequencyFilterTitle => $"Frequenz ({MinFrequency} kHz - {MaxFrequency} kHz)";

        public IEnumerable<BatCall> Apply(IEnumerable<BatCall> calls)
        {
            return calls.Where(Pass);
        }

        public bool Pass(BatCall call)
        {
            return call.PeakFrequency > MinFrequency && call.PeakFrequency < MaxFrequency;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}