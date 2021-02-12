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

using System.Threading.Tasks;

using Nito.Mvvm;

using TeensyBatExplorer.Core.Devices;
using TeensyBatExplorer.WPF.Infrastructure;
using TeensyBatExplorer.WPF.Themes;

namespace TeensyBatExplorer.WPF.Views.Device
{
    public class SetNodeIdViewModel : DialogViewModel<object?>
    {
        private readonly TeensyDeviceManager? _teensyDeviceManger;
        private string? _number;

        public SetNodeIdViewModel(TeensyDeviceManager teensyDeviceManger, BaseViewModel ownerViewModel) : base(ownerViewModel)
        {
            _teensyDeviceManger = teensyDeviceManger;
            SetVoltageCommand = new AsyncCommand(SetVoltage);
            Number = (_teensyDeviceManger?.TeensyBatDevice?.NodeId ?? 0).ToString();
        }

        public AsyncCommand SetVoltageCommand { get; }

        public string? Number
        {
            get => _number;
            set
            {
                if (value != _number)
                {
                    _number = value;
                    OnPropertyChanged();
                }
            }
        }

        private async Task SetVoltage()
        {
            if (_teensyDeviceManger == null || !_teensyDeviceManger.TeensyBatDevice.IsConnected)
            {
                Close(null);
                return;
            }

            using (BusyState busy = BeginBusy("Aktualisiere Gerätenummer..."))
            {
                if (byte.TryParse(Number, out byte number))
                {
                    await _teensyDeviceManger.TeensyBatDevice.SetNodeNumber(number, busy.Token);
                    Close(null);
                }
            }
        }
    }
}