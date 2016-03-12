using System;
using System.Collections.Generic;
using TeensyBatMap.Domain;

namespace TeensyBatMap.ViewModels
{
    public static class DesignData
    {
		public static BatNodeLog CreateBatLog()
        {
			Random rnd = new Random();

			byte[] data = new byte[512];
			rnd.NextBytes(data);

            uint timeMs = 0;

            BatNodeLog batNodeLog = new BatNodeLog();
            batNodeLog.Id = 10;
            batNodeLog.Name = "Log 01, Aarau";
            batNodeLog.LogStart = DateTime.Now;
            for (int i = 0; i < 1000; i++)
            {
                timeMs += (uint)rnd.Next(1000);
				batNodeLog.Calls.Add(new BatCall(batNodeLog, timeMs, (uint)rnd.Next(100000), (uint)rnd.Next(50), (uint)rnd.Next(1024), (uint)rnd.Next(50), (uint)rnd.Next(1024), data));
            }
	        batNodeLog.CallCount = batNodeLog.Calls.Count;
            return batNodeLog;
        }
    }
}