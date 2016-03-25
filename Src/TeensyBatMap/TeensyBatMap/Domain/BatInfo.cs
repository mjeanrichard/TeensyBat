using System;

namespace TeensyBatMap.Domain
{
	public class BatInfo
	{
		public BatInfo(DateTime time, uint timeMs, uint sampleDuration, uint batteryVoltage)
		{
		}

		public BatInfo()
		{
		}

		public int Id { get; set; }

		public int BatNodeLogId { get; set; }

		public DateTime Time { get; set; }

		public uint TimeMs { get; set; }

		/// <summary>
		/// Duration in ms to fill one sample buffer
		/// </summary>
		public uint SampleDuration { get; set; }

		/// <summary>
		/// Battery Voltage in mV.
		/// </summary>
		public uint BatteryVoltage { get; set; }
	}
}