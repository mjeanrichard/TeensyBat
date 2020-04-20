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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using TeensyBatExplorer.Core.Commands;
using TeensyBatExplorer.Core.Infrastructure;
using TeensyBatExplorer.WPF.Annotations;

namespace TeensyBatExplorer.WPF.Infrastructure
{
    public class BusyState : IDisposable, INotifyPropertyChanged
    {
        private readonly BaseViewModel _baseViewModel;
        private readonly CancellationTokenSource _cancellationToken;
        private int? _total;
        private int? _current;
        private string _text;

        public BusyState(BaseViewModel baseViewModel)
        {
            _baseViewModel = baseViewModel;
            _cancellationToken = new CancellationTokenSource();
        }

        public bool IsCancellationRequested => _cancellationToken.IsCancellationRequested;

        public CancellationToken Token => _cancellationToken.Token;

        public bool IsIndeterminate => !_total.HasValue;

        public int? Total
        {
            get => _total;
            set
            {
                _total = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsIndeterminate));
            }
        }

        public int? Current
        {
            get => _current;
            set
            {
                _current = value;
                OnPropertyChanged();
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                OnPropertyChanged();
            }
        }

        public void Cancel()
        {
            _cancellationToken.Cancel();
        }

        public void Dispose()
        {
            _cancellationToken.Dispose();
            _baseViewModel.EndBusy(this);
        }

        public Task Update(CountProgress countProgress)
        {
            return Update(countProgress.Text, countProgress.Current, countProgress.Total);
        }


        public async Task Update(string text, int? current, int? total)
        {
            await _baseViewModel.RunOnUiThreadAsync(() =>
            {
                if (text != null)
                {
                    Text = text;
                }

                Total = total;
                Current = current;
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public StackableProgress GetProgress(int total = 100)
        {
            StackableProgress progress = new StackableProgress(async p => await Update(p));
            progress.Report(Text, 0, total);
            return progress;
        }
    }
}