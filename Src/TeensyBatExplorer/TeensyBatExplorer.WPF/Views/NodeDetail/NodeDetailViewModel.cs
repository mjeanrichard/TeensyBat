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
using System.Threading;
using System.Threading.Tasks;

using MapControl;

using MaterialDesignThemes.Wpf;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using TeensyBatExplorer.Core;
using TeensyBatExplorer.Core.Commands;
using TeensyBatExplorer.Core.Infrastructure;
using TeensyBatExplorer.Core.Models;
using TeensyBatExplorer.Core.Queries;
using TeensyBatExplorer.WPF.Annotations;
using TeensyBatExplorer.WPF.Controls;
using TeensyBatExplorer.WPF.Infrastructure;

namespace TeensyBatExplorer.WPF.Views.NodeDetail
{
    public class NodeDetailViewModel : BaseViewModel
    {
        private readonly ProjectManager _projectManager;
        private readonly AnalyzeNodeCommand _analyzeNodeCommand;
        private BatNode _node;
        private PlotModel _batteryPlot;
        private CallViewModel _selectedCall;
        private PlotModel _temperaturePlot;
        private List<CallViewModel> _calls;

        private Location _currentMapCenter = new Location(47.3925, 8.0616);
        private int _nodeNumber;

        public NodeDetailViewModel(NavigationArgument<int> navigationArgument, ProjectManager projectManager, AnalyzeNodeCommand analyzeNodeCommand)
        {
            _projectManager = projectManager;
            _analyzeNodeCommand = analyzeNodeCommand;
            _nodeNumber = navigationArgument.Data;

            Title = $"Daten zu Gerät Nr. {_nodeNumber}";

            AddToolbarButton(new ToolBarButton(ProcessNode, PackIconKind.Cogs, "Daten analysieren"));
            AddToolbarButton(new ToolBarButton(SaveNode, PackIconKind.ContentSave, "Speichern"));
        }

        public CallViewModel SelectedCall
        {
            get => _selectedCall;
            set
            {
                if (!Equals(value, _selectedCall))
                {
                    _selectedCall = value;
                    OnPropertyChanged();
                }
            }
        }

        public string NodeNumber
        {
            get => Node?.NodeNumber.ToString() ?? _nodeNumber.ToString();
            set
            {
                if (Node != null && int.TryParse(value, out int intValue))
                {
                    if (Node.NodeNumber != intValue)
                    {
                        Node.NodeNumber = intValue;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(MapPushpinText));
                    }
                }
            }
        }

        public BatNode Node
        {
            get => _node;
            set
            {
                if (!Equals(value, _node))
                {
                    _node = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(NodeLocation));
                    OnPropertyChanged(nameof(MapPushpinText));
                    OnPropertyChanged(nameof(NodeNumber));
                }
            }
        }

        public List<DataFileViewModel> DataFiles { get; set; }


        public Location CurrentMapCenter
        {
            get => _currentMapCenter;
            private set
            {
                if (Equals(value, _currentMapCenter))
                {
                    return;
                }

                _currentMapCenter = value;
                OnPropertyChanged();
            }
        }

        public string MapPushpinText
        {
            get => $"Gerät {NodeNumber}";
        }

        public Location NodeLocation
        {
            get
            {
                if (Node?.Longitude != null && Node.Latitude.HasValue)
                {
                    return new Location(Node.Latitude.Value, Node.Longitude.Value);
                }

                return null;
            }
            set
            {
                Node.Longitude = value.Longitude;
                Node.Latitude = value.Latitude;
                OnPropertyChanged();
            }
        }

        public PlotModel BatteryPlot
        {
            get => _batteryPlot;
            set
            {
                if (!Equals(value, _batteryPlot))
                {
                    _batteryPlot = value;
                    OnPropertyChanged();
                }
            }
        }

        public PlotModel TemperaturePlot
        {
            get => _temperaturePlot;
            set
            {
                if (!Equals(value, _temperaturePlot))
                {
                    _temperaturePlot = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<CallViewModel> Calls
        {
            get => _calls;
            set
            {
                if (Equals(value, _calls))
                {
                    return;
                }

                _calls = value;
                OnPropertyChanged();
            }
        }

        public List<BarItemModel> BarItems { get; private set; } = new List<BarItemModel>();

        private async Task SaveNode()
        {
            using (BusyState busyState = BeginBusy("Speichere Gerät..."))
            {
                UpdateNodeCommand cmd = new UpdateNodeCommand();
                await cmd.ExecuteAsync(_projectManager, Node, busyState.GetProgress(), busyState.Token);
                _nodeNumber = Node.NodeNumber;
            }
        }

        private async Task ProcessNode()
        {
            using (BusyState busyState = BeginBusy("Analysiere Gerätedaten..."))
            {
                StackableProgress progress = busyState.GetProgress();
                await _analyzeNodeCommand.Process(_node.Id, progress.Stack(50), busyState.Token);
                progress.Report(50);
                await LoadNode(progress.Stack(50), busyState.Token);
            }
        }

        public override async Task Load()
        {
            using (BusyState busyState = BeginBusy("Lade Gerätedaten..."))
            {
                await LoadNode(busyState.GetProgress(), busyState.Token);
            }
        }

        private async Task LoadNode(StackableProgress progress, CancellationToken cancellationToken)
        {
            Node = await _projectManager.GetBatNodeWithFiles(_nodeNumber, cancellationToken);
            progress.Report(10);

            if (Node.Longitude.HasValue && Node.Latitude.HasValue)
            {
                NodeLocation = new Location(Node.Latitude.Value, Node.Longitude.Value);
                CurrentMapCenter = NodeLocation;
            }

            List<BatCall> calls = await _projectManager.GetCalls(Node.Id, progress.Stack(70), cancellationToken);
            Calls = calls.Select(c => new CallViewModel(c)).ToList();
            BarItems = calls.Select(c => new BarItemModel(c.StartTimeMicros, 1)).ToList();
            SelectedCall = Calls.FirstOrDefault();
            OnPropertyChanged(nameof(BarItems));
            progress.Report(80);

            List<DataFileViewModel> files = new List<DataFileViewModel>(Node.DataFiles.Count);
            foreach (BatDataFile file in Node.DataFiles)
            {
                DataFileViewModel vm = new DataFileViewModel();
                files.Add(vm);
                vm.Load(file, calls.Sum(c => c.Entries.Count(e => e.DataFileId == file.Id)));
            }

            DataFiles = files;
            OnPropertyChanged(nameof(DataFiles));

            UpdateBatteryPlot(await _projectManager.GetBatteryData(Node.Id, cancellationToken));
            progress.Report(90);

            UpdateTemperaturePlot(await _projectManager.GetTemperatureData(Node.Id, cancellationToken));
            progress.Report(100);
        }

        private void UpdateBatteryPlot(List<BatteryData> batteryData)
        {
            PlotModel pm = new PlotModel();
            LineSeries lineSeries = new LineSeries();
            pm.Series.Add(lineSeries);

            pm.Axes.Add(new LinearAxis { Minimum = 0, Maximum = 6, Position = AxisPosition.Left, Title = "Spannung [V]" });
            pm.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Title = "Zeit" });

            DateTime refTime = Node.StartTime;
            lineSeries.Points.AddRange(batteryData.Select((b, i) => DateTimeAxis.CreateDataPoint(refTime.AddMilliseconds(b.Timestamp), b.Voltage / 1000d)));
            lineSeries.CanTrackerInterpolatePoints = false;

            pm.PlotMargins = new OxyThickness(50, double.NaN, double.NaN, double.NaN);

            BatteryPlot = pm;
        }

        private void UpdateTemperaturePlot(List<TemperatureData> temperatureData)
        {
            PlotModel pm = new PlotModel();
            LineSeries lineSeries = new LineSeries();
            pm.Series.Add(lineSeries);

            double minTemp = temperatureData.Min(t => t.Temperature / 10d);
            if (minTemp > 0)
            {
                minTemp = 0;
            }

            pm.Axes.Add(new LinearAxis { Minimum = minTemp, Position = AxisPosition.Left, Title = "CPU Temperatur [°C]" });
            pm.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Title = "Zeit" });

            DateTime refTime = Node.StartTime;
            lineSeries.Points.AddRange(temperatureData.Select((b, i) => DateTimeAxis.CreateDataPoint(refTime.AddMilliseconds(b.Timestamp), b.Temperature / 10d)));
            lineSeries.CanTrackerInterpolatePoints = false;

            pm.PlotMargins = new OxyThickness(50, double.NaN, double.NaN, double.NaN);

            TemperaturePlot = pm;
        }

        public void PositionChanged(in long newPosition)
        {
            foreach (CallViewModel call in Calls)
            {
                if (call.Call.StartTimeMicros >= newPosition)
                {
                    SelectedCall = call;
                    break;
                }
            }
        }
    }

    public class CallViewModel : INotifyPropertyChanged
    {
        public CallViewModel(BatCall call)
        {
            Call = call;
        }

        public BatCall Call { get; private set; }

        public int Duration => (int)(Call.DurationMicros / 1000);
        public string Time => Call.StartTime.ToString("HH:mm:ss");

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}