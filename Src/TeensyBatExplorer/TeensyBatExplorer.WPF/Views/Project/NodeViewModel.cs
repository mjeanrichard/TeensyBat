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

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Nito.Mvvm;

using TeensyBatExplorer.Core;
using TeensyBatExplorer.Core.Commands;
using TeensyBatExplorer.Core.Models;
using TeensyBatExplorer.Core.Queries;
using TeensyBatExplorer.WPF.Annotations;
using TeensyBatExplorer.WPF.Infrastructure;
using TeensyBatExplorer.WPF.Themes;

namespace TeensyBatExplorer.WPF.Views.Project
{
    public class NodeViewModel : INotifyPropertyChanged
    {
        private readonly NavigationService _navigationService;
        private readonly ProjectManager _projectManager;
        private BatNode _node;
        private ProjectPageViewModel _parentViewModel;

        public NodeViewModel(NavigationService navigationService, ProjectManager projectManager)
        {
            _navigationService = navigationService;
            _projectManager = projectManager;
            OpenNodeCommand = new AsyncCommand(OpenNode);
            DeleteNodeCommand = new AsyncCommand(DeleteNode);
        }

        public AsyncCommand DeleteNodeCommand { get; set; }

        public AsyncCommand OpenNodeCommand { get; set; }

        private async Task DeleteNode()
        {
                YesNoDialogViewModel dialog = new YesNoDialogViewModel($"Soll das Gerät {_node.NodeNumber} wirklich entfernt werden?", _parentViewModel);
                DialogResult result = await dialog.Open();
                if (result == DialogResult.Yes)
                {
                    using (BusyState beginBusy = _parentViewModel.BeginBusy("Gerät entfernen..."))
                    {
                        DeleteNodeCommand cmd = new DeleteNodeCommand();
                        await cmd.ExecuteAsync(_projectManager, _node.Id, beginBusy.GetProgress(), beginBusy.Token);
                    }

                    await _parentViewModel.Load();
                }
        }

        public int NodeNumber => _node.NodeNumber;
        public string StartDatum => _node.StartTime.ToFormattedString();
        public int CallCount { get; private set; }

        public int FileCount { get; private set; }

        private async Task OpenNode()
        {
            await _navigationService.NavigateToNodeDetailPage(_node.NodeNumber);
        }

        public async Task Load(BatNode batNode, ProjectPageViewModel parentViewModel)
        {
            _node = batNode;
            _parentViewModel = parentViewModel;

            CallCount = await _projectManager.GetCallCount(_node.Id, CancellationToken.None);
            FileCount = await _projectManager.GetDataFileCount(_node.Id, CancellationToken.None);

            OnPropertyChanged(nameof(NodeNumber));
            OnPropertyChanged(nameof(StartDatum));
            OnPropertyChanged(nameof(CallCount));
            OnPropertyChanged(nameof(FileCount));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}