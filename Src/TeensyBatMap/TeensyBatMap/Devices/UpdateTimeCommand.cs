using System;
using System.Globalization;

namespace TeensyBatMap.Devices
{
	public class UpdateTimeCommand : DeviceCommand<bool?>
	{
		private static readonly DateTime EpochStart = new DateTime(1970, 1, 1);

		protected override void ExecuteInternal(TeensyBatDevice device)
		{
			int totalSeconds = (int)Math.Round((DateTime.UtcNow - EpochStart).TotalSeconds);
			totalSeconds = totalSeconds + 1;
			device.Send(string.Format(CultureInfo.InvariantCulture, "CT{0}\n", totalSeconds));
		}

		protected override bool? HandleLine(TeensyBatDevice device, string data)
		{
			if (data.StartsWith("OK: ", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			if (data.StartsWith("ERR: ", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			return null;
		}
	}
}