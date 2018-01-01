// 
// Teensy Bat Explorer - Copyright(C) 2017 Meinard Jean-Richard
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
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

using Windows.Storage;

using TeensyBatExplorer.Core.BatLog.Raw;

namespace TeensyBatExplorer.Core.BatLog
{
    public class BatNodeLogReader
    {
        public async Task<RawNodeData> Load(IStorageFile file)
        {
            RawNodeData log = new RawNodeData();
            log.LogStart = DateTime.UtcNow;
            using (Stream logStream = await file.OpenStreamForReadAsync())
            {
                using (BinaryReader reader = new BinaryReader(logStream))
                {
                    ReadData(log, reader);
                }
            }
            return log;
        }

        private void ReadData(RawNodeData log, BinaryReader reader)
        {
            RecordTypes recordType = GetNextRecordType(reader);
            if (recordType != RecordTypes.Header)
            {
                throw new InvalidOperationException("No Header found in Log File!");
            }
            ReadHeader(log, reader);

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                recordType = GetNextRecordType(reader);
                switch (recordType)
                {
                    case RecordTypes.None:
                        break;
                    case RecordTypes.Call:
                        if (log.Verison == 1)
                        {
                            ReadCallRecordV1(log, reader);
                        }
                        else
                        {
                            ReadCallRecordV2(log, reader);
                        }
                        break;
                    case RecordTypes.Info:
                        ReadInfoRecord(log, reader);
                        break;
                    case RecordTypes.Header:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void ReadCallRecordV1(RawNodeData log, BinaryReader reader)
        {
            RawCall rawCall = new RawCall();

            rawCall.Duration = reader.ReadUInt32();
            rawCall.StartTimeMs = reader.ReadUInt32();
            rawCall.ClippedSamples = reader.ReadUInt16();

            // Ignore Max Power
            reader.ReadUInt16();

            rawCall.MissedSamples = reader.ReadUInt16();
            rawCall.FftData = ReadUInt16Array(reader, 256);

            log.Calls.Add(rawCall);
        }

        private void ReadCallRecordV2(RawNodeData log, BinaryReader reader)
        {
            RawCall rawCall = new RawCall();

            rawCall.Duration = reader.ReadUInt32();
            rawCall.StartTimeMs = reader.ReadUInt32();
            rawCall.ClippedSamples = reader.ReadUInt16();
            rawCall.MissedSamples = reader.ReadUInt16();

            int powerDataLength = reader.ReadUInt16();
            rawCall.PowerData = reader.ReadBytes(powerDataLength);
            rawCall.FftData = ReadUInt16Array(reader, 256);

            log.Calls.Add(rawCall);
        }

        private ushort[] ReadUInt16Array(BinaryReader reader, int arrayLength)
        {
            ushort[] result = new ushort[arrayLength];
            for (int i = 0; i < arrayLength; i++)
            {
                result[i] = reader.ReadUInt16();
            }
            return result;
        }

        private void ReadInfoRecord(RawNodeData log, BinaryReader reader)
        {
            InfoData infoData = new InfoData();

            infoData.Time = CreateDate(reader.ReadUInt32());
            infoData.TimeMs = reader.ReadUInt32();
            infoData.BatteryVoltage = reader.ReadUInt16();
            infoData.SampleDuration = reader.ReadUInt16();
            log.Infos.Add(infoData);
        }

        private RecordTypes GetNextRecordType(BinaryReader reader)
        {
            if (reader.BaseStream.Position + 2 >= reader.BaseStream.Length)
            {
                return RecordTypes.None;
            }
            byte[] recordMarker = reader.ReadBytes(2);
            RecordTypes recordType = GetRecordType(recordMarker);
            if (recordType == RecordTypes.None)
            {
                //skip until next recognized marker
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    recordMarker[0] = recordMarker[1];
                    recordMarker[1] = reader.ReadByte();
                    recordType = GetRecordType(recordMarker);
                    if (recordType != RecordTypes.None)
                    {
                        break;
                    }
                }
            }
            return recordType;
        }

        private RecordTypes GetRecordType(byte[] marker)
        {
            if (marker.Length != 2)
            {
                return RecordTypes.None;
            }
            if (marker[0] == 255)
            {
                switch (marker[1])
                {
                    case 255:
                        return RecordTypes.Call;
                    case 244:
                        return RecordTypes.Info;
                    default:
                        return RecordTypes.None;
                }
            }
            // TB in ASCII is 84, 66
            if (marker[0] == 84 && marker[1] == 66)
            {
                return RecordTypes.Header;
            }
            return RecordTypes.None;
        }

        private void ReadHeader(RawNodeData log, BinaryReader reader)
        {
            byte[] marker = reader.ReadBytes(2);
            if (marker[0] != 76) //  76 -> L
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Ungültiger Marker ({0}).", marker[0]));
            }
            log.Verison = marker[1];
            log.NodeId = reader.ReadByte();
            uint seconds = reader.ReadUInt32();
            DateTime startTime = CreateDate(seconds);
            log.LogStart = startTime;
        }

        private DateTime CreateDate(uint time)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local).AddSeconds(time);
        }
    }
}