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
using System.Linq;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

using Microsoft.Toolkit.Uwp.Helpers;

using TeensyBatExplorer.Business;
using TeensyBatExplorer.Business.Commands;
using TeensyBatExplorer.Helpers.ViewModels;
using TeensyBatExplorer.Services;

namespace TeensyBatExplorer.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ProjectManager _projectManager;
        private readonly NavigationService _navigationService;
        private readonly MruService _mruService;

        public MainViewModel(ProjectManager projectManager, NavigationService navigationService, MruService mruService)
        {
            _projectManager = projectManager;
            _navigationService = navigationService;
            _mruService = mruService;
            CreateNewProjectCommand = new AsyncCommand(CreateNewProject, this);
            OpenProjectCommand = new AsyncCommand(OpenProject, this);
            OpenMruProjectCommand = new AsyncParameterCommand<string>(OpenMruProject, this);


        }

        private async Task OpenMruProject(string token)
        {
            StorageFile storageFile = await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(token, AccessCacheOptions.None);
            await _projectManager.OpenProject(storageFile);
            await _navigationService.NavigateToProjectPage();
        }

        public AsyncParameterCommand<string> OpenMruProjectCommand { get; set; }
        public AsyncCommand OpenProjectCommand { get; set; }
        public AsyncCommand CreateNewProjectCommand { get; set; }

        private async Task CreateNewProject()
        {
            using (await MarkBusy())
            {
                FileSavePicker savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.Desktop;
                savePicker.DefaultFileExtension = ".batproj";
                savePicker.FileTypeChoices.Add("Bat Project", new List<string>() { ".batproj" });
                StorageFile storageFile = await DispatcherHelper.ExecuteOnUIThreadAsync(async () => await savePicker.PickSaveFileAsync());
                if (storageFile != null)
                {
                    await _projectManager.CreateNewProject(storageFile);
                    await _navigationService.NavigateToProjectPage();
                }
            }
        }

        private async Task OpenProject()
        {
            using (await MarkBusy())
            {
                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.ViewMode = PickerViewMode.List;
                openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
                openPicker.FileTypeFilter.Add(".batproj");
                StorageFile file = await DispatcherHelper.ExecuteOnUIThreadAsync(async () => await openPicker.PickSingleFileAsync());
                if (file != null)
                {
                    await _projectManager.OpenProject(file);
                    await _navigationService.NavigateToProjectPage();
                }
            }
        }

        protected override async Task LoadData()
        {
            MruProjects = _mruService.GetProjects().ToList();
            await RunOnUiThread(() => OnPropertyChanged(nameof(MruProjects)));
        }

        public IList<ProjectMetadata> MruProjects { get; set; }
    }
}