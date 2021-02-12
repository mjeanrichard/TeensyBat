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
using System.Threading.Tasks;

using MaterialDesignThemes.Wpf;

using TeensyBatExplorer.Core;
using TeensyBatExplorer.Core.Commands;
using TeensyBatExplorer.Core.Infrastructure;
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
        private readonly AnalyzeNodeCommand _analyzeNodeCommand;
        private IEnumerable<NodeViewModel>? _nodes;

        public ProjectPageViewModel(NavigationService navigationService, ProjectManager projectManager, Func<NodeViewModel> nodeViewModelFactory, SaveProjectCommand saveProjectCommand, AnalyzeNodeCommand analyzeNodeCommand)
        {
            _navigationService = navigationService;
            _projectManager = projectManager;
            _nodeViewModelFactory = nodeViewModelFactory;
            _saveProjectCommand = saveProjectCommand;
            _analyzeNodeCommand = analyzeNodeCommand;

            AddToolbarButton(new ToolBarButton(AddLog, PackIconKind.PlusBoxMultipleOutline, "Logs hinzufügen"));
            AddToolbarButton(new ToolBarButton(SaveProject, PackIconKind.ContentSave, "Speichern"));
            AddToolbarButton(new ToolBarButton(AnalyzeAllNodes, PackIconKind.Cogs, "Alle Geräte analysieren"));

            Title = "Projekt";
        }

        public BatProject? BatProject { get; private set; }

        public IEnumerable<NodeViewModel>? Nodes
        {
            get => _nodes;
            private set
            {
                _nodes = value;
                OnPropertyChanged();
            }
        }

        private async Task AnalyzeAllNodes()
        {
            if (Nodes == null)
            {
                return;
            }

            using (BusyState busyState = BeginBusy("Analysiere Geräte..."))
            {
                StackableProgress progress = busyState.GetProgress();
                int i = 0;
                foreach (NodeViewModel node in Nodes)
                {
                    if (node.Node == null)
                    {
                        throw new InvalidOperationException("Node not loaded.");
                    }

                    progress.Report($"Analysiere Gerät {node.NodeNumber}...", i++, Nodes.Count() + 2);
                    await _analyzeNodeCommand.Process(node.Node.Id, progress.Stack(1), busyState.Token);
                }

                await LoadNodes(busyState, progress.Stack(2));
            }
        }

        private async Task AddLog()
        {
            await _navigationService.NavigateToAddLogsPage();
        }

        private async Task SaveProject()
        {
            if (BatProject == null)
            {
                return;
            }

            using (BusyState busyState = BeginBusy("Speichere Projekt"))
            {
                await _saveProjectCommand.Execute(_projectManager, BatProject, busyState.Token);
            }
        }

        public override async Task Load()
        {
            using (BusyState busyState = BeginBusy("Lade Projekt..."))
            {
                StackableProgress progress = busyState.GetProgress();
                Title = $"Projekt {_projectManager.Project?.Name}";
                await LoadNodes(busyState, progress);
            }
        }

        private async Task LoadNodes(BusyState busyState, StackableProgress progress)
        {
            List<BatNode> nodes = await _projectManager.GetNodes(busyState.Token);
            List<NodeViewModel> vmNodes = new(nodes.Count);
            int i = 0;
            foreach (BatNode node in nodes)
            {
                progress.Report(i, nodes.Count);
                NodeViewModel vm = _nodeViewModelFactory();
                await vm.Load(node, this);
                vmNodes.Add(vm);
                i++;
            }

            Nodes = vmNodes;
        }

        public override Task Initialize()
        {
            BatProject = _projectManager.Project;
            return Task.CompletedTask;
        }
    }
}