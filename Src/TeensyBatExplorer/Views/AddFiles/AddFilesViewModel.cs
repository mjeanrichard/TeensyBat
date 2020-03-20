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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.Pickers;

using Microsoft.Toolkit.Uwp.Helpers;

using TeensyBatExplorer.Business;
using TeensyBatExplorer.Business.Commands;
using TeensyBatExplorer.Business.Models;
using TeensyBatExplorer.Helpers.ViewModels;
using TeensyBatExplorer.Services;

namespace TeensyBatExplorer.Views.AddFiles
{
    public class AddFilesViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly LogReader _logReader;
        private readonly ProjectManager _projectManager;
        private readonly AddLogsCommand _addLogsCommand;
        private readonly List<IStorageFile> _filesToAdd = new List<IStorageFile>();
        private BatProject _batProject;

        public AddFilesViewModel(NavigationService navigationService, LogReader logReader, ProjectManager projectManager, AddLogsCommand addLogsCommand)
        {
            _navigationService = navigationService;
            _logReader = logReader;
            _projectManager = projectManager;
            _addLogsCommand = addLogsCommand;
            AddCommand = new AsyncCommand(AddLogs, this);
            SaveCommand = new AsyncCommand(SaveLogs, this);

            BatLogs = new ObservableCollection<BatLogViewModel>();
        }

        public AsyncCommand SaveCommand { get; set; }

        public AsyncCommand AddCommand { get; }

        public BatProject BatProject
        {
            get => _batProject;
            set => Set(ref _batProject, value);
        }

        public ObservableCollection<BatLogViewModel> BatLogs { get; }

        private async Task SaveLogs()
        {
            using (BusyState busyState = await MarkBusy())
            {
                Progress<CountProgress> progress = new Progress<CountProgress>(async p => await busyState.Update(p).ConfigureAwait(false));
                await _addLogsCommand.ExecuteAsync(_projectManager, BatLogs.Select(v => v.Log), progress, CancellationToken.None);
            }
        }

        private async Task AddLogs()
        {
            using (await MarkBusy())
            {
                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.ViewMode = PickerViewMode.List;
                openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
                openPicker.FileTypeFilter.Add(".dat");
                IReadOnlyList<StorageFile> files = await DispatcherHelper.ExecuteOnUIThreadAsync(async () => await openPicker.PickMultipleFilesAsync());
                if (files.Count > 0)
                {
                    foreach (StorageFile file in files)
                    {
                        _filesToAdd.Add(file);
                        BatLog batLog = new BatLog();
                        batLog.Filename = file.Name;
                        await _logReader.Load(file, batLog);
                        await RunOnUiThread(() => BatLogs.Add(new BatLogViewModel(batLog)));
                    }
                }
            }
        }


        protected override async Task LoadData()
        {
            await AddLogs();
        }
    }
}