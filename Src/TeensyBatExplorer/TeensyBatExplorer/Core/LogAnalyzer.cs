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
using System.Collections.Generic;
using System.Linq;

using TeensyBatExplorer.Core.BatLog;
using TeensyBatExplorer.Core.BatLog.Raw;

namespace TeensyBatExplorer.Core
{
    public class LogAnalyzer
    {
        public static int GetFrequencyFromFftBin(int bin)
        {
            return (int)Math.Round((bin + 1) * 3.6);
        }

        public void Analyze(BatProject project, BatNode node, RawNodeData rawNodeData)
        {
            BatNodeData nodeData = node.NodeData;
            node.IsDirty = true;

            node.NodeId = rawNodeData.NodeId;
            node.LogStart = rawNodeData.LogStart;

            nodeData.Calls.Clear();

            List<RawCall> rawCallsToMerge = new List<RawCall>();
            BatCall currentCallData = new BatCall();
            nodeData.Calls.Add(currentCallData);
            uint endCallTime = 0;
            foreach (RawCall call in rawNodeData.Calls)
            {
                if (endCallTime + 50 >= call.StartTimeMs || rawCallsToMerge.Count == 0)
                {
                    rawCallsToMerge.Add(call);
                }
                else
                {
                    nodeData.Calls.Add(Merge(rawCallsToMerge));
                    rawCallsToMerge.Clear();
                }

                endCallTime = call.EndTimeMs;
            }

            if (rawCallsToMerge.Count > 0)
            {
                nodeData.Calls.Add(Merge(rawCallsToMerge));
            }
        }

        public BatCall Merge(List<RawCall> rawCalls)
        {
            BatCall mergedCall = new BatCall();
            RawCall firstCall = rawCalls[0];

            mergedCall.StartTimeMs = firstCall.StartTimeMs;

            mergedCall.ClippedSamples = 0;
            mergedCall.MissedSamples = 0;

            List<byte> powerData = new List<byte>();
            ulong lastEndTime = firstCall.EndTimeMs;

            uint[] tempFftData = new uint[256];
            foreach (RawCall rawCall in rawCalls)
            {
                if (lastEndTime < rawCall.StartTimeMs)
                {
                    int delta = (int)Math.Round((rawCall.StartTimeMs - lastEndTime) / 0.251);
                    powerData.AddRange(Enumerable.Repeat((byte)0, delta));
                }

                powerData.AddRange(rawCall.PowerData);
                lastEndTime = rawCall.EndTimeMs;

                // We currently have a sample rate of 231kHz and a Buffer of 1024.
                // That means we get data from 0Hz to 115.5kHz within 256 buckets.
                // We'll combine 8 buckets together to get a range of about 3.6kHz per Bucket.
                // The fist and the last Bucket are omitted
                ushort[] fftData = rawCall.FftData;

                if (fftData.Length != 256)
                {
                    return mergedCall;
                }

                for (int i = 0; i < rawCall.FftData.Length; i++)
                {
                    tempFftData[i] += rawCall.FftData[i];
                }

                mergedCall.Duration += rawCall.Duration;
                mergedCall.ClippedSamples += rawCall.ClippedSamples;
                mergedCall.MissedSamples += rawCall.MissedSamples;
            }

            AnalyzeFftData(mergedCall, tempFftData, rawCalls.Count);
            mergedCall.PowerData = powerData.ToArray();
            mergedCall.AveragePower = (byte)Math.Round(mergedCall.PowerData.Where(p => p > 20).DefaultIfEmpty().Average(p => p));
            mergedCall.MergeCount = rawCalls.Count;

            return mergedCall;
        }

        private void AnalyzeFftData(BatCall mergedCall, uint[] fftData, int count)
        {
            byte[] newFftData = new byte[32];

            int fftIndex = 8;
            int max = fftData.Length - 7;
            int mergedIndex = 0;

            byte maxValue = 0;
            int maxFftIndex = 0;

            while (fftIndex < max)
            {
                uint val = fftData[fftIndex++];
                val += fftData[fftIndex++];
                val += fftData[fftIndex++];
                val += fftData[fftIndex++];
                val += fftData[fftIndex++];
                val += fftData[fftIndex++];
                val += fftData[fftIndex++];
                val += fftData[fftIndex++];
                byte average = (byte)(val / (8 * count));
                if (average > maxValue)
                {
                    maxFftIndex = mergedIndex;
                    maxValue = average;
                }

                newFftData[mergedIndex++] = average;
            }

            mergedCall.FftData = newFftData;
            mergedCall.MaxFrequencyBin = maxFftIndex;
        }
    }
}