//// 
//// Teensy Bat Explorer - Copyright(C) 2018 Meinrad Jean-Richard
////  
//// This program is free software: you can redistribute it and/or modify
//// it under the terms of the GNU General Public License as published by
//// the Free Software Foundation, either version 3 of the License, or
//// (at your option) any later version.
////  
//// This program is distributed in the hope that it will be useful,
//// but WITHOUT ANY WARRANTY; without even the implied warranty of
//// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//// GNU General Public License for more details.
////  
//// You should have received a copy of the GNU General Public License
//// along with this program.  If not, see <http://www.gnu.org/licenses/>.

//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Runtime.InteropServices.WindowsRuntime;
//using System.Text;
//using System.Threading.Tasks;

//using Windows.Devices.Enumeration;
//using Windows.Devices.HumanInterfaceDevice;
//using Windows.Storage.Streams;

//using TeensyBatExplorer.Services;

//namespace TeensyBatExplorer.Business.Models
//{
//    public class TeensyBatDevice
//    {
//        private const byte CMD_SET_NODEID = 1;
//        private const byte CMD_SET_TIME = 2;
//        private const byte CMD_SET_VOLTAGE = 3;

//        private const byte DATA_DEVICE_INFO = 1;
//        private const byte DATA_CALL = 2;

//        private const byte DATA_CALL_HEADER = 1;
//        private const byte DATA_CALL_DATA1 = 2;
//        private const byte DATA_CALL_DATA2 = 3;

//        private readonly HidDeviceHandler _teensyBatDeviceHandler;
//        private readonly HidDeviceHandler _serialEmulatorDeviceHandler;

//        public TeensyBatDevice()
//        {
//            _teensyBatDeviceHandler = new HidDeviceHandler(0xFFAB, 0x0200);
//            _teensyBatDeviceHandler.DeviceConnected += OnTeensyBatDeviceConnected;
//            _teensyBatDeviceHandler.DeviceClosing += OnTeensyBatDeviceClosing;

//            _serialEmulatorDeviceHandler = new HidDeviceHandler(0xFFC9, 0x4);
//            _serialEmulatorDeviceHandler.DeviceConnected += OnSerEmuDeviceConnected;
//            _serialEmulatorDeviceHandler.DeviceClosing += OnSerEmuDeviceClosing;
//        }

//        public bool IsDeviceConnected { get; private set; }
//        public bool IsSerialConnected { get; private set; }

//        public byte? NodeId { get; private set; }
//        public DateTimeOffset? NodeTime { get; private set; }
//        public DateTimeOffset? UpdateTime { get; private set; }
//        public double? InputVoltage { get; private set; }

//        public event EventHandler DeviceUpdated;
//        public event EventHandler SerialUpdated;
//        public event EventHandler<string> SerialReceived;
//        public event EventHandler<CallData> DataReceived;

//        private void OnTeensyBatDeviceClosing(HidDeviceHandler sender, DeviceInformation args)
//        {
//            if (sender.Device != null)
//            {
//                sender.Device.InputReportReceived -= InputReceived;
//            }

//            IsDeviceConnected = false;
//            NodeId = null;
//            NodeTime = null;
//            InputVoltage = null;
//            UpdateTime = null;
//            DeviceUpdated?.Invoke(this, null);
//        }

//        private void OnTeensyBatDeviceConnected(HidDeviceHandler sender, OnDeviceConnectedEventArgs args)
//        {
//            if (sender.Device != null)
//            {
//                sender.Device.InputReportReceived += InputReceived;
//                IsDeviceConnected = true;
//            }
//        }

//        private void OnSerEmuDeviceClosing(HidDeviceHandler sender, DeviceInformation args)
//        {
//            if (sender.Device != null)
//            {
//                sender.Device.InputReportReceived -= SerialInputReceived;
//            }

//            IsSerialConnected = false;
//            SerialUpdated?.Invoke(this, null);
//        }

//        private void OnSerEmuDeviceConnected(HidDeviceHandler sender, OnDeviceConnectedEventArgs args)
//        {
//            if (sender.Device != null)
//            {
//                sender.Device.InputReportReceived += SerialInputReceived;
//                IsSerialConnected = true;
//            }
//        }

//        private async Task SendData(Command command)
//        {
//            HidOutputReport outputReport = _teensyBatDeviceHandler.Device.CreateOutputReport(0);
//            outputReport.Data = command.ToBuffer();
//            uint bytesWritten = await _teensyBatDeviceHandler.Device.SendOutputReportAsync(outputReport);
//        }

//        private void SerialInputReceived(HidDevice device, HidInputReportReceivedEventArgs args)
//        {
//            DataReader reader = DataReader.FromBuffer(args.Report.Data);
//            reader.ByteOrder = ByteOrder.BigEndian;
//            // This is the ReportId, should always be 0, but we don't care...
//            byte firstByte = reader.ReadByte();
//            if (firstByte != 0)
//            {
//                return;
//            }

//            byte lastByte = reader.ReadByte();

//            StringBuilder sb = new StringBuilder();
//            while (lastByte != 0 && reader.UnconsumedBufferLength > 0)
//            {
//                sb.Append((char)lastByte);
//                lastByte = reader.ReadByte();
//            }
//            SerialReceived?.Invoke(this, sb.ToString());
//        }

//        private void InputReceived(HidDevice device, HidInputReportReceivedEventArgs args)
//        {
//            DateTimeOffset updateTime = DateTimeOffset.UtcNow;
//            DataReader reader = DataReader.FromBuffer(args.Report.Data);
//            reader.ByteOrder = ByteOrder.BigEndian;

//            // This is the ReportId, should always be 0, but we don't care...
//            byte firstByte = reader.ReadByte();
//            if (firstByte != 0)
//            {
//                return;
//            }

//            byte dataType = reader.ReadByte();
//            switch (dataType)
//            {
//                case DATA_DEVICE_INFO:
//                    NodeId = reader.ReadByte();
//                    DateTimeOffset time = DateTimeOffset.FromUnixTimeSeconds(reader.ReadUInt32());
//                    ushort ms = reader.ReadUInt16();
//                    NodeTime = time.AddMilliseconds(ms);
//                    UpdateTime = updateTime;
//                    InputVoltage = reader.ReadUInt16() / 1000d;
//                    break;

//                case DATA_CALL:
//                    ReadCall(reader);
//                    break;

//            }
//            DeviceUpdated?.Invoke(this, null);
//        }

//        private CallData _callData = new CallData();

//        private byte[] _tempData = new byte[64];

//        private void ReadCall(DataReader reader)
//        {
//            byte dataPart = reader.ReadByte();
//            switch (dataPart)
//            {
//                case DATA_CALL_HEADER:
//                    if (_callData.LastDataPart != 0)
//                    {
//                        _callData = new CallData();
//                        Debug.WriteLine("Ups 1");
//                        break;
//                    }
//                    _callData.Length = reader.ReadInt16();
//                    _callData.LastDataPart = DATA_CALL_HEADER;
//                    Debug.WriteLine("Length: " + _callData.Length);
//                    break;
//                case DATA_CALL_DATA1:
//                    if (_callData.LastDataPart != DATA_CALL_HEADER && _callData.LastDataPart != DATA_CALL_DATA2)
//                    {
//                        _callData = new CallData();
//                        Debug.WriteLine("Ups 2");
//                        break;
//                    }

//                    short idx = reader.ReadInt16();
//                    short s = reader.ReadInt16();
//                    for (int i = 6; i < 32; i++) // Skip bytes 6 to 32
//                    {
//                        reader.ReadByte();
//                    }

//                    for (int i = 0; i < 32; i++)
//                    {
//                        _tempData[i] = reader.ReadByte();
//                    }

//                    _callData.LastDataPart = DATA_CALL_DATA1;

//                    break;
//                case DATA_CALL_DATA2:
//                    if (_callData.LastDataPart != DATA_CALL_DATA1)
//                    {
//                        _callData = new CallData();
//                        Debug.WriteLine("Ups 3");
//                        break;
//                    }

//                    short idx2 = reader.ReadInt16();
//                    for (int i = 4; i < 32; i++) // Skip bytes 6 to 32
//                    {
//                        reader.ReadByte();
//                    }
//                    for (int i = 32; i < 64; i++)
//                    {
//                        _tempData[i] = reader.ReadByte();
//                    }

//                    _callData.Data.Add(_tempData);
//                    _tempData = new byte[64];

//                    _callData.LastDataPart = DATA_CALL_DATA2;
//                    if (idx2 == _callData.Length - 1)
//                    {
//                        DataReceived?.Invoke(this, _callData);
//                        _callData = new CallData();
//                        Debug.WriteLine("Done!");
//                    }
                    
//                    break;
//            }
//        }

//        public async Task SetTime()
//        {
//            Command cmd = new Command(CMD_SET_TIME);
//            DateTimeOffset now = DateTimeOffset.UtcNow;
//            long unixTimeStamp = now.ToUnixTimeSeconds() + 1;
//            ushort offset = (ushort)(1000 - now.Millisecond);

//            if (offset < 200)
//            {
//                unixTimeStamp++;
//                offset += 1000;
//            }

//            cmd.Write((uint)unixTimeStamp);
//            cmd.Write(offset);
//            await SendData(cmd);
//        }

//        public async Task SetVoltage(double voltage)
//        {
//            Command cmd = new Command(CMD_SET_VOLTAGE);
//            cmd.Write((ushort)Math.Round(voltage * 1000));
//            await SendData(cmd);
//        }

//        public async Task SetNodeId(byte nodeId)
//        {
//            Command cmd = new Command(CMD_SET_NODEID);
//            cmd.Write(nodeId);
//            await SendData(cmd);
//        }

//        private class Command
//        {
//            private readonly byte[] _buffer;
//            private byte _index;

//            public Command(byte command)
//            {
//                _buffer = new byte[65];
//                _buffer[0] = 0;
//                _buffer[1] = command;
//                _index = 2;
//            }

//            public void Write(byte data)
//            {
//                _buffer[_index++] = data;
//            }

//            public void Write(ushort data)
//            {
//                WriteBigEndian(BitConverter.GetBytes(data));
//            }

//            private void WriteBigEndian(byte[] data)
//            {
//                if (BitConverter.IsLittleEndian)
//                {
//                    for (int i = data.Length - 1; i >= 0; i--)
//                    {
//                        _buffer[_index++] = data[i];
//                    }
//                }
//                else
//                {
//                    for (int i = 0; i < data.Length; i++)
//                    {
//                        _buffer[_index++] = data[i];
//                    }
//                }
//            }

//            public void Write(uint data)
//            {
//                WriteBigEndian(BitConverter.GetBytes(data));
//            }

//            public IBuffer ToBuffer()
//            {
//                return _buffer.AsBuffer();
//            }
//        }
//    }
//    public class CallData
//    {
//        public byte LastDataPart { get; set; }
//        public int Length { get; set; }

//        public List<byte[]> Data { get; private set; } = new List<byte[]>();
//    }

//}