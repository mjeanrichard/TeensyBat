using System;

namespace TeensyBatExplorer.Core.Infrastructure
{
    public class StackableProgress : Progress<CountProgress>, IProgress<CountProgress>
    {
        private readonly StackableProgress _parentProgress;
        private readonly int? _progressSpan;
        private CountProgress _lastProgress = new CountProgress();

        public CountProgress LastProgress => _lastProgress;

        public StackableProgress(StackableProgress parentProgress, int progressSpan)
        {
            _parentProgress = parentProgress;
            _progressSpan = progressSpan;
            _lastProgress = new CountProgress { Text = parentProgress._lastProgress.Text, Current = 0, Total = 100 };
        }

        public StackableProgress(Action<CountProgress> handler) : base(handler)
        {
        }

        public void Report(string message, int current, int total)
        {
            _lastProgress = new CountProgress { Current = current, Total = total, Text = message };
            OnReport(_lastProgress);
        }

        protected override void OnReport(CountProgress value)
        {
            if (_parentProgress != null && _progressSpan.HasValue)
            {
                double factor = value.Current / (double)value.Total;
                int current = _parentProgress._lastProgress.Current + (int)Math.Round(_progressSpan.Value * factor);
                _parentProgress.OnReport(new CountProgress { Text = value.Text, Current = current, Total = _parentProgress._lastProgress.Total });
            }
            else
            {
                base.OnReport(value);
            }
        }

        void IProgress<CountProgress>.Report(CountProgress value) { OnReport(value); }

        public void Report(int current, int total)
        {
            Report(_lastProgress.Text, current, total);
        }

        public void Report(int current)
        {
            Report(_lastProgress.Text, current, _lastProgress.Total);
        }

    }
}