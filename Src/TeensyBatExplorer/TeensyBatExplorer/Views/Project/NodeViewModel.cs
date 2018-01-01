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

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using TeensyBatExplorer.Common;
using TeensyBatExplorer.Core.BatLog;

using UniversalMapControl.Interfaces;

namespace TeensyBatExplorer.Views.Project
{
    public class NodeViewModel : INotifyPropertyChanged, IHasLocation
    {
        private readonly BatProject _project;
        private readonly BatNode _batNode;
        private readonly NavigationService _navigationService;
        private bool _isHighlighted;

        public NodeViewModel(BatProject project, BatNode batNode, NavigationService navigationService, ProjectViewModel parentViewModel)
        {
            _project = project;
            _batNode = batNode;
            _navigationService = navigationService;
            EditNodeCommand = new AsyncCommand(EditNode, parentViewModel);
        }

        public RelayCommand EditNodeCommand { get; private set; }


        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_batNode.Name))
                {
                    return $"Node {_batNode.NodeId}";
                }
                return $"Node {_batNode.NodeId} - {_batNode.Name}";
            }
        }

        public DateTime LogStart
        {
            get { return _batNode.LogStart; }
        }

        public bool IsHighlighted
        {
            get { return _isHighlighted; }
            set
            {
                _isHighlighted = value;
                OnPropertyChanged();
            }
        }

        public ILocation Location
        {
            get { return _batNode.Location; }
        }

        private async Task EditNode()
        {
            await _navigationService.ShowNodeDetails(_batNode, _project);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}