using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TeensyBatMap.Devices
{
	public class GetInfoCommand : DeviceCommand<DeviceInfo>
	{
		private static readonly Regex ConfigData = new Regex(@"^CFG:NodeId:[\s]+(?<NodeId>[0-9]+)\s*;\s*Time:\s+(?<Date>[0-9:\. ]+)$", RegexOptions.Compiled);

		protected override void ExecuteInternal(TeensyBatDevice device)
		{
			device.Send("CP\n");
		}

		protected override DeviceInfo HandleLine(TeensyBatDevice device, string data)
		{
			Match match = ConfigData.Match(data);
			if (match.Success)
			{
				DeviceInfo info = new DeviceInfo();
				info.NodeId = int.Parse(match.Groups["NodeId"].Value, CultureInfo.InvariantCulture);
				info.CurrentDate = DateTime.ParseExact(match.Groups["Date"].Value, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
				info.RecordingTime = DateTime.UtcNow;
				return info;
			}
			return null;
		}
	}

	public class DeviceInfo
	{
		public int NodeId { get; set; }
		public DateTime CurrentDate { get; set; }
		public DateTime RecordingTime { get; set; }

		public double TimeDeltaSeconds
		{
			get { return (RecordingTime - CurrentDate).TotalSeconds; }
		}
	}
}