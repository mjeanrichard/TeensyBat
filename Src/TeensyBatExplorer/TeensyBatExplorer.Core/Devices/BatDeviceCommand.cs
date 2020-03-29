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

namespace TeensyBatExplorer.Core.Devices
{
    internal class BatDeviceCommand
    {
        private byte _index;

        public BatDeviceCommand(byte command)
        {
            Buffer = new byte[64];
            Buffer[0] = command;
            _index = 1;
        }

        public byte[] Buffer { get; }

        public void Write(byte data)
        {
            Buffer[_index++] = data;
        }

        public void Write(ushort data)
        {
            WriteBigEndian(BitConverter.GetBytes(data));
        }

        private void WriteBigEndian(byte[] data)
        {
            if (BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    Buffer[_index++] = data[i];
                }
            }
            else
            {
                for (int i = data.Length - 1; i >= 0; i--)
                {
                    Buffer[_index++] = data[i];
                }
            }
        }

        public void Write(uint data)
        {
            WriteBigEndian(BitConverter.GetBytes(data));
        }
    }
}