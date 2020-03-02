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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Windows.Storage;

namespace TeensyBatExplorer.Services
{
    public class LogReader
    {

        public async Task Load(StorageFile file, BatLog log)
        {
            using (var logStream = await file.OpenStreamForReadAsync())
            {
                using (BinaryReader reader = new BinaryReader(logStream))
                {
                    ReadData(reader, log);
                }
            }
        }

        private void ReadData(BinaryReader reader, BatLog log)
        {
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            StreamWriter writer = File.AppendText($"{storageFolder.Path}\\test.csv");
            Debug.WriteLine(storageFolder.Path);
            Stream stream = reader.BaseStream;
            while (stream.Position < stream.Length)
            {
                if (reader.ReadByte() == 0xFF)
                {
                    if (reader.ReadByte() == 0xFE)
                    {
                        BatCall call = new BatCall();
                        ReadCall(call, reader, log);
                        ProcessCall(call, log, writer);
                        log.Calls.Add(call);
                    }
                }

                //Seek to the next block
                long offset = stream.Position % 512;
                if (offset > 0)
                {
                    stream.Seek(512 - offset, SeekOrigin.Current);
                }
            }
            writer.Close();
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
            try
            {
                call.FftCount = reader.ReadUInt16();
                call.StartTimeMS = reader.ReadUInt32();
                call.StartTime = batLog.StartTime.AddMilliseconds(call.StartTimeMS);

                // Error Counters
                reader.SkipBytes(8);

                ReadAdditionalData(reader, batLog);
                ReadAdditionalData(reader, batLog);

                for (int i = 0; i < call.FftCount; i++)
                {
                    if (i > 0 && i % 3 == 0)
                    {
                        //Additional Header
                        if (reader.ReadByte() != 0xFF)
                        {
                            //ups
                        }

                        if (reader.ReadByte() != 0xFC)
                        {
                            // ups
                        }

                        reader.SkipBytes(6);
                        ReadAdditionalData(reader, batLog);
                        ReadAdditionalData(reader, batLog);
                        ReadAdditionalData(reader, batLog);
                    }

                    FftBlock fftData = ReadFftData(reader, batLog);
                    if (fftData == null || fftData.Index != i)
                    {
                        //Ups?
                    }
                    else
                    {
                        call.FftData.Add(fftData);
                    }
                }
            }
            catch
            {
            }
        }

        private void ReadAdditionalData(BinaryReader reader, BatLog log)
        {
            AdditionalDataType type = (AdditionalDataType)reader.ReadByte();
            int timestamp = reader.ReadInt32();
            switch (type)
            {
                case AdditionalDataType.None:
                    reader.SkipBytes(3);
                    break;
                case AdditionalDataType.Temperature:
                    TemperatureData tData = new TemperatureData { Timestamp = timestamp, DateTime = log.StartTime.AddMilliseconds(timestamp) };
                    tData.Temperature = reader.ReadInt16() / 10.0;
                    log.TemperatureData.Add(tData);
                    reader.SkipBytes(1);
                    break;
                case AdditionalDataType.Voltage:
                    BatteryData vData = new BatteryData { Timestamp = timestamp, DateTime = log.StartTime.AddMilliseconds(timestamp) };
                    vData.Voltage = reader.ReadInt16() / 10.0;
                    log.BatteryData.Add(vData);
                    reader.SkipBytes(1);
                    break;
                default:
                    //Ups
                    reader.SkipBytes(3);
                    break;
            }
        }

        private void ProcessCall(BatCall call, BatLog log, StreamWriter writer)
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

            if (pwrAbove >= 2 || call.AvgPeakFrequency < 1 || (peak > 30 && call.AvgPeakFrequency < 2))
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
            fft.Index = reader.ReadInt16();

            ReadAdditionalData(reader, batLog);
            ReadAdditionalData(reader, batLog);
            ReadAdditionalData(reader, batLog);

            fft.Loudness = reader.ReadInt16();
            fft.SampleNr = reader.ReadInt16();

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

    public static class BinaryReaderHelper
    {
        public static void SkipBytes(this BinaryReader reader, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (reader.ReadByte() != 0)
                {
                    // Ups...
                }
            }
        }
    }

    public class BatLog
    {
        public DateTime StartTime { get; set; }
        public List<BatCall> Calls { get; set; } = new List<BatCall>();
        public List<BatteryData> BatteryData { get; set; } = new List<BatteryData>();
        public List<TemperatureData> TemperatureData { get; set; } = new List<TemperatureData>();

        public int[] _dist = new int[255];

    }

    public class BatCall
    {
        public int FftCount { get; set; }
        public long StartTimeMS { get; set; }
        public List<FftBlock> FftData { get; set; } = new List<FftBlock>();
        public double MaxPeakFrequency { get; set; }
        public double AvgPeakFrequency { get; set; }
        public DateTime StartTime { get; set; }
        public bool IsBat { get; set; }
    }

    public class FftBlock
    {
        public int Index { get; set; }
        public int Loudness { get; set; }
        public int SampleNr { get; set; }
        public byte[] Data { get; set; }
    }

    [DebuggerDisplay("{" + nameof(Voltage) + "}V")]
    public class BatteryData : AdditionalData
    {
        public double Voltage { get; set; }
    }

    [DebuggerDisplay("{" + nameof(Temperature) + "}°C")]
    public class TemperatureData : AdditionalData
    {
        public double Temperature { get; set; }
    }

    public class AdditionalData
    {
        public long Timestamp { get; set; }
        public DateTime DateTime { get; set; }
    }
}