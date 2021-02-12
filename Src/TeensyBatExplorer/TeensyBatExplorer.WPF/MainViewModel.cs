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
using System.Threading.Tasks;

using MaterialDesignThemes.Wpf;

using Nito.Mvvm;

using TeensyBatExplorer.Core;
using TeensyBatExplorer.WPF.Infrastructure;

namespace TeensyBatExplorer.WPF
{
    public class MainViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly ProjectManager _projectManager;
        private readonly ISnackbarMessageQueue _snackbarMessageQueue;
        private BaseViewModel? _currentPage;

        public MainViewModel(NavigationService navigationService, ProjectManager projectManager, ISnackbarMessageQueue snackbarMessageQueue)
        {
            _navigationService = navigationService;
            _projectManager = projectManager;
            _snackbarMessageQueue = snackbarMessageQueue;
            _navigationService.OnViewModelChanged += NavigationServiceOnOnViewModelChanged;

            GoHomeCommand = new AsyncCommand(GoHome);
            GoToProjectCommand = new AsyncCommand(GoToProject);
            GoToMapCommand = new AsyncCommand(GoToMap);
            GoToDeviceCommand = new AsyncCommand(GoToDevice);
            Cancel = new CustomAsyncCommand(CancelOperation, () => !IsCancellationRequested);

            CurrentPage = _navigationService.CurrentViewModel;

            _projectManager.ProjectChanged += OnProjectChanged;
        }

        public bool HasProject => _projectManager.IsProjectOpen;

        public AsyncCommand GoHomeCommand { get; set; }
        public AsyncCommand GoToProjectCommand { get; set; }
        public AsyncCommand GoToMapCommand { get; set; }
        public AsyncCommand GoToDeviceCommand { get; set; }
        public CustomAsyncCommand Cancel { get; set; }

        public ISnackbarMessageQueue SnackbarMessageQueue => _snackbarMessageQueue;

        public BaseViewModel? CurrentPage
        {
            get => _currentPage;
            private set
            {
                if (Equals(value, _currentPage))
                {
                    return;
                }

                _currentPage = value;
                OnPropertyChanged();
            }
        }

        private async Task GoToMap()
        {
            await _navigationService.NavigateToMapPage();
        }

        private async Task GoToDevice()
        {
            await _navigationService.NavigateToDevicePage();
        }

        private void OnProjectChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(HasProject));
        }

        private async Task GoHome()
        {
            await _navigationService.NavigateToStartPage();
        }

        private async Task GoToProject()
        {
            await _navigationService.NavigateToProjectPage();
        }

        private async Task CancelOperation()
        {
            if (CurrentPage != null)
            {
                await CurrentPage.CancelCurrentOperation();
            }
        }

        private void NavigationServiceOnOnViewModelChanged(object? sender, EventArgs e)
        {
            CurrentPage = _navigationService.CurrentViewModel;
        }

        public override Task Initialize()
        {
            return Task.CompletedTask;
        }
    }
}