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
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using MaterialDesignThemes.Wpf;

using Nito.Mvvm;

using TeensyBatExplorer.Core;
using TeensyBatExplorer.Core.Models;
using TeensyBatExplorer.Core.Queries;
using TeensyBatExplorer.WPF.Annotations;
using TeensyBatExplorer.WPF.Infrastructure;

namespace TeensyBatExplorer.WPF.Views.Project
{
    public class ProjectPageViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly ProjectManager _projectManager;
        private readonly Func<NodeViewModel> _nodeViewModelFactory;
        private IEnumerable<NodeViewModel> _nodes;

        public ProjectPageViewModel(NavigationService navigationService, ProjectManager projectManager, Func<NodeViewModel> nodeViewModelFactory)
        {
            _navigationService = navigationService;
            _projectManager = projectManager;
            _nodeViewModelFactory = nodeViewModelFactory;

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
            //SaveProjectCommand command = new SaveProjectCommand();
            //await command.ExecuteAsyc(BatProject, _projectFile);
        }

        public override async Task Load()
        {
            using (BusyState busyState = BeginBusy("Lade Projekt..."))
            {
                List<BatNode> nodes = await _projectManager.GetNodes(busyState.Token);
                Nodes = nodes.Select(n =>
                {
                    NodeViewModel vm = _nodeViewModelFactory();
                    vm.Load(n, this);
                    return vm;
                });
                Title = $"Projekt {_projectManager.Project.Name}";
            }
        }

        public override Task Initialize()
        {
            BatProject = _projectManager.Project;
            return Task.CompletedTask;
        }
    }

    public class NodeViewModel : INotifyPropertyChanged
    {
        private readonly NavigationService _navigationService;
        private BatNode _node;
        private BaseViewModel _parentViewModel;

        public NodeViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
            OpenNodeCommand = new AsyncCommand(OpenNode);
        }

        private async Task OpenNode()
        {
            await _navigationService.NavigateToNodeDetailPage(_node.NodeNumber);
        }

        public AsyncCommand OpenNodeCommand { get; set; }

        public void Load(BatNode batNode, BaseViewModel parentViewModel)
        {
            _node = batNode;
            _parentViewModel = parentViewModel;

            OnPropertyChanged(nameof(NodeNumber));
        }

        public int NodeNumber => _node.NodeNumber;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}