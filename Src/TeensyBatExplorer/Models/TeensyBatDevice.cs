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
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Storage.Streams;

using TeensyBatExplorer.Services;

namespace TeensyBatExplorer.Models
{
    public class TeensyBatDevice
    {
        private const byte CMD_SET_NODEID = 1;
        private const byte CMD_SET_TIME = 2;
        private const byte CMD_SET_VOLTAGE = 3;

        private const byte DATA_DEVICE_INFO = 1;

        private readonly HidDeviceHandler _teensyBatDeviceHandler;
        private readonly HidDeviceHandler _serialEmulatorDeviceHandler;

        public TeensyBatDevice()
        {
            _teensyBatDeviceHandler = new HidDeviceHandler(0xFFAB, 0x0200);
            _teensyBatDeviceHandler.DeviceConnected += OnTeensyBatDeviceConnected;
            _teensyBatDeviceHandler.DeviceClosing += OnTeensyBatDeviceClosing;

            _serialEmulatorDeviceHandler = new HidDeviceHandler(0xFFC9, 0x4);
            _serialEmulatorDeviceHandler.DeviceConnected += OnSerEmuDeviceConnected;
            _serialEmulatorDeviceHandler.DeviceClosing += OnSerEmuDeviceClosing;
        }

        public bool IsDeviceConnected { get; private set; }
        public bool IsSerialConnected { get; private set; }

        public byte? NodeId { get; private set; }
        public DateTimeOffset? NodeTime { get; private set; }
        public DateTimeOffset? UpdateTime { get; private set; }
        public double? InputVoltage { get; private set; }

        public event EventHandler DeviceUpdated;
        public event EventHandler SerialUpdated;
        public event EventHandler<string> SerialReceived;

        private void OnTeensyBatDeviceClosing(HidDeviceHandler sender, DeviceInformation args)
        {
            sender.Device.InputReportReceived -= InputReceived;
            IsDeviceConnected = false;
            NodeId = null;
            NodeTime = null;
            InputVoltage = null;
            UpdateTime = null;
            DeviceUpdated?.Invoke(this, null);
        }

        private void OnTeensyBatDeviceConnected(HidDeviceHandler sender, OnDeviceConnectedEventArgs args)
        {
            sender.Device.InputReportReceived += InputReceived;
            IsDeviceConnected = true;
        }

        private void OnSerEmuDeviceClosing(HidDeviceHandler sender, DeviceInformation args)
        {
            sender.Device.InputReportReceived -= SerialInputReceived;
            IsSerialConnected = false;
            SerialUpdated?.Invoke(this, null);
        }

        private void OnSerEmuDeviceConnected(HidDeviceHandler sender, OnDeviceConnectedEventArgs args)
        {
            sender.Device.InputReportReceived += SerialInputReceived;
            IsSerialConnected = true;
        }

        private async Task SendData(Command command)
        {
            HidOutputReport outputReport = _teensyBatDeviceHandler.Device.CreateOutputReport(0);
            outputReport.Data = command.ToBuffer();
            uint bytesWritten = await _teensyBatDeviceHandler.Device.SendOutputReportAsync(outputReport);
        }

        private void SerialInputReceived(HidDevice device, HidInputReportReceivedEventArgs args)
        {
            DataReader reader = DataReader.FromBuffer(args.Report.Data);
            reader.ByteOrder = ByteOrder.BigEndian;
            // This is the ReportId, should always be 0, but we don't care...
            byte firstByte = reader.ReadByte();
            if (firstByte != 0)
            {
                return;
            }

            byte lastByte = reader.ReadByte();
            StringBuilder sb = new StringBuilder();
            while (lastByte != 0)
            {
                sb.Append((char)lastByte);
                lastByte = reader.ReadByte();
            }
            SerialReceived?.Invoke(this, sb.ToString());
        }

        private void InputReceived(HidDevice device, HidInputReportReceivedEventArgs args)
        {
            DateTimeOffset updateTime = DateTimeOffset.UtcNow;
            DataReader reader = DataReader.FromBuffer(args.Report.Data);
            reader.ByteOrder = ByteOrder.BigEndian;

            // This is the ReportId, should always be 0, but we don't care...
            byte firstByte = reader.ReadByte();
            if (firstByte != 0)
            {
                return;
            }

            byte dataType = reader.ReadByte();
            switch (dataType)
            {
                case DATA_DEVICE_INFO:
                    NodeId = reader.ReadByte();
                    DateTimeOffset time = DateTimeOffset.FromUnixTimeSeconds(reader.ReadUInt32());
                    ushort ms = reader.ReadUInt16();
                    NodeTime = time.AddMilliseconds(ms);
                    UpdateTime = updateTime;
                    InputVoltage = reader.ReadUInt16() / 1000d;
                    break;
            }
            DeviceUpdated?.Invoke(this, null);
        }

        public async Task SetTime()
        {
            Command cmd = new Command(CMD_SET_TIME);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            long unixTimeStamp = now.ToUnixTimeSeconds() + 1;
            ushort offset = (ushort)(1000 - now.Millisecond);

            if (offset < 200)
            {
                unixTimeStamp++;
                offset += 1000;
            }

            cmd.Write((uint)unixTimeStamp);
            cmd.Write(offset);
            await SendData(cmd);
        }

        public async Task SetVoltage(double voltage)
        {
            Command cmd = new Command(CMD_SET_VOLTAGE);
            cmd.Write((ushort)Math.Round(voltage * 1000));
            await SendData(cmd);
        }

        public async Task SetNodeId(byte nodeId)
        {
            Command cmd = new Command(CMD_SET_NODEID);
            cmd.Write(nodeId);
            await SendData(cmd);
        }

        private class Command
        {
            private readonly byte[] _buffer;
            private byte _index;

            public Command(byte command)
            {
                _buffer = new byte[65];
                _buffer[0] = 0;
                _buffer[1] = command;
                _index = 2;
            }

            public void Write(byte data)
            {
                _buffer[_index++] = data;
            }

            public void Write(ushort data)
            {
                WriteBigEndian(BitConverter.GetBytes(data));
            }

            private void WriteBigEndian(byte[] data)
            {
                if (BitConverter.IsLittleEndian)
                {
                    for (int i = data.Length - 1; i >= 0; i--)
                    {
                        _buffer[_index++] = data[i];
                    }
                }
                else
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        _buffer[_index++] = data[i];
                    }
                }
            }

            public void Write(uint data)
            {
                WriteBigEndian(BitConverter.GetBytes(data));
            }

            public IBuffer ToBuffer()
            {
                return _buffer.AsBuffer();
            }
        }
    }
}