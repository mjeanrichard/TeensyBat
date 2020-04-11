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
using System.Threading.Tasks;
using System.Windows;

using MaterialDesignThemes.Wpf;

using TeensyBatExplorer.WPF.Annotations;
using TeensyBatExplorer.WPF.Infrastructure;

namespace TeensyBatExplorer.WPF.Themes
{
    public abstract class DialogViewModel<TResult> : INotifyPropertyChanged
    {
        private readonly BaseViewModel _ownerViewModel;
        private DialogSession _session;

        protected DialogViewModel(BaseViewModel ownerViewModel)
        {
            _ownerViewModel = ownerViewModel;
        }

        protected BusyState BeginBusy(string title)
        {
            return _ownerViewModel.BeginBusy(title);
        }

        public async Task<TResult> Open()
        {
            return (TResult)await DialogHost.Show(this, "RootDialog", OnDialogOpened);
        }


        public void Close(TResult value)
        {
            _session.Close(value);
        }

        private void OnDialogOpened(object sender, DialogOpenedEventArgs eventargs)
        {
            _session = eventargs.Session;
        }


        protected async Task RunOnUiThreadAsync(Action action)
        {
            await Application.Current.Dispatcher.InvokeAsync(action);
        }

        protected void RunOnUiThread(Action action)
        {
            Application.Current.Dispatcher.Invoke(action);
        }

        protected async Task RunOnUiThreadAsync(Func<Task> action)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () => await action());
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}