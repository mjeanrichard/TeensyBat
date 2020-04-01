// 
// Teensy Bat Explorer - Copyright(C) 2019 Meinrad Jean-Richard
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TeensyBatExplorer.Core.Models;

namespace TeensyBatExplorer.Core
{
    public class LogReader
    {
        private static Regex FilenamePattern = new Regex(@"TB(?<nn>[0-9]{3})-(?<d>[0-9]{12})-(?<i>[0-9]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private void AddMessage(BatLog log, BatLogMessageLevel level, string message, BinaryReader reader)
        {
            log.LogMessages.Add(new BatLogMessage { Message = message, Level = level, Position = reader.BaseStream.Position });
        }

        public async Task Load(string filename, BatLog log)
        {
            await Task.Run(() => LoadInternal(filename, log));
        }

        public void LoadInternal(string filename, BatLog log)
        {
            using (Stream logStream = File.OpenRead(filename))
            {
                using (BinaryReader reader = new BinaryReader(logStream))
                {
                    ReadData(reader, log);

                    Match filenameMatch = FilenamePattern.Match(Path.GetFileNameWithoutExtension(filename));
                    if (filenameMatch.Success && filenameMatch.Groups["d"].Success)
                    {
                        if (log.StartTime == DateTime.UnixEpoch)
                        {
                            if (DateTime.TryParseExact(filenameMatch.Groups["d"].Value, "yyyyMMddHHmm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime parsedDate))
                            {
                                log.StartTime = parsedDate;
                            }
                        }

                        if (log.NodeNumber == 0 && filenameMatch.Groups["nn"].Success)
                        {
                            if (int.TryParse(filenameMatch.Groups["nn"].Value, out int parsedNodeNumber))
                            {
                                log.NodeNumber = parsedNodeNumber;
                            }
                        }
                    }
                }
            }
        }

        private void ReadFileHeader(BinaryReader reader, BatLog log)
        {
            byte magicOne = reader.ReadByte();
            byte magicTwo = reader.ReadByte();
            if (magicOne != 0xFF || magicTwo != 0xCC)
            {
                AddMessage(log, BatLogMessageLevel.Warning, $"Ungültiger File Marker 0x{magicOne:X2}{magicTwo:X2} (0xFFCC erwartet).", reader);
            }

            log.HardwareVersion = reader.ReadByte();
            log.FirmwareVersion = reader.ReadByte();
            log.NodeNumber = reader.ReadByte();
            log.Debug = reader.ReadBoolean();
            log.PreCallBufferSize = reader.ReadByte();
            log.AfterCallBufferSize = reader.ReadByte();
            log.CallStartThreshold = reader.ReadUInt16();
            log.CallEndThreshold = reader.ReadUInt16();

            reader.SkipBytes(4);

            log.StartTime = reader.ReadDateTimeWithMicroseconds();
            log.OriginalStartTime = log.StartTime;

            reader.SkipBytes(488);
        }

        private void ReadData(BinaryReader reader, BatLog log)
        {
            Stream stream = reader.BaseStream;
            ReadFileHeader(reader, log);
            while (stream.Position < stream.Length)
            {
                byte markOne = reader.ReadByte();
                byte markTwo = reader.ReadByte();

                if (markOne == 0 && markTwo == 0)
                {
                    // Assuming end of file. Rest should be zero
                    return;
                }

                if (markOne != 0xFF || markTwo != 0xFE)
                {
                    throw new LogFileFormatException($"Call marker erwartet (0xFFFE) aber 0x{markOne:X2}{markTwo:X2} gefunden.", reader.BaseStream.Position);
                }

                BatCall call = new BatCall();
                ReadCall(call, reader, log);
                ProcessCall(call, log);
                log.Calls.Add(call);

                //Seek to the next block
                long offset = stream.Position % 512;
                if (offset > 0)
                {
                    stream.Seek(512 - offset, SeekOrigin.Current);
                }
            }
        }

        /// <summary>
        ///    Record (512 bytes):
        ///    |  0-7   |    8-79   |   80-151  |  152-223  |  224-295  |  296-367  |  368-439  |  440-511  |
        ///    | Header | FFT Block | FFT Block | FFT Block | FFT Block | FFT Block | FFT Block | FFT Block |
        ///
        ///    Call Header:
        ///    |  0  |  1  |  2  |  3  |  4  |  5  |  6  |  7  |
        ///    | 255 | 254 | # Blocks  |  0  |  0  |  0  |  0  |
        ///
        ///    Additional Header:
        ///    |  0  |  1  |  2  |  3  |  4  |  5  |  6  |  7  |
        ///    | 255 | 252 |  0  |  0  |  0  |  0  |  0  |  0  |
        /// </summary>
        private void ReadCall(BatCall call, BinaryReader reader, BatLog batLog)
        {
            call.FftCount = reader.ReadUInt16();
            call.StartTimeMS = reader.ReadUInt32();
            call.StartTime = batLog.StartTime.AddMilliseconds(call.StartTimeMS);

            // Error Counters
            batLog.ErrorCountCallBuffFull += reader.ReadByte();
            batLog.ErrorCountPointerBufferFull += reader.ReadByte();
            batLog.ErrorCountDataBufferFull += reader.ReadByte();
            batLog.ErrorCountProcessOverlap += reader.ReadByte();

            call.HighFreqSampleCount = reader.ReadByte();
            call.HighPowerSampleCount = reader.ReadByte();
            call.MaxLevel = reader.ReadByte();
            reader.ReadByte();

            ReadAdditionalData(reader, batLog);
            ReadAdditionalData(reader, batLog);

            for (int i = 0; i < call.FftCount; i++)
            {
                if (i > 0 && i % 3 == 0)
                {
                    //Additional Header
                    byte markOne = reader.ReadByte();
                    byte markTwo = reader.ReadByte();
                    if (markOne != 0xFF || markTwo != 0xFC)
                    {
                        throw new LogFileFormatException($"Ungültiger Marker 0x{markOne:X2}{markTwo:X2} (Additional Header erwartet: 0xFFFC).", reader.BaseStream.Position);
                    }

                    reader.SkipBytes(6);
                    ReadAdditionalData(reader, batLog);
                    ReadAdditionalData(reader, batLog);
                    ReadAdditionalData(reader, batLog);
                }

                FftBlock fftData = ReadFftData(reader, batLog);
                if (fftData == null || fftData.Index != i)
                {
                    AddMessage(batLog, BatLogMessageLevel.Warning, $"FFT index stimmt nicht mit den daten überein.", reader);
                }
                else
                {
                    call.FftData.Add(fftData);
                }
            }
        }

        private void ReadAdditionalData(BinaryReader reader, BatLog log)
        {
            AdditionalDataType type = (AdditionalDataType)reader.ReadByte();
            long timestamp = reader.ReadUInt32();
            switch (type)
            {
                case AdditionalDataType.None:
                    reader.SkipBytes(3);
                    break;
                case AdditionalDataType.Temperature:
                    TemperatureData tData = new TemperatureData();
                    tData.Timestamp = timestamp;
                    tData.DateTime = log.StartTime.AddMilliseconds(timestamp);
                    tData.Temperature = reader.ReadUInt16();
                    log.TemperatureData.Add(tData);
                    reader.SkipBytes(1);
                    break;
                case AdditionalDataType.Voltage:
                    BatteryData vData = new BatteryData();
                    vData.Timestamp = timestamp;
                    vData.DateTime = log.StartTime.AddMilliseconds(timestamp);
                    vData.Voltage = reader.ReadUInt16();
                    log.BatteryData.Add(vData);
                    reader.SkipBytes(1);
                    break;
                default:
                    AddMessage(log, BatLogMessageLevel.Warning, $"Unbekannter AdditionalDataType '0x{type:X}' wird ignoriert.", reader);
                    reader.SkipBytes(3);
                    break;
            }
        }

        private void ProcessCall(BatCall call, BatLog log)
        {
            int noisiness = 0;
            bool[] check = new bool[40];
            int[] values = new int[40];
            int peak = 0;
            int pwrAbove = 0;
            for (int i = 0; i < call.FftData.Count; i++)
            {
                FftBlock fftBlock = call.FftData[i];
                for (int j = 10; j < fftBlock.Data.Length; j++)
                {
                    byte value = fftBlock.Data[j];
                    if (j > 25)
                    {
                        if (value > 50)
                        {
                            pwrAbove++;
                        }
                    }

                    if (j < 25)
                    {
                        if (i == 0)
                        {
                            values[j] = value;
                        }
                        else
                        {
                            int delta = values[j] - value;
                            if (check[j] && delta > 10)
                            {
                                noisiness++;
                            }
                            else if (!check[j] && delta < -10)
                            {
                                noisiness++;
                            }

                            values[j] = value;
                            check[j] = value < 0;
                        }
                    }
                }

                if (fftBlock.Loudness >= 2400)
                {
                    peak++;
                }
            }


            call.AvgPeakFrequency = Math.Round(noisiness / (double)call.FftData.Count, 1);
            call.MaxPeakFrequency = peak;

            if (pwrAbove >= 2 || call.AvgPeakFrequency < 1 || peak > 30 && call.AvgPeakFrequency < 2)
            {
                call.IsBat = true;
            }


            //int pwrAbove = 0;
            //int pwrBelow = 0;
            //int ctr = 0;
            //bool[] check = new bool[40];
            //for (int i = 0; i < call.FftData.Count; i++)
            //{
            //    for (int j = 0; j < call.FftData[i].Data.Length; j++)
            //    {
            //        byte value = call.FftData[i].Data[j];
            //        if (j > 25)
            //        {
            //            if (value > 50)
            //            {
            //                pwrAbove++;
            //            }
            //        }
            //        else if (j > 10)
            //        {
            //            if (value > 10)
            //            {
            //                check[j] = true;
            //            }

            //        }
            //    }
            //}
            //int belowRate = pwrBelow / call.FftData.Count;

            //if (pwrAbove >= 2)
            //{
            //    call.IsBat = true;
            //}

            //call.AvgPeakFrequency = Math.Round(call.FftData.Select(f => f.Loudness).Average());
            //call.MaxPeakFrequency = check.Count(c => c);
            ////if (call.AvgPeakFrequency > 2000)
            ////{
            ////    call.IsBat = true;
            ////}
            //if (call.MaxPeakFrequency < 10 && call.AvgPeakFrequency > 2000)
            //{
            //    call.IsBat = true;
            //}

            //if (maxValue > 20)
            //{
            //    call.IsBat = true;
            //}

            //if (maxValue >= 255)
            //{
            //    log._dist[254]++;
            //}
            //else
            //{
            //    log._dist[maxValue]++;
            //}

            //int maxValue = 0;
            //int maxValueIndex = 0;
            //int[] maxAvg = new int[128];

            //for (int i = 0; i < call.FftData.Count; i++)
            //{
            //    for (int j = 20; j < call.FftData[i].Data.Length; j++)
            //    {
            //        byte value = call.FftData[i].Data[j];
            //        if (value > maxValue)
            //        {
            //            maxValue = value;
            //            maxValueIndex = j;
            //        }

            //        maxAvg[j] += value;
            //    }
            //}

            //int max = 0;
            //for (int i = 0; i < maxAvg.Length; i++)
            //{
            //    if (maxAvg[i] > max)
            //    {
            //        max = maxAvg[i];
            //        call.AvgPeakFrequency = i;
            //    }
            //}

            //call.MaxPeakFrequency = maxValueIndex;
        }

        /// <summary>
        ///    FFT Block:
        ///    |  0  |  1  |  2  |  3  |  4  |  5  |  6  |  7  |  8-64 |
        ///    | 255 | 253 |   Index   | Loudness  |  0  |  0  |  FFT  |
        /// </summary>
        private FftBlock ReadFftData(BinaryReader reader, BatLog batLog)
        {
            if (reader.ReadByte() != 0xFF)
            {
                return null;
            }

            if (reader.ReadByte() != 0xFD)
            {
                return null;
            }

            FftBlock fft = new FftBlock();
            fft.Index = reader.ReadUInt16();

            ReadAdditionalData(reader, batLog);
            ReadAdditionalData(reader, batLog);
            ReadAdditionalData(reader, batLog);

            fft.Loudness = reader.ReadUInt16();
            fft.SampleNr = reader.ReadUInt16();

            fft.Data = reader.ReadBytes(128);

            return fft;
        }

        private enum AdditionalDataType
        {
            None = 0,
            Temperature = 1,
            Voltage = 2
        }
    }
}