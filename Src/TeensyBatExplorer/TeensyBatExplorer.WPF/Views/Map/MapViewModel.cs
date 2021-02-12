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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using MapControl;

using MaterialDesignThemes.Wpf;

using Microsoft.Xaml.Behaviors.Core;

using TeensyBatExplorer.Core;
using TeensyBatExplorer.Core.Commands;
using TeensyBatExplorer.Core.Infrastructure;
using TeensyBatExplorer.Core.Models;
using TeensyBatExplorer.Core.Queries;
using TeensyBatExplorer.WPF.Controls;
using TeensyBatExplorer.WPF.Infrastructure;

namespace TeensyBatExplorer.WPF.Views.Map
{
    public class MapViewModel : BaseViewModel
    {
        private readonly ProjectManager _projectManager;
        private readonly UpdateNodeCommand _updateNodeCommand;
        private List<MapNodeViewModel>? _mapNodes;
        private Location? _mapCenter;
        private MapNodeViewModel? _selectedMapNode;
        private List<BarItemModel>? _barItems;
        private DateTime _referenceTime;
        private long _currentPosition;
        private long _detailBarDuration;

        public MapViewModel(ProjectManager projectManager, UpdateNodeCommand updateNodeCommand)
        {
            _projectManager = projectManager;
            _updateNodeCommand = updateNodeCommand;

            SelectNodeCommand = new ActionCommand(i => SelectNode((int)i));

            Title = "Projekt";

            Filter = new CallFilter();
            Filter.PropertyChanged += FilterChanged;

            AddToolbarButton(new ToolBarButton(SaveAll, PackIconKind.ContentSave, "Speichern"));
        }

        public ICommand SelectNodeCommand { get; }

        public bool IsNodeSelected => SelectedMapNode != null;

        public MapNodeViewModel? SelectedMapNode
        {
            get => _selectedMapNode;
            set
            {
                if (!Equals(value, _selectedMapNode))
                {
                    if (_selectedMapNode != null)
                    {
                        _selectedMapNode.IsSelected = false;
                    }

                    _selectedMapNode = value;
                    if (_selectedMapNode != null)
                    {
                        _selectedMapNode.IsSelected = true;
                    }

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsNodeSelected));
                }
            }
        }

        public Location? MapCenter
        {
            get => _mapCenter;
            set
            {
                if (!Equals(value, _mapCenter))
                {
                    _mapCenter = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<MapNodeViewModel>? MapNodes
        {
            get => _mapNodes;
            private set
            {
                if (!Equals(value, _mapNodes))
                {
                    _mapNodes = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<BarItemModel>? BarItems
        {
            get => _barItems;
            set
            {
                if (!Equals(value, _barItems))
                {
                    _barItems = value;
                    OnPropertyChanged();
                }
            }
        }

        public long DetailBarDuration
        {
            get => _detailBarDuration;
            set
            {
                if (value != _detailBarDuration)
                {
                    _detailBarDuration = value;
                    OnPropertyChanged();
                    UpdateMapItems();
                }
            }
        }

        public long CurrentPosition
        {
            get => _currentPosition;
            set
            {
                if (value != _currentPosition)
                {
                    _currentPosition = value;
                    OnPropertyChanged();
                    UpdateMapItems();
                }
            }
        }

        public DateTime ReferenceTime
        {
            get => _referenceTime;
            set
            {
                if (value.Equals(_referenceTime))
                {
                    return;
                }

                _referenceTime = value;
                OnPropertyChanged();
            }
        }

        public CallFilter Filter { get; }

        private void FilterChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (MapNodes == null)
            {
                return;
            }

            DateTime earliestStartTime = MapNodes.Min(n => n.Node.StartTime);

            List<BarItemModel> barItems = new();
            foreach (MapNodeViewModel nodeVm in MapNodes)
            {
                BatNode node = nodeVm.Node;
                double diffMilliseconds = (node.StartTime - earliestStartTime).TotalMilliseconds;
                barItems.AddRange(Filter.Apply(nodeVm.Calls).Select(c => new BarItemModel((int)Math.Round(c.StartTimeMicros / 1000d + diffMilliseconds), 1)));
            }

            BarItems = barItems;
        }

        private async Task SaveAll()
        {
            if (MapNodes == null)
            {
                return;
            }

            foreach (MapNodeViewModel nodeVm in MapNodes)
            {
                await _updateNodeCommand.ExecuteAsync(_projectManager, nodeVm.Node, StackableProgress.NoProgress, CancellationToken.None);
            }
        }

        private void SelectNode(int nodeId)
        {
            if (MapNodes == null)
            {
                return;
            }

            MapNodeViewModel? node = MapNodes.FirstOrDefault(n => n.Node.Id == nodeId);
            if (node != null)
            {
                SelectedMapNode = node;
            }
        }

        private void UpdateMapItems()
        {
            if (MapNodes == null)
            {
                return;
            }

            foreach (MapNodeViewModel mapNode in MapNodes)
            {
                mapNode.Update(_currentPosition, DetailBarDuration);
            }
        }

        public override async Task Load()
        {
            using (BusyState busyState = BeginBusy("Lade Geräte..."))
            {
                StackableProgress progress = busyState.GetProgress();
                List<BatNode> nodes = await _projectManager.GetNodes(busyState.Token);
                DateTime earliestStartTime = nodes.Min(n => n.StartTime);
                progress.Report(10);

                List<BarItemModel> barItems = new();
                List<MapNodeViewModel> mapNodes = new(nodes.Count);

                StackableProgress nodeProgress = progress.Stack(90);

                int i = 0;
                foreach (BatNode node in nodes)
                {
                    double diffMilliseconds = (node.StartTime - earliestStartTime).TotalMilliseconds;
                    List<BatCall> calls = await _projectManager.GetCalls(node.Id, true, progress.Stack(1), busyState.Token);
                    barItems.AddRange(Filter.Apply(calls).Select(c => new BarItemModel((int)Math.Round(c.StartTimeMicros / 1000d + diffMilliseconds), 1)));

                    MapNodeViewModel mapNode = new(node, Filter);
                    mapNode.LoadData(calls, (long)(diffMilliseconds * 1000));
                    mapNodes.Add(mapNode);
                    nodeProgress.Report(i++, nodes.Count);
                }

                BarItems = barItems;
                MapNodes = mapNodes;
                ReferenceTime = earliestStartTime;

                MapNodeViewModel? firstModel = MapNodes.FirstOrDefault(n => n.HasLocation);
                if (firstModel != null)
                {
                    MapCenter = firstModel.Location;
                }
                else
                {
                    MapCenter = new Location(47.3923813, 8.0514308);
                }
            }
        }

        public void UpdateNodeLocation(Location location)
        {
            if (SelectedMapNode != null)
            {
                SelectedMapNode.Location = location;
            }
        }
    }
}