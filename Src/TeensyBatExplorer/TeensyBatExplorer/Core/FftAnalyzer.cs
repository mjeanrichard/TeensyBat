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

namespace TeensyBatExplorer.Core
{
    //public class FftAnalyzer
    //{
    //    private readonly double _factor;
    //    private readonly int _windowSize;

    //    public FftAnalyzer(double factor, int windowSize = 10)
    //    {
    //        _factor = factor;
    //        _windowSize = windowSize;
    //    }

    //    public FftResult Analyze(BatNodeData nodeData)
    //    {
    //        FftResult result = new FftResult();

    //        result.FftData = nodeData.FftData;
    //        result.DcOffset = result.FftData[0];

    //        result.Peaks = GetPeaks(result.FftData).Select(p => p.Key).ToArray();
    //        return result;
    //    }

    //    public IEnumerable<KeyValuePair<int, uint>> GetPeaks(ushort[] data)
    //    {
    //        uint localMax = 0;

    //        double avgMax = 0;
    //        double avgMin = double.MaxValue;

    //        int iMax = -1;

    //        uint movingSum = 0;

    //        bool lookingForMax = true;

    //        for (int i = 1; i < data.Length; i++)
    //        {
    //            uint val = data[i];
    //            double avg;
    //            if (i < _windowSize + 1)
    //            {
    //                movingSum += val;
    //            }
    //            else
    //            {
    //                movingSum += val - data[i - _windowSize];
    //            }
    //            avg = Math.Max(1, movingSum / (double)_windowSize);

    //            if (lookingForMax)
    //            {
    //                if (avg > avgMax)
    //                {
    //                    avgMax = avg;
    //                }
    //                if (val > localMax)
    //                {
    //                    localMax = val;
    //                    iMax = i;
    //                }
    //            }
    //            else
    //            {
    //                if (avg < avgMin)
    //                {
    //                    avgMin = avg;
    //                    localMax = 0;
    //                }
    //                else if (val > localMax)
    //                {
    //                    localMax = val;
    //                    iMax = i;
    //                }
    //            }
    //            if (lookingForMax && avg < avgMax / _factor)
    //            {
    //                yield return new KeyValuePair<int, uint>(iMax, localMax);
    //                avgMin = avg;
    //                localMax = 0;
    //                lookingForMax = false;
    //            }
    //            else if (!lookingForMax && avg > avgMin * _factor)
    //            {
    //                avgMax = avg;
    //                lookingForMax = true;
    //            }
    //        }
    //        //Check if there is a Peak at the end...
    //        if (lookingForMax)
    //        {
    //            yield return new KeyValuePair<int, uint>(iMax, localMax);
    //        }
    //    }

    //    public void Recalculate(BatNodeData nodeData)
    //    {
    //        FftResult fftResult = Analyze(nodeData);
    //        //call.DcOffset = fftResult.DcOffset;
    //        uint maxPeak = 0;
    //        int maxPeakIndex = -1;
    //        foreach (int peakIndex in fftResult.Peaks)
    //        {
    //            uint peakValue = fftResult.FftData[peakIndex];
    //            if (peakValue > maxPeak)
    //            {
    //                maxPeak = peakValue;
    //                maxPeakIndex = peakIndex;
    //            }
    //        }
    //        if (maxPeakIndex >= 0 && maxPeak > 10)
    //        {
    //            //call.MaxFrequencyBin = (uint)Math.Round(maxPeakIndex * 0.451);
    //        }
    //        else
    //        {
    //            //call.Enabled = false;
    //        }
    //    }
    //}
}