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

using System.ComponentModel;
using System.Runtime.CompilerServices;

using TeensyBatExplorer.Annotations;
using TeensyBatExplorer.Business.Models;

namespace TeensyBatExplorer.Views.AddFiles
{
    public class BatLogViewModel : INotifyPropertyChanged
    {
        private bool _selected;

        public BatLogViewModel(BatLog batLog)
        {
            Log = batLog;
        }

        public string Node => Log.NodeNumber.ToString();
        public string Datum => Log.StartTime.ToString("dd.MM.yy hh:mm:ss");
        public string CallCount => Log.Calls.Count.ToString();

        public bool Selected
        {
            get => _selected;
            set
            {
                if (value == _selected) return;
                _selected = value;
                OnPropertyChanged();
            }
        }

        public BatLog Log { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}