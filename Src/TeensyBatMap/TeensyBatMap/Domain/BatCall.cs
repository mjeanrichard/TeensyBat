using System;
using TeensyBatMap.Database;

namespace TeensyBatMap.Domain
{
    public class BatCall
    {
        public BatCall(BatNodeLog log, uint startTimeMs, uint duration, uint maxFrequency, uint maxIntensity, uint avgFrequency, uint avgIntensity, byte[] data)
        {
            StartTimeMs = startTimeMs;
            Duration = duration;
            MaxFrequency = maxFrequency;
            MaxIntensity = maxIntensity;
            AvgFrequency = avgFrequency;
            AvgIntensity = avgIntensity;
            StartTime = log.LogStart.AddMilliseconds(startTimeMs);
			FftData = data;
	        Enabled = true;
        }

	    public BatCall()
	    {
		    Enabled = true;
	    }

        public int Id { get; set; }

        public DateTime StartTime { get; private set; }

        public uint StartTimeMs { get; set; }

        public int ClippedSamples { get; set; }

        public int MissedSamples { get; set; }

		/// <summary>
		/// The Duration of the Call in MircoSeconds.
		/// </summary>
        public uint Duration { get; set; }

        public uint MaxFrequency { get; set; }

        public uint MaxIntensity { get; set; }

        public uint AvgFrequency { get; set; }

        public uint AvgIntensity { get; set; }

        public int BatNodeLogId { get; set; }

		public bool Enabled { get; set; }

		public virtual byte[] FftData { get; set; }
    }
}