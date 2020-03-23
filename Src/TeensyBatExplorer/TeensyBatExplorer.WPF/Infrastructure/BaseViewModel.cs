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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using TeensyBatExplorer.WPF.Annotations;

namespace TeensyBatExplorer.WPF.Infrastructure
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        private readonly List<BusyState> _busyStates = new List<BusyState>();
        private string _title;

        public bool IsBusy
        {
            get => _busyStates.Any();
        }

        public BusyState BusyState => _busyStates.LastOrDefault();

        public List<ToolBarButton> ToolBarButtons { get; } = new List<ToolBarButton>();

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        protected void AddToolbarButton(ToolBarButton button)
        {
            ToolBarButtons.Add(button);
            OnPropertyChanged(nameof(ToolBarButtons));
        }

        public BusyState BeginBusy()
        {
            BusyState busyState = new BusyState(this);
            _busyStates.Add(busyState);
            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(BusyState));
            return busyState;
        }

        public void EndBusy(BusyState busyState)
        {
            _busyStates.Remove(busyState);
            OnPropertyChanged(nameof(IsCancellationRequested));
            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(BusyState));
        }

        public bool IsCancellationRequested => _busyStates.LastOrDefault()?.IsCancellationRequested ?? false;

        public virtual Task Initialize()
        {
            return Task.CompletedTask;
        }

        public virtual Task Load()
        {
            return Task.CompletedTask;
        }

        public async Task RunOnUiThread(Action action)
        {
            await Application.Current.Dispatcher.InvokeAsync(action);
        }

        public async Task RunOnUiThread(Func<Task> action)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () => await action());
        }


        public virtual Task CancelCurrentOperation()
        {
            if (_busyStates.Any())
            {
                _busyStates.Last().Cancel();
            }

            return Task.CompletedTask;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}