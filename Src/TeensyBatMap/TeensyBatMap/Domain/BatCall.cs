using System;
using TeensyBatMap.Database;

namespace TeensyBatMap.Domain
{
    public class BatCall
    {
        public BatCall(BatNodeLog log, int startTimeMs, int duration, int maxFrequency, int maxIntensity, int avgFrequency, int avgIntensity)
        {
            StartTimeMs = startTimeMs;
            Duration = duration;
            MaxFrequency = maxFrequency;
            MaxIntensity = maxIntensity;
            AvgFrequency = avgFrequency;
            AvgIntensity = avgIntensity;
            StartTime = log.LogStart.AddMilliseconds(startTimeMs);
        }

        public BatCall()
        {}

        public int Id { get; set; }

        public DateTime StartTime { get; private set; }

        public int StartTimeMs { get; set; }

        public int Duration { get; set; }

        public int MaxFrequency { get; set; }

        public int MaxIntensity { get; set; }

        public int AvgFrequency { get; set; }

        public int AvgIntensity { get; set; }

        public int BatNodeLogId { get; set; }

		public virtual byte[] FftData { get; set; }
    }
}