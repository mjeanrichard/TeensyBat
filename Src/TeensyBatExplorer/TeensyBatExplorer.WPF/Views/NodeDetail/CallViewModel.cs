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
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Nito.Mvvm;

using TeensyBatExplorer.Core.Commands;
using TeensyBatExplorer.Core.Models;
using TeensyBatExplorer.WPF.Annotations;
using TeensyBatExplorer.WPF.Controls;

namespace TeensyBatExplorer.WPF.Views.NodeDetail
{
    public class CallViewModel : INotifyPropertyChanged
    {
        private readonly Func<UpdateCallCommand> _updateCallCommandFactory;
        private readonly CallFilter _callFilter;

        public CallViewModel(BatCall call, Func<UpdateCallCommand> updateCallCommandFactory, CallFilter callFilter)
        {
            _updateCallCommandFactory = updateCallCommandFactory;
            _callFilter = callFilter;
            Call = call;
            UpdateIsBatCommand = new AsyncCommand(o => UpdateIsBat((bool)o));
        }

        public BatCall Call { get; private set; }

        public bool IsFiltered => _callFilter.Pass(Call);

        public int Duration => (int)(Call.DurationMicros / 1000);
        public string Time => Call.StartTime.ToString("dd.MM. HH:mm:ss");

        public AsyncCommand UpdateIsBatCommand { get; private set; }

        private async Task UpdateIsBat(bool isChecked)
        {
            Call.IsBat = isChecked;
            await _updateCallCommandFactory().ExecuteAsync(Call);
            OnPropertyChanged(nameof(Call));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SetEntries(List<BatDataFileEntry> entries)
        {
            Call.Entries = entries;
            OnPropertyChanged(nameof(Call));
            OnPropertyChanged(nameof(IsFiltered));
        }
    }
}