// 
// Teensy Bat Explorer - Copyright(C) 2018 Meinrad Jean-Richard
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
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Toolkit.Uwp.Helpers;

using TeensyBatExplorer.Business.Commands;

namespace TeensyBatExplorer.Helpers.ViewModels
{
    public abstract class BaseViewModel : Observable
    {
        private int _busyCounter;

        protected BaseViewModel()
        {
            Busy = BusyState.Idle;
        }

        protected bool IsInitialized { get; set; }

        public BusyState Busy { get; set; }

        protected virtual Task InitializeInternalAsync()
        {
            return Task.CompletedTask;
        }

        public async Task Initialize()
        {
            if (IsInitialized)
            {
                return;
            }
            await InitializeInternalAsync();
            await Task.Run(LoadData);
            IsInitialized = true;
        }

        public virtual Task Refresh()
        {
            return Task.CompletedTask;
        }

        protected virtual Task LoadData()
        {
            return Task.CompletedTask;
        }

        public async Task<BusyState> MarkBusy(string message = null)
        {
            return await RunOnUiThread(() => new BusyState(this, message));
        }

        public async Task RunBusy(Func<Task> action, string message)
        {
            using (await MarkBusy(message))
            {
                await action();
            }
        }

        public virtual Task Leave()
        {
            return Task.CompletedTask;
        }

        public virtual Task Suspending()
        {
            return Task.CompletedTask;
        }

        public virtual Task Resuming()
        {
            return Task.CompletedTask;
        }

        public void DecrementBusyCounter(BusyState oldBusyState)
        {
            Interlocked.Decrement(ref _busyCounter);
            Busy = oldBusyState;
            BusyChanged();
        }

        public void IncrementBusyCounter(BusyState busyState)
        {
            Interlocked.Increment(ref _busyCounter);
            Busy = busyState;
            BusyChanged();
        }

        public void BusyChanged()
        {
            OnPropertyChanged(nameof(Busy));
        }

        protected async Task<T> RunOnUiThread<T>(Func<Task<T>> action)
        {
            return await DispatcherHelper.ExecuteOnUIThreadAsync(action).ConfigureAwait(false);
        }

        protected async Task<T> RunOnUiThread<T>(Func<T> action)
        {
            return await DispatcherHelper.ExecuteOnUIThreadAsync(action).ConfigureAwait(false);
        }

        protected async Task RunOnUiThread(Action action)
        {
            await DispatcherHelper.ExecuteOnUIThreadAsync(action).ConfigureAwait(false);
        }
    }

    public class BusyState : IDisposable
    {
        public static readonly BusyState Idle = new BusyState(null);

        private readonly BaseViewModel _baseViewModel;
        private readonly BusyState _oldBusyState;
        private string _message;
        private int _maxProgressValue;
        private int _progressValue;


        public BusyState(BaseViewModel baseViewModel, string message = null)
        {
            _baseViewModel = baseViewModel;
            _message = message;

            if (baseViewModel != null)
            {
                _oldBusyState = baseViewModel.Busy;
                _baseViewModel.IncrementBusyCounter(this);
            }
        }

        public BusyState(BaseViewModel baseViewModel, int maxProgressValue, string message = null) : this(baseViewModel, message)
        {
            _maxProgressValue = maxProgressValue;
            _progressValue = 0;
        }

        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                _baseViewModel.BusyChanged();
            }
        }

        public int MaxProgressValue
        {
            get { return _maxProgressValue; }
            set
            {
                _maxProgressValue = value;
                _baseViewModel.BusyChanged();
            }
        }

        public int ProgressValue
        {
            get { return _progressValue; }
            set
            {
                _progressValue = value;
                _baseViewModel.BusyChanged();
            }
        }

        public async void Dispose()
        {
            await DispatcherHelper.ExecuteOnUIThreadAsync(() => _baseViewModel.DecrementBusyCounter(_oldBusyState)).ConfigureAwait(false);
        }

        public async Task Update(CountProgress countProgress)
        {
            await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
            {
                MaxProgressValue = countProgress.Total;
                ProgressValue = countProgress.Current;
                Message = countProgress.Text;
            }).ConfigureAwait(false);
        }
    }
}