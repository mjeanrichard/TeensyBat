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
using System.Threading.Tasks;

using MaterialDesignThemes.Wpf;

using Nito.Mvvm;

using TeensyBatExplorer.Core.Devices;
using TeensyBatExplorer.WPF.Controls;
using TeensyBatExplorer.WPF.Infrastructure;

namespace TeensyBatExplorer.WPF.Views.Device
{
    public class DeviceViewModel : BaseViewModel, IConsoleTextProvider
    {
        private readonly Func<TeensyDeviceManager> _deviceMangerFactory;
        private TeensyDeviceManager? _teensyDeviceManger;
        private StrongCanExecuteChanged? _canExecuteChangedRefresh;


        public DeviceViewModel(Func<TeensyDeviceManager> deviceMangerFactory)
        {
            _deviceMangerFactory = deviceMangerFactory;

            IAsyncCommand cmd = new CustomAsyncCommand(RefreshDevice, () => _teensyDeviceManger?.TeensyBatDevice.IsConnected ?? false, s =>
            {
                _canExecuteChangedRefresh = new StrongCanExecuteChanged(s);
                return _canExecuteChangedRefresh;
            });

            AddToolbarButton(new ToolBarButton(cmd, PackIconKind.Refresh, "Refresh"));

            UpdateTimeCommand = new AsyncCommand(SetTime);
            OpenSetVoltageCommand = new AsyncCommand(OpenSetVoltage);
            OpenSetNodeNumberCommand = new AsyncCommand(OpenSetNodeNumber);

            Title = "Geräte konfigurieren";
        }

        public AsyncCommand OpenSetNodeNumberCommand { get; set; }
        public AsyncCommand OpenSetVoltageCommand { get; set; }
        public AsyncCommand UpdateTimeCommand { get; }

        public bool IsConnected => _teensyDeviceManger?.TeensyBatDevice.IsConnected ?? false;
        public string? NodeNumber => _teensyDeviceManger?.TeensyBatDevice.NodeId?.ToString();
        public string CpuTemp => $"{_teensyDeviceManger?.TeensyBatDevice.CpuTemp:0.00} °C";
        public string? FirmwareVersion => _teensyDeviceManger?.TeensyBatDevice.FirmwareVersion?.ToString();
        public string? HardwareVersion => _teensyDeviceManger?.TeensyBatDevice.HardwareVersion?.ToString();
        public string InputVoltage => $"{_teensyDeviceManger?.TeensyBatDevice.InputVoltage} V";
        public string DeviceTime => $"{_teensyDeviceManger?.TeensyBatDevice.NodeTime:dd.MM.yyyy HH:mm:ss}";
        public string TimeErrorPpm => $"{_teensyDeviceManger?.TeensyBatDevice.TimeErrorPpm:0.0} ppm";

        public string TimeDiff
        {
            get
            {
                TeensyBatDevice? device = _teensyDeviceManger?.TeensyBatDevice;
                if (device?.UpdateTime != null && device.NodeTime.HasValue)
                {
                    return (device.NodeTime.Value - device.UpdateTime.Value).ToString("hh\\:mm\\:ss\\.fff");
                }

                return "";
            }
        }

        private async Task OpenSetVoltage()
        {
            if (_teensyDeviceManger == null)
            {
                return;
            }

            SetVoltageViewModel vm = new(_teensyDeviceManger, this);
            await vm.Open();
        }

        private async Task OpenSetNodeNumber()
        {
            if (_teensyDeviceManger == null)
            {
                return;
            }

            SetNodeIdViewModel vm = new(_teensyDeviceManger, this);
            await vm.Open();
        }


        private async Task RefreshDevice()
        {
            if (_teensyDeviceManger == null)
            {
                return;
            }

            if (_teensyDeviceManger.TeensyBatDevice.IsConnected)
            {
                using (BusyState busy = BeginBusy("Lade Daten vom Gerät..."))
                {
                    await _teensyDeviceManger.TeensyBatDevice.Refresh(busy.Token);
                }
            }
        }

        private async Task SetTime()
        {
            if (_teensyDeviceManger == null)
            {
                return;
            }

            if (_teensyDeviceManger.TeensyBatDevice.IsConnected)
            {
                using (BusyState busy = BeginBusy("Aktualisiere die Zeit auf dem Gerät..."))
                {
                    await _teensyDeviceManger.TeensyBatDevice.SetTime(busy.Token);
                }
            }
        }

        public override Task Initialize()
        {
            _teensyDeviceManger = _deviceMangerFactory();
            _teensyDeviceManger.StartListening();
            _teensyDeviceManger.SerialReceived += OnLineAvailable;
            _teensyDeviceManger.TeensyBatDevice.DeviceUpdated += OnDeviceUpdated;
            return Task.CompletedTask;
        }

        private void OnDeviceUpdated(object? sender, EventArgs e)
        {
            RunOnUiThread(() =>
            {
                _canExecuteChangedRefresh?.OnCanExecuteChanged();
                OnPropertyChanged(nameof(NodeNumber));
                OnPropertyChanged(nameof(CpuTemp));
                OnPropertyChanged(nameof(FirmwareVersion));
                OnPropertyChanged(nameof(HardwareVersion));
                OnPropertyChanged(nameof(InputVoltage));
                OnPropertyChanged(nameof(DeviceTime));
                OnPropertyChanged(nameof(TimeDiff));
                OnPropertyChanged(nameof(TimeErrorPpm));
                OnPropertyChanged(nameof(IsConnected));
            });
        }

        public event EventHandler<string>? LineAvailable;

        protected void OnLineAvailable(object? sender, string e)
        {
            RunOnUiThread(() => LineAvailable?.Invoke(this, e));
        }
    }
}