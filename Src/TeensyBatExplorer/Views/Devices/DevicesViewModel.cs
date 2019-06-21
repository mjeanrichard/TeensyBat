// 
// Teensy Bat Explorer - Copyright(C) 2018 Meinard Jean-Richard
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
using System.Text;
using System.Threading.Tasks;

using Microsoft.Toolkit.Uwp.Helpers;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using TeensyBatExplorer.Helpers.DependencyInjection;
using TeensyBatExplorer.Helpers.ViewModels;
using TeensyBatExplorer.Models;

namespace TeensyBatExplorer.Views.Devices
{
    public class DevicesViewModel : BaseViewModel
    {
        private readonly TeensyBatDevice _teensyBatDevice;
        private TeensyBatDevice _teensyBat;

        private byte? _nodeId;

        private string _nodeTime;

        private string _voltage;

        private string _serialLog;

        private bool _isDeviceConnected;

        private string _timeDiff;

        private StringBuilder _logBuilder = new StringBuilder();
        private PlotModel _plotModel;
        private HeatMapSeries _heatMapSeries;

        public DevicesViewModel(TeensyBatDevice teensyBatDevice)
        {
            _teensyBatDevice = teensyBatDevice;
            SetTimeCommand = new AsyncCommand(SetTime, this);
            SetVoltageCommand = new AsyncCommand(SetVoltage, this);
            SetNodeIdCommand = new AsyncCommand(SetNodeId, this);
        }

        public TeensyBatDevice TeensyBat
        {
            get => _teensyBat;
            set => Set(ref _teensyBat, value);
        }

        public RelayCommand SetTimeCommand { get; private set; }
        public RelayCommand SetVoltageCommand { get; private set; }
        public RelayCommand SetNodeIdCommand { get; private set; }

        public byte? NodeId
        {
            get => _nodeId;
            set => Set(ref _nodeId, value);
        }

        public string NodeTime
        {
            get => _nodeTime;
            set => Set(ref _nodeTime, value);
        }

        public string Voltage
        {
            get => _voltage;
            set => Set(ref _voltage, value);
        }

        public bool IsDeviceConnected
        {
            get => _isDeviceConnected;
            set => Set(ref _isDeviceConnected, value);
        }

        public string TimeDiff
        {
            get => _timeDiff;
            set => Set(ref _timeDiff, value);
        }

        public string SerialLog
        {
            get => _serialLog;
            set => Set(ref _serialLog, value);
        }

        private async Task SetNodeId()
        {
            byte? newValue = await DispatcherHelper.ExecuteOnUIThreadAsync(async () => await SetNodeIdDialog.GetNodeId(_teensyBat.NodeId.GetValueOrDefault(0)));
            if (newValue.HasValue)
            {
                await _teensyBat.SetNodeId(newValue.Value);
            }
        }

        private async Task SetVoltage()
        {
            double? newValue = await DispatcherHelper.ExecuteOnUIThreadAsync(async () => await SetVoltageDialog.GetVoltage(_teensyBat.InputVoltage.GetValueOrDefault(0)));
            if (newValue.HasValue)
            {
                await _teensyBat.SetVoltage(newValue.Value);
            }
        }

        private async Task SetTime()
        {
            await _teensyBat.SetTime();
        }

        protected override Task InitializeInternalAsync()
        {
            TeensyBat = _teensyBatDevice;
            TeensyBat.DeviceUpdated += TeensyBatOnDeviceUpdated;
            TeensyBat.SerialReceived += SerialReceived;
            TeensyBat.DataReceived += DataReceived;


            _heatMapSeries = new HeatMapSeries
            {
                X0 = 0,
                X1 = 470,
                Y0 = 0,
                Y1 = 70,
                Data = new double[1,1],
                Interpolate = false,
                //RenderMethod = HeatMapRenderMethod.Bitmap,
            };

            _plotModel = new PlotModel();
            _plotModel.Axes.Add(new LinearColorAxis
            {
                Palette = OxyPalettes.Rainbow(100)
            });
            _plotModel.Series.Add(_heatMapSeries);


            return Task.CompletedTask;
        }

        private void DataReceived(object sender, CallData e)
        {
            double[,] data = new double[e.Length,64];
            int i = 0;
            foreach (byte[] row in e.Data)
            {
                for (int j = 0; j < 64; j++)
                {
                    data[i, j] = row[j];
                }
                i++;
            }

            _heatMapSeries.Data = data;
            _heatMapSeries.X1 = e.Length;
            _plotModel.InvalidatePlot(true);

            OnPropertyChanged(nameof(PlotModel));
        }

        public PlotModel PlotModel
        {
            get { return _plotModel; }
            set { Set(ref _plotModel, value); }
        }

        private void SerialReceived(object sender, string e)
        {
            _logBuilder.Append(e);
            SerialLog = _logBuilder.ToString();
        }

        private void TeensyBatOnDeviceUpdated(object sender, EventArgs e)
        {
            NodeId = TeensyBat.NodeId;
            NodeTime = TeensyBat.NodeTime.HasValue ? TeensyBat.NodeTime.Value.ToString("dd.MM.yyyy HH:mm:ss") : "--.--.-- --:--:--";

            if (TeensyBat.UpdateTime.HasValue && TeensyBat.NodeTime.HasValue)
            {
                TimeDiff = (TeensyBat.NodeTime.Value - TeensyBat.UpdateTime.Value).ToString("hh\\:mm\\:ss\\.fff");
            }
            Voltage = TeensyBat.InputVoltage != null ? $"{Math.Round(TeensyBat.InputVoltage.Value, 1)} V" : "-- V";

            IsDeviceConnected = TeensyBat.IsDeviceConnected;
        }
    }
}