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
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.Pickers;

using Microsoft.Toolkit.Uwp.Helpers;

using TeensyBatExplorer.Commands;
using TeensyBatExplorer.Helpers.ViewModels;
using TeensyBatExplorer.Services;
using TeensyBatExplorer.Services.Project;

namespace TeensyBatExplorer.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ProjectManager _projectManager;
        private readonly NavigationService _navigationService;

        public MainViewModel(ProjectManager projectManager, NavigationService navigationService)
        {
            _projectManager = projectManager;
            _navigationService = navigationService;
            CreateNewProjectCommand = new AsyncCommand(CreateNewProject, this);
            OpenProjectCommand = new AsyncCommand(OpenProject, this);
        }

        public AsyncCommand OpenProjectCommand { get; set; }

        private async Task CreateNewProject()
        {
            using (MarkBusy())
            {
                FileSavePicker savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.Desktop;
                savePicker.DefaultFileExtension = ".batproj";
                savePicker.FileTypeChoices.Add("Bat Project", new List<string>() { ".batproj" });
                StorageFile storageFile = await DispatcherHelper.ExecuteOnUIThreadAsync(async () => await savePicker.PickSaveFileAsync());
                await new CreateProjectCommand().ExecuteAsyc(storageFile);
            }
        }

        private async Task OpenProject()
        {
            using (MarkBusy())
            {
                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.ViewMode = PickerViewMode.List;
                openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
                openPicker.FileTypeFilter.Add(".batproj");
                StorageFile file = await DispatcherHelper.ExecuteOnUIThreadAsync(async () => await openPicker.PickSingleFileAsync());
                //_navigationService.
            }
        }


        public AsyncCommand CreateNewProjectCommand { get; set; }
    }
}