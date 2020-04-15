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

        private void AddMessage(BatDataFile dataFile, BatLogMessageLevel level, string message, BinaryReader reader)
        {
            dataFile.LogMessages.Add(new ProjectMessage { Message = message, MessageType = MessageTypes.LogFile, Level = level, Position = reader?.BaseStream.Position });
        }

        public async Task Load(string filename, BatDataFile dataFile)
        {
            await Task.Run(() => LoadInternal(filename, dataFile));
        }

        public void LoadInternal(string filename, BatDataFile dataFile)
        {
            using (Stream logStream = File.OpenRead(filename))
            {
                using (BinaryReader reader = new BinaryReader(logStream))
                {
                    ReadData(reader, dataFile);

                    Match filenameMatch = FilenamePattern.Match(Path.GetFileNameWithoutExtension(filename));
                    if (filenameMatch.Success && filenameMatch.Groups["d"].Success)
                    {
                        if (dataFile.FileCreateTime == DateTime.UnixEpoch)
                        {
                            if (DateTime.TryParseExact(filenameMatch.Groups["d"].Value, "yyyyMMddHHmm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime parsedDate))
                            {
                                AddMessage(dataFile, BatLogMessageLevel.Information, "Datei Erstellungsdatum aus Dateiname geladen.", null);
                                dataFile.FileCreateTime = parsedDate;
                            }
                        }

                        if (dataFile.ReferenceTime == DateTime.UnixEpoch)
                        {
                            if (DateTime.TryParseExact(filenameMatch.Groups["d"].Value, "yyyyMMddHHmm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime parsedDate))
                            {
                                AddMessage(dataFile, BatLogMessageLevel.Warning, "Datei Referenzdatum aus Dateiname geladen.", null);
                                dataFile.ReferenceTime = parsedDate;
                            }
                        }

                        if (dataFile.NodeNumber == 0 && filenameMatch.Groups["nn"].Success)
                        {
                            if (int.TryParse(filenameMatch.Groups["nn"].Value, out int parsedNodeNumber))
                            {
                                AddMessage(dataFile, BatLogMessageLevel.Information, "Gerätenummer aus Dateinamen geladen.", null);
                                dataFile.NodeNumber = parsedNodeNumber;
                            }
                        }
                    }
                }
            }
        }

        private void ReadFileHeader(BinaryReader reader, BatDataFile dataFile)
        {
            byte magicOne = reader.ReadByte();
            byte magicTwo = reader.ReadByte();
            if (magicOne != 0xFF || magicTwo != 0xCC)
            {
                AddMessage(dataFile, BatLogMessageLevel.Warning, $"Ungültiger File Marker 0x{magicOne:X2}{magicTwo:X2} (0xFFCC erwartet).", reader);
            }

            dataFile.HardwareVersion = reader.ReadByte();
            dataFile.FirmwareVersion = reader.ReadByte();
            dataFile.NodeNumber = reader.ReadByte();
            dataFile.Debug = reader.ReadBoolean();
            dataFile.PreCallBufferSize = reader.ReadByte();
            dataFile.AfterCallBufferSize = reader.ReadByte();
            dataFile.CallStartThreshold = reader.ReadUInt16();
            dataFile.CallEndThreshold = reader.ReadUInt16();

            reader.SkipBytes(4);


            if (dataFile.FirmwareVersion < 3)
            {
                // DataFile only has the timestamp of file creation in it.
                dataFile.FileCreateTime = reader.ReadDateTime();

                // the microseconds Offset is not usable in these versions.
                reader.ReadUInt32();

                // This is incorrect, but the best we have... It should later be
                // fixed to the first date of the Node
                dataFile.OriginalReferenceTime = dataFile.FileCreateTime;
                dataFile.ReferenceTime = dataFile.FileCreateTime;
                reader.ReadUInt32();
            }
            else
            {
                dataFile.ReferenceTime = reader.ReadDateTimeWithMicroseconds();
                dataFile.OriginalReferenceTime = dataFile.ReferenceTime;
                dataFile.FileCreateTime = reader.ReadDateTime();
            }

            reader.SkipBytes(484);
        }

        private void ReadData(BinaryReader reader, BatDataFile dataFile)
        {
            Stream stream = reader.BaseStream;
            ReadFileHeader(reader, dataFile);
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
                    AddMessage(dataFile, BatLogMessageLevel.Error, $"Call marker erwartet (0xFFFE) aber 0x{markOne:X2}{markTwo:X2} gefunden.", reader);
                    return;
                }

                BatDataFileEntry dataFileEntry = new BatDataFileEntry();
                ReadCall(dataFileEntry, reader, dataFile);
                ProcessCall(dataFileEntry, dataFile);
                dataFile.Entries.Add(dataFileEntry);

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
        private void ReadCall(BatDataFileEntry dataFileEntry, BinaryReader reader, BatDataFile batDataFile)
        {
            dataFileEntry.FftCount = reader.ReadUInt16();
            dataFileEntry.StartTimeMillis = reader.ReadUInt32();
            dataFileEntry.StartTimeMicros = dataFileEntry.StartTimeMillis * 1000;

            // Error Counters
            batDataFile.ErrorCountCallBuffFull += reader.ReadByte();
            batDataFile.ErrorCountPointerBufferFull += reader.ReadByte();
            batDataFile.ErrorCountDataBufferFull += reader.ReadByte();
            batDataFile.ErrorCountProcessOverlap += reader.ReadByte();

            dataFileEntry.HighFreqSampleCount = reader.ReadByte();
            dataFileEntry.HighPowerSampleCount = reader.ReadByte();
            dataFileEntry.MaxLevel = reader.ReadByte();
            reader.ReadByte();

            ReadAdditionalData(reader, batDataFile);
            ReadAdditionalData(reader, batDataFile);

            for (int i = 0; i < dataFileEntry.FftCount; i++)
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
                    ReadAdditionalData(reader, batDataFile);
                    ReadAdditionalData(reader, batDataFile);
                    ReadAdditionalData(reader, batDataFile);
                }

                FftBlock fftData = ReadFftData(reader, batDataFile);
                if (fftData == null || fftData.Index != i)
                {
                    AddMessage(batDataFile, BatLogMessageLevel.Warning, $"FFT index stimmt nicht mit den daten überein.", reader);
                }
                else
                {
                    dataFileEntry.FftData.Add(fftData);
                }
            }
        }

        private void ReadAdditionalData(BinaryReader reader, BatDataFile dataFile)
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
                    tData.DateTime = dataFile.ReferenceTime.AddMilliseconds(timestamp);
                    tData.Temperature = reader.ReadUInt16();
                    dataFile.TemperatureData.Add(tData);
                    reader.SkipBytes(1);
                    break;
                case AdditionalDataType.Voltage:
                    BatteryData vData = new BatteryData();
                    vData.Timestamp = timestamp;
                    vData.DateTime = dataFile.ReferenceTime.AddMilliseconds(timestamp);
                    vData.Voltage = reader.ReadUInt16();
                    dataFile.BatteryData.Add(vData);
                    reader.SkipBytes(1);
                    break;
                default:
                    AddMessage(dataFile, BatLogMessageLevel.Warning, $"Unbekannter AdditionalDataType '0x{type:X}' wird ignoriert.", reader);
                    reader.SkipBytes(3);
                    break;
            }
        }

        private void ProcessCall(BatDataFileEntry dataFileEntry, BatDataFile dataFile)
        {
            //int noisiness = 0;
            //bool[] check = new bool[40];
            //int[] values = new int[40];
            //int peak = 0;
            //int pwrAbove = 0;
            //for (int i = 0; i < dataFileEntry.FftData.Count; i++)
            //{
            //    FftBlock fftBlock = dataFileEntry.FftData[i];
            //    for (int j = 10; j < fftBlock.Data.Length; j++)
            //    {
            //        byte value = fftBlock.Data[j];
            //        if (j > 25)
            //        {
            //            if (value > 50)
            //            {
            //                pwrAbove++;
            //            }
            //        }

            //        if (j < 25)
            //        {
            //            if (i == 0)
            //            {
            //                values[j] = value;
            //            }
            //            else
            //            {
            //                int delta = values[j] - value;
            //                if (check[j] && delta > 10)
            //                {
            //                    noisiness++;
            //                }
            //                else if (!check[j] && delta < -10)
            //                {
            //                    noisiness++;
            //                }

            //                values[j] = value;
            //                check[j] = value < 0;
            //            }
            //        }
            //    }

            //    if (fftBlock.Loudness >= 2400)
            //    {
            //        peak++;
            //    }
            //}


            //dataFileEntry.AvgPeakFrequency = Math.Round(noisiness / (double)dataFileEntry.FftData.Count, 1);
            //dataFileEntry.MaxPeakFrequency = peak;

            //if (pwrAbove >= 2 || dataFileEntry.AvgPeakFrequency < 1 || peak > 30 && dataFileEntry.AvgPeakFrequency < 2)
            //{
            //    dataFileEntry.IsBat = true;
            //}


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
        private FftBlock ReadFftData(BinaryReader reader, BatDataFile batDataFile)
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

            ReadAdditionalData(reader, batDataFile);
            ReadAdditionalData(reader, batDataFile);
            ReadAdditionalData(reader, batDataFile);

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