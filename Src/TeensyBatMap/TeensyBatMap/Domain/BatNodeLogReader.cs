using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;

namespace TeensyBatMap.Domain
{
    public class BatNodeLogReader
    {
        private static readonly Regex TimeMarkerRegex = new Regex(@"^#RTC:\s*(?<dt>\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2})\s*$");

        public async Task<BatNodeLog> Load(IStorageFile file)
        {
            BatNodeLog log = new BatNodeLog();
            IList<string> lines = await FileIO.ReadLinesAsync(file);
            List<BatCall> calls = new List<BatCall>(lines.Count);
            foreach (string line in lines)
            {
                if (line.StartsWith("#"))
                {
                    ParseCommentLine(line, log);
                }
                else
                {
                    try {
                        BatCall batCall = ParseCall(line, log);
                        calls.Add(batCall);
                    } catch (Exception e)
                    {

                    }
                }
            }
            log.SetCalls(calls);
            return log;
        }

        private void ParseCommentLine(string commentLine, BatNodeLog log)
        {
            Match match = TimeMarkerRegex.Match(commentLine);
            if (match.Success)
            {
                string dateTime = match.Groups["dt"].Value;
                log.LogStart = DateTime.ParseExact(dateTime, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None);
            }
        }

        private BatCall ParseCall(string logLine, BatNodeLog log)
        {
            string[] parts = logLine.Split(';');

            if (parts.Length != 6)
            {
                throw new InvalidOperationException("The specified Log Line could not be parsed (Invalid Length).");
            }

            int startTimeMs = int.Parse(parts[0], CultureInfo.InvariantCulture);
            int duration = int.Parse(parts[1], CultureInfo.InvariantCulture);
            int maxFrequency = int.Parse(parts[2], CultureInfo.InvariantCulture);
            int avgFrequency = int.Parse(parts[3], CultureInfo.InvariantCulture);
            int maxIntensity = int.Parse(parts[4], CultureInfo.InvariantCulture);
            int avgIntensity = int.Parse(parts[5], CultureInfo.InvariantCulture);
            return new BatCall(log, startTimeMs, duration, maxFrequency, maxIntensity, avgFrequency, avgIntensity);
        }


    }
}