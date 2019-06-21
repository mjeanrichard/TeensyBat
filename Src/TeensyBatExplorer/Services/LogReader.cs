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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.Streams;

using Microsoft.AppCenter.Crashes;

namespace TeensyBatExplorer.Services
{
    public class LogReader
    {
        public async Task<List<BatLog2>> Load(StorageFile file)
        {
            List<BatLog2> logs;
            using (var logStream = await file.OpenStreamForReadAsync())
            {
                using (BinaryReader reader = new BinaryReader(logStream))
                {
                    logs = ReadData(reader).ToList();
                }
            }

            return logs;
        }

        private IEnumerable<BatLog2> ReadData(BinaryReader reader)
        {
            Stream stream = reader.BaseStream;
            while (stream.Position < stream.Length)
            {
                if (reader.ReadByte() == 0xFF)
                {
                    if (reader.ReadByte() == 0xFE)
                    {
                        BatLog2 log = new BatLog2();
                        ReadCall(log, reader);
                        ProcessCall(log);
                        yield return log;
                    }
                }

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
        private void ReadCall(BatLog2 log, BinaryReader reader)
        {
            log.FftCount = reader.ReadUInt16();
            log.StartTimeMS = reader.ReadUInt32();

            reader.SkipBytes(24);

            for (int i = 0; i < log.FftCount; i++)
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

                    reader.SkipBytes(30);
                }

                FftBlock fftData = ReadFftData(reader);
                if (fftData == null || fftData.Index != i)
                {
                    //Ups?
                }
                else
                {
                    log.FftData.Add(fftData);
                }
            }
        }

        private void ProcessCall(BatLog2 call)
        {
            int maxValue = 0;
            int maxValueIndex = 0;
            int[] maxAvg = new int[128];

            for (int i = 0; i < call.FftData.Count; i++)
            {
                for (int j = 1; j < call.FftData[i].Data.Length; j++)
                {
                    byte value = call.FftData[i].Data[j];
                    if (value > maxValue)
                    {
                        maxValue = value;
                        maxValueIndex = j;
                    }

                    maxAvg[j] += value;
                }
            }

            int max = 0;
            for (int i = 0; i < maxAvg.Length; i++)
            {
                if (maxAvg[i] > max)
                {
                    max = maxAvg[i];
                    call.AvgPeakFrequency = i * 2;
                }
            }

            call.MaxPeakFrequency = maxValueIndex * 2;
            
        }

        /// <summary>
        ///    FFT Block:
        ///    |  0  |  1  |  2  |  3  |  4  |  5  |  6  |  7  |  8-64 |
        ///    | 255 | 253 |   Index   | Loudness  |  0  |  0  |  FFT  |
        /// </summary>
        private FftBlock ReadFftData(BinaryReader reader)
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

            reader.SkipBytes(24);

            fft.Loudness = reader.ReadInt16();
            fft.SampleNr = reader.ReadInt16();

            fft.Data = reader.ReadBytes(128);

            return fft;
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

    public class BatLog2
    {
        public int FftCount { get; set; }
        public long StartTimeMS { get; set; }
        public List<FftBlock> FftData { get; set; } = new List<FftBlock>();
        public int MaxPeakFrequency { get; set; }
        public int AvgPeakFrequency { get; set; }
    }

    public class FftBlock
    {
        public int Index { get; set; }
        public int Loudness { get; set; }
        public int SampleNr { get; set; }
        public byte[] Data { get; set; }
    }
}