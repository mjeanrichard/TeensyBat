﻿using System;

namespace TeensyBatMap.Domain
{
    public class BatCall
    {
        public BatCall(BatNodeLog log, uint startTimeMs, uint duration, uint maxFrequency, uint maxPower, uint avgFrequency, uint dcOffset, byte[] data, byte[] powerData)
        {
            StartTimeMs = startTimeMs;
            Duration = duration;
            MaxFrequency = maxFrequency;
            MaxPower = maxPower;
            AvgFrequency = avgFrequency;
            DcOffset = dcOffset;
            StartTime = log.LogStart.AddMilliseconds(startTimeMs);
            FftData = data;
            PowerData = powerData;
            Enabled = true;
        }

        public BatCall()
        {
            Enabled = true;
            PowerData = new byte[0];
            FftData = new byte[0];
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

        public uint MaxPower { get; set; }

        public uint AvgFrequency { get; set; }

        public uint DcOffset { get; set; }

        public int BatNodeLogId { get; set; }

        public bool Enabled { get; set; }

        public virtual byte[] FftData { get; set; }

        public virtual byte[] PowerData { get; set; }
    }
}