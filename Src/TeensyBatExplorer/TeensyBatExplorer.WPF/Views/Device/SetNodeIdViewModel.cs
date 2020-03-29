using System.Threading.Tasks;

using Nito.Mvvm;

using TeensyBatExplorer.Core.Devices;
using TeensyBatExplorer.WPF.Infrastructure;

namespace TeensyBatExplorer.WPF.Views.Device
{
    public class SetNodeIdViewModel : DialogViewModel<object>
    {
        private readonly TeensyDeviceManager _teensyDeviceManger;
        private string _number;

        public SetNodeIdViewModel(TeensyDeviceManager teensyDeviceManger, BaseViewModel ownerViewModel) : base(ownerViewModel)
        {
            _teensyDeviceManger = teensyDeviceManger;
            SetVoltageCommand = new AsyncCommand(SetVoltage);
            Number = (_teensyDeviceManger?.TeensyBatDevice?.NodeId ?? 0).ToString();
        }

        public AsyncCommand SetVoltageCommand { get; }

        public string Number
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
            if (!_teensyDeviceManger.TeensyBatDevice.IsConnected)
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