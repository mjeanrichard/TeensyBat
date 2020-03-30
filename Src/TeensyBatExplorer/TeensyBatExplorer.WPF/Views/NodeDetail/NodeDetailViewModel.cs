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

using TeensyBatExplorer.Core;
using TeensyBatExplorer.Core.Models;
using TeensyBatExplorer.Core.Queries;
using TeensyBatExplorer.WPF.Infrastructure;

namespace TeensyBatExplorer.WPF.Views.NodeDetail
{
    public class NodeDetailViewModel : BaseViewModel
    {
        private readonly ProjectManager _projectManager;
        private int _nodeNumber;
        private BatNode _node;

        public NodeDetailViewModel(NavigationArgument<int> navigationArgument, ProjectManager projectManager)
        {
            _projectManager = projectManager;
            _nodeNumber = navigationArgument.Data;
        }

        public BatNode Node
        {
            get => _node;
            set
            {
                if (Equals(value, _node))
                {
                    return;
                }

                _node = value;
                OnPropertyChanged();
            }
        }

        public override async Task Load()
        {
            using (BusyState busyState = BeginBusy("Lade Gerätedaten..."))
            {
                BatNode batNode = await Task.Run(async () => await LoadNode(busyState));
                Node = batNode;
            }
        }

        private async Task<BatNode> LoadNode(BusyState busyState)
        {
            return await _projectManager.GetBatNodeWithLogs(_nodeNumber, busyState.Token);
        }
    }
}