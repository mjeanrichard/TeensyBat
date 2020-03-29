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

using System.Collections.Generic;
using System.Threading.Tasks;

using MaterialDesignThemes.Wpf;

using Nito.Mvvm;

using TeensyBatExplorer.Core;
using TeensyBatExplorer.Core.Models;
using TeensyBatExplorer.Core.Queries;
using TeensyBatExplorer.WPF.Infrastructure;

namespace TeensyBatExplorer.WPF.Views.Project
{
    public class ProjectPageViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly ProjectManager _projectManager;
        private IEnumerable<BatNode> _nodes;

        public ProjectPageViewModel(NavigationService navigationService, ProjectManager projectManager)
        {
            _navigationService = navigationService;
            _projectManager = projectManager;

            OpenNodeCommand = new AsyncCommand(async o => await OpenLog((int)o));

            AddToolbarButton(new ToolBarButton(AddLog, PackIconKind.PlusBoxMultipleOutline, "Logs hinzufügen"));
            AddToolbarButton(new ToolBarButton(SaveProject, PackIconKind.ContentSave, "Speichern"));
        }

        public AsyncCommand OpenNodeCommand { get; set; }

        public BatProject BatProject { get; private set; }

        public IEnumerable<BatNode> Nodes
        {
            get => _nodes;
            private set
            {
                _nodes = value;
                OnPropertyChanged();
            }
        }

        private async Task OpenLog(int nodeNumber)
        {
            //await _navigationService.NavigateToNodePage(nodeNumber);
        }

        private async Task AddLog()
        {
            await _navigationService.NavigateToAddLogsPage();
        }

        private async Task SaveProject()
        {
            //SaveProjectCommand command = new SaveProjectCommand();
            //await command.ExecuteAsyc(BatProject, _projectFile);
        }

        public override async Task Load()
        {
            Nodes = await Task.Run(async () => await _projectManager.GetNodes());
        }

        public override Task Initialize()
        {
            BatProject = _projectManager.Project;
            return Task.CompletedTask;
        }
    }
}