// 
// Teensy Bat Explorer - Copyright(C) 2017 Meinard Jean-Richard
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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TeensyBatExplorer.Common;
using TeensyBatExplorer.Core;
using TeensyBatExplorer.Core.BatLog;

namespace TeensyBatExplorer.Views.Main
{
    public class MainViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly BatProjectManager _batProjectManager;
        private IEnumerable<ExistingProject> _projects;

        public MainViewModel(NavigationService navigationService, BatProjectManager batProjectManager) : this()
        {
            _navigationService = navigationService;
            _batProjectManager = batProjectManager;
        }


        public MainViewModel()
        {
            NewProjectCommand = new AsyncCommand(CreateNewProject, this);
        }

        public RelayCommand NewProjectCommand { get; private set; }

        public IEnumerable<ExistingProject> Projects
        {
            get { return _projects; }
            set
            {
                _projects = value;
                OnPropertyChanged();
            }
        }

        public async Task OpenProject(BatProject project)
        {
            if (project != null)
            {
                await _navigationService.OpenProject(project);
            }
        }

        private async Task CreateNewProject()
        {
            await _navigationService.GoToNewProject();
        }

        public override async Task Refresh()
        {
            await LoadData();
        }

        protected override async Task LoadData()
        {
            Projects = (await _batProjectManager.FindAllProjects()).Select(p => new ExistingProject(p, this));
        }
    }
}