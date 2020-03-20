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
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Navigation;

using Microsoft.Toolkit.Uwp.Helpers;

using TeensyBatExplorer.Business;
using TeensyBatExplorer.Business.Commands;
using TeensyBatExplorer.Business.Models;
using TeensyBatExplorer.Business.Queries;
using TeensyBatExplorer.Helpers.ViewModels;
using TeensyBatExplorer.Services;

namespace TeensyBatExplorer.Views.Project
{
    public class ProjectViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly ProjectManager _projectManager;
        private BatProject _batProject;
        private IEnumerable<BatNode> _nodes;

        public ProjectViewModel(NavigationService navigationService, ProjectManager projectManager)
        {
            _navigationService = navigationService;
            _projectManager = projectManager;
            SaveCommand = new AsyncCommand(SaveProject, this);
            AddLogCommand = new AsyncCommand(AddLog, this);
            OpenNodeCommand = new AsyncParameterCommand<int>(OpenLog, this);
        }

        private async Task OpenLog(int nodeNumber)
        {
            await _navigationService.NavigateToNodePage(nodeNumber);
        }

        public AsyncParameterCommand<int> OpenNodeCommand { get; set; }
        public AsyncCommand SaveCommand { get; }
        public AsyncCommand AddLogCommand { get; }

        public BatProject BatProject
        {
            get => _batProject;
            set => Set(ref _batProject, value);
        }

        public IEnumerable<BatNode> Nodes
        {
            get => _nodes;
            set => Set(ref _nodes, value);
        }

        private async Task AddLog()
        {
            await _navigationService.NavigateToAddFilesPage();
        }

        private async Task SaveProject()
        {
            //SaveProjectCommand command = new SaveProjectCommand();
            //await command.ExecuteAsyc(BatProject, _projectFile);
        }

        protected override async Task LoadData()
        {
            await RunOnUiThread(() => BatProject = _projectManager.Project);
            List<BatNode> nodes = _projectManager.GetDatabase().GetCollection<BatNode>().FindAll().ToList();
            await RunOnUiThread(() => Nodes = nodes);
        }
    }
}