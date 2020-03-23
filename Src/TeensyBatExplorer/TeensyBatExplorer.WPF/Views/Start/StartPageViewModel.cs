// 
// Teensy Bat Explorer - Copyright(C) 2020 Meinard Jean-Richard
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

using MaterialDesignThemes.Wpf;

using Microsoft.Win32;

using Nito.Mvvm;

using TeensyBatExplorer.Core;
using TeensyBatExplorer.WPF.Infrastructure;

namespace TeensyBatExplorer.WPF.Views.Start
{
    internal class StartPageViewModel : BaseViewModel
    {
        private readonly ProjectManager _projectManager;
        private readonly NavigationService _navigationService;

        public StartPageViewModel(ProjectManager projectManager, NavigationService navigationService)
        {
            _projectManager = projectManager;
            _navigationService = navigationService;

            AddToolbarButton(new ToolBarButton(OpenProject, PackIconKind.FolderOpenOutline, "Open"));
            AddToolbarButton(new ToolBarButton(CreateNewProject, PackIconKind.FolderAddOutline, "Create new"));
        }

        private async Task CreateNewProject()
        {
            using (BeginBusy())
            {
                SaveFileDialog savePicker = new SaveFileDialog();
                savePicker.CheckPathExists = true;
                savePicker.DefaultExt = ".batproj";
                bool? dialogResult = savePicker.ShowDialog();
                if (dialogResult.HasValue && dialogResult.Value)
                {
                    await _projectManager.CreateNewProject(savePicker.FileName);
                    await _navigationService.NavigateToProjectPage();
                }
            }
        }

        private async Task OpenProject()
        {
            using (BeginBusy())
            {
                OpenFileDialog openPicker = new OpenFileDialog();
                openPicker.Multiselect = false;
                openPicker.DefaultExt = ".batproj";
                openPicker.CheckFileExists = true;
                bool? pickerResult = openPicker.ShowDialog();
                if (pickerResult.HasValue && pickerResult.Value)
                {
                    await _projectManager.OpenProject(openPicker.FileName);
                    await _navigationService.NavigateToProjectPage();
                }
            }
        }
    }
}