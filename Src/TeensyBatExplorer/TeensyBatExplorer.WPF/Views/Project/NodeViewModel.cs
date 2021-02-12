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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Nito.Mvvm;

using TeensyBatExplorer.Core;
using TeensyBatExplorer.Core.Commands;
using TeensyBatExplorer.Core.Infrastructure;
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
        private ProjectPageViewModel? _parentViewModel;

        public NodeViewModel(NavigationService navigationService, ProjectManager projectManager)
        {
            _navigationService = navigationService;
            _projectManager = projectManager;
            OpenNodeCommand = new AsyncCommand(OpenNode);
            DeleteNodeCommand = new AsyncCommand(DeleteNode);
        }

        public BatNode? Node { get; private set; }

        public AsyncCommand DeleteNodeCommand { get; set; }

        public AsyncCommand OpenNodeCommand { get; set; }

        public int? NodeNumber => Node?.NodeNumber;
        public string? StartDatum => Node?.StartTime.ToFormattedString();
        public int CallCount { get; private set; }

        public int FileCount { get; private set; }

        private async Task DeleteNode()
        {
            if (Node == null || _parentViewModel == null)
            {
                return;
            }

            YesNoDialogViewModel dialog = new($"Soll das Gerät {NodeNumber} wirklich entfernt werden?", _parentViewModel);
            DialogResult result = await dialog.Open();
            if (result == DialogResult.Yes)
            {
                using (BusyState beginBusy = _parentViewModel.BeginBusy("Gerät entfernen..."))
                {
                    DeleteNodeCommand cmd = new();
                    await cmd.ExecuteAsync(_projectManager, Node.Id, beginBusy.GetProgress(), beginBusy.Token);
                }

                await _parentViewModel.Load();
            }
        }

        private async Task OpenNode()
        {
            if (Node == null)
            {
                throw new InvalidOperationException("Please call load first,");
            }

            await _navigationService.NavigateToNodeDetailPage(Node.NodeNumber);
        }

        public async Task Load(BatNode batNode, ProjectPageViewModel parentViewModel)
        {
            Node = batNode;
            _parentViewModel = parentViewModel;

            CallCount = await _projectManager.GetCallCount(Node.Id, CancellationToken.None);
            FileCount = await _projectManager.GetDataFileCount(Node.Id, CancellationToken.None);
            OnPropertyChanged(nameof(NodeNumber));
            OnPropertyChanged(nameof(StartDatum));
            OnPropertyChanged(nameof(CallCount));
            OnPropertyChanged(nameof(FileCount));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}