using System;

namespace TeensyBatExplorer.Core.Infrastructure
{
    public static class ProgressHelper
    {
        public static void Report(this IProgress<CountProgress> progress, string message, int current, int total)
        {
            progress.Report(new CountProgress { Current = current, Total = total, Text = message });
        }

        public static void Report(this IProgress<CountProgress> progress, int current, int total)
        {
            progress.Report(new CountProgress { Current = current, Total = total });
        }

        public static StackableProgress Stack(this StackableProgress progress, int progressSpan)
        {
            return new StackableProgress(progress, progressSpan);
        }
    }
}