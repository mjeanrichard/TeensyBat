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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Device.Net;

namespace TeensyBatExplorer.Core.Devices
{
    public class TeensyBatDevice
    {
        private const byte CMD_SET_NODEID = 1;
        private const byte CMD_SET_TIME = 2;
        private const byte CMD_SET_VOLTAGE = 3;
        private const byte CMD_SET_REFRESH = 4;

        private const byte DATA_DEVICE_INFO = 1;
        private const byte DATA_CALL = 2;

        private const byte DATA_CALL_HEADER = 1;
        private const byte DATA_CALL_DATA1 = 2;
        private const byte DATA_CALL_DATA2 = 3;

        private readonly SemaphoreSlim _readWriteLock = new SemaphoreSlim(1, 1);

        private IDevice _batDevice;

        public byte? NodeId { get; private set; }
        public DateTimeOffset? NodeTime { get; private set; }
        public DateTimeOffset? UpdateTime { get; private set; }
        public DateTimeOffset? LastSetTime { get; set; }
        public double? InputVoltage { get; private set; }
        public int? HardwareVersion { get; private set; }
        public int? FirmwareVersion { get; private set; }
        public double? CpuTemp { get; set; }

        public double? TimeErrorPpm { get; private set; }

        public bool IsConnected { get; set; }

        public event EventHandler DeviceUpdated;

        private void Update(byte[] data)
        {
            DateTimeOffset updateTime = DateTimeOffset.UtcNow;
            using (BinaryReader reader = new BinaryReader(new MemoryStream(data)))
            {
                byte dataType = reader.ReadByte();
                switch (dataType)
                {
                    case DATA_DEVICE_INFO:
                        ReadDeviceInfo(reader, updateTime);
                        break;

                    case DATA_CALL:
                        //ReadCall(reader);
                        break;
                }
            }

            OnDeviceUpdated();
        }

        private void ReadDeviceInfo(BinaryReader reader, DateTimeOffset updateTime)
        {
            FirmwareVersion = reader.ReadByte();
            HardwareVersion = reader.ReadByte();
            NodeId = reader.ReadByte();
            uint timestamps = reader.ReadUInt32();
            DateTimeOffset time = DateTimeOffset.FromUnixTimeSeconds(timestamps);
            ushort ms = reader.ReadUInt16();
            NodeTime = time.AddMilliseconds(ms);
            UpdateTime = updateTime;
            InputVoltage = reader.ReadUInt16() / 1000d;
            CpuTemp = reader.ReadInt16() / 10d;
            LastSetTime = DateTimeOffset.FromUnixTimeSeconds(reader.ReadUInt32());

            TimeSpan offset = updateTime - NodeTime.Value;
            TimeSpan delay = updateTime - LastSetTime.Value;

            TimeErrorPpm = offset.TotalMilliseconds * 1_000_000 / delay.TotalMilliseconds;
        }


        public async Task Connect(IDevice batDevice, CancellationToken cancellationToken)
        {
            if (_batDevice != null)
            {
                await Disconnect();
            }

            _batDevice = batDevice;
            IsConnected = true;
            await Refresh(cancellationToken);
            OnDeviceUpdated();
        }

        public async Task Refresh(CancellationToken cancellationToken)
        {
            BatDeviceCommand cmd = new BatDeviceCommand(CMD_SET_REFRESH);
            await SendCommand(cmd, cancellationToken);
        }

        public async Task SetTime(CancellationToken cancellationToken)
        {
            BatDeviceCommand cmd = new BatDeviceCommand(CMD_SET_TIME);
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
            await SendCommand(cmd, cancellationToken);
        }

        public async Task SetVoltage(decimal voltage, CancellationToken cancellationToken)
        {
            BatDeviceCommand cmd = new BatDeviceCommand(CMD_SET_VOLTAGE);
            cmd.Write((ushort)Math.Round(voltage * 1000));
            await SendCommand(cmd, cancellationToken);
        }

        public async Task SetNodeNumber(byte nodeNumber, CancellationToken cancellationToken)
        {
            BatDeviceCommand cmd = new BatDeviceCommand(CMD_SET_NODEID);
            cmd.Write(nodeNumber);
            await SendCommand(cmd, cancellationToken);
        }

        private async Task SendCommand(BatDeviceCommand command, CancellationToken cancellationToken)
        {
            await _readWriteLock.WaitAsync(cancellationToken);
            try
            {
                await _batDevice.WriteAsync(command.Buffer);
                byte[] answer = await WaitForAnswer(cancellationToken);
                if (answer != null)
                {
                    Update(answer);
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (IOException)
            {
            }
            finally
            {
                _readWriteLock.Release();
            }
        }

        protected virtual void OnDeviceUpdated()
        {
            DeviceUpdated?.Invoke(this, EventArgs.Empty);
        }

        private async Task<byte[]> WaitForAnswer(CancellationToken cancellationToken)
        {
            while (_batDevice != null && _batDevice.IsInitialized)
            {
                ReadResult readResult = await _batDevice.ReadAsync();
                if (readResult.BytesRead > 0)
                {
                    return readResult.Data;
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            return null;
        }

        public async Task Disconnect()
        {
            IsConnected = false;

            _batDevice = null;


            NodeId = null;
            NodeTime = null;
            UpdateTime = null;
            InputVoltage = null;
            HardwareVersion = null;
            FirmwareVersion = null;
            CpuTemp = null;
            LastSetTime = null;

            OnDeviceUpdated();
        }
    }
}