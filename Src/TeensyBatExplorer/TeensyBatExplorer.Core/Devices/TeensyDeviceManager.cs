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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Device.Net;

using Hid.Net.Windows;

namespace TeensyBatExplorer.Core.Devices
{
    public class TeensyDeviceManager : IDisposable
    {
        private const ushort VendorId = 0x16C0;
        private const ushort ProductId = 0x0486;
        private const ushort TeensyBatUsagePage = 0xFFAB;
        private const ushort SerialUsagePage = 0xFFC9;


        private readonly DeviceListener _deviceListener;
        private IDevice _batDevice;
        private IDevice _serialDevice;

        private Task _serialReaderTask;

        public TeensyDeviceManager()
        {
            WindowsHidDeviceFactory.Register(null, new DebugTracer());

            var deviceDefinitions = new List<FilterDeviceDefinition>
            {
                new FilterDeviceDefinition { DeviceType = DeviceType.Hid, VendorId = VendorId, ProductId = ProductId, UsagePage = SerialUsagePage },
                new FilterDeviceDefinition { DeviceType = DeviceType.Hid, VendorId = VendorId, ProductId = ProductId, UsagePage = TeensyBatUsagePage },
            };

            _deviceListener = new DeviceListener(deviceDefinitions, 500) { Logger = new DebugLogger() };
            _deviceListener.DeviceDisconnected += OnDeviceDisconnected;
            _deviceListener.DeviceInitialized += OnDeviceInitialized;
        }

        public TeensyBatDevice TeensyBatDevice { get; } = new TeensyBatDevice();
        public event EventHandler<string> SerialReceived;

        private async Task ReadSerial()
        {
            while (_serialDevice != null && _serialDevice.IsInitialized)
            {
                try
                {
                    ReadResult readResult = await _serialDevice.ReadAsync();
                    if (readResult.BytesRead > 0)
                    {
                        string data = Encoding.ASCII.GetString(readResult.Data);
                        OnSerialReceived(data);
                    }
                    else
                    {
                        await Task.Delay(100);
                    }
                }
                catch (IOException ioex)
                {
                    Debug.WriteLine(ioex.ToString());
                }
            }
        }

        private async void OnDeviceInitialized(object sender, DeviceEventArgs e)
        {
            ushort? usagePage = e.Device?.ConnectedDeviceDefinition?.UsagePage;
            if (usagePage.HasValue)
            {
                if (usagePage.Value == TeensyBatUsagePage)
                {
                    _batDevice = e.Device;
                    await TeensyBatDevice.Connect(_batDevice, CancellationToken.None);
                }
                else if (usagePage.Value == SerialUsagePage)
                {
                    _serialDevice = e.Device;
                    _serialReaderTask = Task.Run(ReadSerial);
                }
            }
        }

        private async void OnDeviceDisconnected(object sender, DeviceEventArgs e)
        {
            if (e.Device == _serialDevice)
            {
                _serialDevice = null;
            }
            else if (e.Device == _batDevice)
            {
                await TeensyBatDevice.Disconnect();
                _batDevice = null;
            }
        }

        public void StartListening()
        {
            _deviceListener.Start();
        }

        public void Dispose()
        {
            _deviceListener.DeviceDisconnected -= OnDeviceDisconnected;
            _deviceListener.DeviceInitialized -= OnDeviceInitialized;
            _deviceListener?.Dispose();
        }

        protected virtual void OnSerialReceived(string e)
        {
            SerialReceived?.Invoke(this, e);
        }
    }
}