using System;
using TeensyBatMap.Database;

namespace TeensyBatMap.Domain
{
    [Table("BatCalls")]
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

        [PrimaryKey]
        [Unique]
        [AutoIncrement]
        public int Id { get; set; }

        [Ignore]
        public DateTime StartTime { get; private set; }

        [NotNull]
        public int StartTimeMs { get; set; }

        [NotNull]
        public int Duration { get; set; }

        [NotNull]
        public int MaxFrequency { get; set; }

        [NotNull]
        public int MaxIntensity { get; set; }

        [NotNull]
        public int AvgFrequency { get; set; }

        [NotNull]
        public int AvgIntensity { get; set; }

        [NotNull]
        [Indexed(Name = "IFK_BatNodeLog")]
        public int BatNodeLogId { get; set; }
    }
}