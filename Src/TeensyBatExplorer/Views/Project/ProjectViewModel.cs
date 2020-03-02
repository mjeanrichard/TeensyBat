// 
// Teensy Bat Explorer - Copyright(C) 2019 Meinrad Jean-Richard
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
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Navigation;

using Microsoft.Toolkit.Uwp.Helpers;

using TeensyBatExplorer.Business.Commands;
using TeensyBatExplorer.Business.Models;
using TeensyBatExplorer.Business.Queries;
using TeensyBatExplorer.Helpers.ViewModels;
using TeensyBatExplorer.Services;

namespace TeensyBatExplorer.Views.Project
{
    public class ProjectViewModel : BaseViewModel
    {
        private readonly Func<AddLogsCommand> _addLogCommandFactory;
        private readonly NavigationService _navigationService;
        private readonly IStorageFile _projectFile;
        private BatProject _batProject;

        public ProjectViewModel(NavigationEventArgs navigationEventArgs, Func<AddLogsCommand> addLogCommandFactory, NavigationService navigationService)
        {
            _addLogCommandFactory = addLogCommandFactory;
            _navigationService = navigationService;
            _projectFile = (IStorageFile)navigationEventArgs.Parameter;
            SaveCommand = new AsyncCommand(SaveProject, this);
            AddLogCommand = new AsyncCommand(AddLog, this);
            OpenLogCommand = new AsyncCommand(OpenLog, this);
        }

        private async Task OpenLog()
        {
            await _navigationService.NavigateToLogPage(_projectFile);
        }

        public AsyncCommand OpenLogCommand { get; set; }

        public AsyncCommand SaveCommand { get; }
        public AsyncCommand AddLogCommand { get; }

        public BatProject BatProject
        {
            get => _batProject;
            set => Set(ref _batProject, value);
        }

        private async Task AddLog()
        {
            using (BusyState busyState = MarkBusy())
            {
                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.ViewMode = PickerViewMode.List;
                openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
                openPicker.FileTypeFilter.Add(".dat");
                IReadOnlyList<StorageFile> files = await DispatcherHelper.ExecuteOnUIThreadAsync(async () => await openPicker.PickMultipleFilesAsync());
                if (files.Count > 0)
                {
                    AddLogsCommand command = _addLogCommandFactory();
                    await command.ExecuteAsync(_projectFile, files, busyState);
                }
            }
        }

        private async Task SaveProject()
        {
            SaveProjectCommand command = new SaveProjectCommand();
            await command.ExecuteAsyc(BatProject, _projectFile);
        }

        protected override async Task LoadData()
        {
            GetProjectQuery query = new GetProjectQuery();
            BatProject = await query.Execute(_projectFile);
        }
    }
}