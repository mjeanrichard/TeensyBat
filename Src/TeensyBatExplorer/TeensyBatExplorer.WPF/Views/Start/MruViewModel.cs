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

using System.Threading.Tasks;

using Nito.Mvvm;

using TeensyBatExplorer.Core;
using TeensyBatExplorer.Core.Infrastructure;
using TeensyBatExplorer.WPF.Infrastructure;

namespace TeensyBatExplorer.WPF.Views.Start
{
    public class MruViewModel
    {
        private readonly ProjectMruEntry _projectMruEntry;
        private readonly ProjectManager _projectManager;
        private readonly BaseViewModel _parentViewModel;
        private readonly NavigationService _navigationService;

        public MruViewModel(ProjectMruEntry projectMruEntry, ProjectManager projectManager, BaseViewModel parentViewModel, NavigationService navigationService)
        {
            _projectMruEntry = projectMruEntry;
            _projectManager = projectManager;
            _parentViewModel = parentViewModel;
            _navigationService = navigationService;
            OpenCommand = new AsyncCommand(Open);
        }

        public AsyncCommand OpenCommand { get; private set; }

        public string? Name => _projectMruEntry.ProjectName;
        public string? Filename => _projectMruEntry.FullPath;
        public string LastAccess => _projectMruEntry.LastAccessTime.ToFormattedString();

        private async Task Open()
        {
            if (string.IsNullOrWhiteSpace(_projectMruEntry.FullPath))
            {
                return;
            }

            using (_parentViewModel.BeginBusy("Öffne Projekt..."))
            {
                await Task.Run(async () => await _projectManager.OpenProject(_projectMruEntry.FullPath));
                await _navigationService.NavigateToProjectPage();
            }
        }
    }
}