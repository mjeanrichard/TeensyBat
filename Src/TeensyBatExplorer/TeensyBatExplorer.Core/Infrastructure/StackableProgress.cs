// 
// Teensy Bat Explorer - Copyright(C) 2020 Meinrad Jean-Richard
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;

namespace TeensyBatExplorer.Core.Infrastructure
{
    public class StackableProgress : Progress<CountProgress>, IProgress<CountProgress>
    {
        public static readonly StackableProgress NoProgress = new(p => { });

        private readonly StackableProgress? _parentProgress;
        private readonly int? _progressSpan;
        private CountProgress _lastProgress = new();
        private readonly int _initialParentValue;

        public StackableProgress(StackableProgress parentProgress, int progressSpan)
        {
            _parentProgress = parentProgress;
            _progressSpan = progressSpan;
            _lastProgress = new CountProgress { Text = parentProgress._lastProgress.Text, Current = 0, Total = 100 };
            _initialParentValue = _parentProgress._lastProgress.Current;
        }

        public StackableProgress(Action<CountProgress> handler) : base(handler)
        {
        }

        public CountProgress LastProgress => _lastProgress;

        public void Report(string? message, int current, int total)
        {
            _lastProgress = new CountProgress { Current = current, Total = total, Text = message };
            OnReport(_lastProgress);
        }

        protected override void OnReport(CountProgress value)
        {
            if (_parentProgress != null && _progressSpan.HasValue)
            {
                double factor = value.Current / (double)value.Total;
                int current = _initialParentValue + (int)Math.Round(_progressSpan.Value * factor);
                _parentProgress.OnReport(new CountProgress { Text = value.Text, Current = current, Total = _parentProgress._lastProgress.Total });
            }
            else
            {
                _lastProgress = value;
                base.OnReport(value);
            }
        }

        void IProgress<CountProgress>.Report(CountProgress value)
        {
            OnReport(value);
        }

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