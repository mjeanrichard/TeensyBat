using System;
using System.Collections.Generic;
using System.Linq;

namespace TeensyBatMap.Domain
{
    public class FftAnalyzer
    {
        private readonly double _factor;
        private readonly int _windowSize;

        public FftAnalyzer(double factor, int windowSize = 10)
        {
            _factor = factor;
            _windowSize = windowSize;
        }

        public uint[] BuildFft(BatCall call)
        {
            byte[] data = call.FftData;

            uint[] fft = new uint[data.Length / 2];
            for (int i = 0; i < data.Length / 2; i++)
            {
                fft[i] = BitConverter.ToUInt16(data, i * 2);
            }
            return fft;
        }

        public FftResult Analyze(BatCall call)
        {
            FftResult result = new FftResult();

            result.FftData = BuildFft(call);
            result.DcOffset = result.FftData[0];

            result.Peaks = GetPeaks(result.FftData).Select(p => p.Key).ToArray();
            return result;
        }

        public IEnumerable<KeyValuePair<int, uint>> GetPeaks(uint[] data)
        {
            uint localMax = 0;

            double avgMax = 0;
            double avgMin = double.MaxValue;

            int iMax = -1;

            uint movingSum = 0;

            bool lookingForMax = true;

            for (int i = 1; i < data.Length; i++)
            {
                uint val = data[i];
                double avg;
                if (i < _windowSize + 1)
                {
                    movingSum += val;
                }
                else
                {
                    movingSum += val - data[i - _windowSize];
                }
                avg = Math.Max(1, movingSum / (double)_windowSize);

                if (lookingForMax)
                {
                    if (avg > avgMax)
                    {
                        avgMax = avg;
                    }
                    if (val > localMax)
                    {
                        localMax = val;
                        iMax = i;
                    }
                }
                else
                {
                    if (avg < avgMin)
                    {
                        avgMin = avg;
                        localMax = 0;
                    }
                    else if (val > localMax)
                    {
                        localMax = val;
                        iMax = i;
                    }
                }
                if (lookingForMax && (avg < avgMax / _factor))
                {
                    yield return new KeyValuePair<int, uint>(iMax, localMax);
                    avgMin = avg;
                    localMax = 0;
                    lookingForMax = false;
                }
                else if (!lookingForMax && (avg > avgMin * _factor))
                {
                    avgMax = avg;
                    lookingForMax = true;
                }
            }
            //Check if there is a Peak at the end...
            if (lookingForMax)
            {
                yield return new KeyValuePair<int, uint>(iMax, localMax);
            }
        }

        public void Recalculate(BatCall call)
        {
            FftResult fftResult = Analyze(call);
            call.DcOffset = fftResult.DcOffset;
            uint maxPeak = 0;
            int maxPeakIndex = -1;
            foreach (int peakIndex in fftResult.Peaks)
            {
                uint peakValue = fftResult.FftData[peakIndex];
                if (peakValue > maxPeak)
                {
                    maxPeak = peakValue;
                    maxPeakIndex = peakIndex;
                }
            }
            if ((maxPeakIndex >= 0) && (maxPeak > 10))
            {
                call.MaxFrequency = (uint)Math.Round(maxPeakIndex * 0.451);
            }
            else
            {
                call.Enabled = false;
            }
        }
    }
}