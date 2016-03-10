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

            int timeMs = 0;

            BatNodeLog batNodeLog = new BatNodeLog();
            batNodeLog.Id = 10;
            batNodeLog.Name = "Log 01, Aarau";
            batNodeLog.LogStart = DateTime.Now;
            List<BatCall> calls = new List<BatCall>(5000);
            for (int i = 0; i < calls.Capacity; i++)
            {
                timeMs += rnd.Next(1000);
                calls.Add(new BatCall(batNodeLog, timeMs, rnd.Next(100000), rnd.Next(50), rnd.Next(1024), rnd.Next(50), rnd.Next(1024)));
            }
            batNodeLog.SetCalls(calls);
            return batNodeLog;
        }
    }
}