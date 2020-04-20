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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MapControl;

using MaterialDesignThemes.Wpf;

using TeensyBatExplorer.Core;
using TeensyBatExplorer.Core.Commands;
using TeensyBatExplorer.Core.Models;
using TeensyBatExplorer.Core.Queries;
using TeensyBatExplorer.WPF.Infrastructure;

namespace TeensyBatExplorer.WPF.Views.Project
{
    public class ProjectPageViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly ProjectManager _projectManager;
        private readonly Func<NodeViewModel> _nodeViewModelFactory;
        private readonly SaveProjectCommand _saveProjectCommand;
        private IEnumerable<NodeViewModel> _nodes;

        public ProjectPageViewModel(NavigationService navigationService, ProjectManager projectManager, Func<NodeViewModel> nodeViewModelFactory, SaveProjectCommand saveProjectCommand)
        {
            _navigationService = navigationService;
            _projectManager = projectManager;
            _nodeViewModelFactory = nodeViewModelFactory;
            _saveProjectCommand = saveProjectCommand;

            AddToolbarButton(new ToolBarButton(AddLog, PackIconKind.PlusBoxMultipleOutline, "Logs hinzufügen"));
            AddToolbarButton(new ToolBarButton(SaveProject, PackIconKind.ContentSave, "Speichern"));

            Title = "Projekt";
        }

        public BatProject BatProject { get; private set; }

        public IEnumerable<NodeViewModel> Nodes
        {
            get => _nodes;
            private set
            {
                _nodes = value;
                OnPropertyChanged();
            }
        }

        private async Task AddLog()
        {
            await _navigationService.NavigateToAddLogsPage();
        }

        private async Task SaveProject()
        {
            using (BusyState busyState = BeginBusy("Speichere Projekt"))
            {
                await _saveProjectCommand.Execute(_projectManager, BatProject, busyState.Token);
            }
        }

        public override async Task Load()
        {
            using (BusyState busyState = BeginBusy("Lade Projekt..."))
            {
                Title = $"Projekt {_projectManager.Project.Name}";

                List<BatNode> nodes = await _projectManager.GetNodes(busyState.Token);
                List<NodeViewModel> vmNodes = new List<NodeViewModel>(nodes.Count);
                foreach (BatNode node in nodes)
                {
                    NodeViewModel vm = _nodeViewModelFactory();
                    await vm.Load(node, this);
                    vmNodes.Add(vm);
                }

                Nodes = vmNodes;
            }
        }

        public override Task Initialize()
        {
            BatProject = _projectManager.Project;
            return Task.CompletedTask;
        }
    }
}