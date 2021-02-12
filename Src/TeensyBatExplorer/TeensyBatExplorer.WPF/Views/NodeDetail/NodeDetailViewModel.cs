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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using MapControl;

using MaterialDesignThemes.Wpf;

using Nito.Mvvm;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using TeensyBatExplorer.Core;
using TeensyBatExplorer.Core.Commands;
using TeensyBatExplorer.Core.Infrastructure;
using TeensyBatExplorer.Core.Models;
using TeensyBatExplorer.Core.Queries;
using TeensyBatExplorer.WPF.Controls;
using TeensyBatExplorer.WPF.Infrastructure;

namespace TeensyBatExplorer.WPF.Views.NodeDetail
{
    public class NodeDetailViewModel : BaseViewModel
    {
        private readonly ProjectManager _projectManager;
        private readonly AnalyzeNodeCommand _analyzeNodeCommand;
        private readonly RemoveLogFileCommand _removeLogFileCommand;

        private readonly Func<UpdateCallCommand> _updateCallCommandFactory;

        private BatNode? _node;
        private PlotModel? _batteryPlot;
        private CallViewModel? _selectedCall;
        private PlotModel? _temperaturePlot;
        private List<CallViewModel>? _calls;

        private Location _currentMapCenter = new(47.3925, 8.0616);
        private int _nodeNumber;
        private NotifyTask<CallViewModel>? _selectedCallLoading;
        private long _currentPosition;

        public NodeDetailViewModel(NavigationArgument<int> navigationArgument, ProjectManager projectManager, AnalyzeNodeCommand analyzeNodeCommand,
            RemoveLogFileCommand removeLogFileCommand, Func<UpdateCallCommand> updateCallCommandFactory)
        {
            _projectManager = projectManager;
            _analyzeNodeCommand = analyzeNodeCommand;
            _removeLogFileCommand = removeLogFileCommand;
            _updateCallCommandFactory = updateCallCommandFactory;
            _nodeNumber = navigationArgument.Data;

            Title = $"Daten zu Gerät Nr. {_nodeNumber}";

            AddToolbarButton(new ToolBarButton(ProcessNode, PackIconKind.Cogs, "Daten analysieren"));
            AddToolbarButton(new ToolBarButton(SaveNode, PackIconKind.ContentSave, "Speichern"));
            RemoveFileCommand = new AsyncCommand(o => RemoveFile((int)o));
        }

        public AsyncCommand RemoveFileCommand { get; set; }

        public long CurrentPosition
        {
            get => _currentPosition;
            set
            {
                if (value != _currentPosition)
                {
                    _currentPosition = value;
                    OnPropertyChanged();
                    PositionChanged(value);
                }
            }
        }

        public CallViewModel? SelectedCall
        {
            get => _selectedCall;
            set
            {
                if (!Equals(value, _selectedCall))
                {
                    _selectedCall = value;
                    OnPropertyChanged();
                    if (_selectedCall != null)
                    {
                        CurrentPosition = (int)Math.Round(_selectedCall.Call.StartTimeMicros / 1000d);
                        SelectedCallLoading = NotifyTask.Create<CallViewModel>(LoadCallData(_selectedCall));
                    }
                    else
                    {
                        CurrentPosition = 0;
                        SelectedCallLoading = null;
                    }
                }
            }
        }

        public NotifyTask<CallViewModel>? SelectedCallLoading
        {
            get => _selectedCallLoading;
            set
            {
                if (Equals(value, _selectedCallLoading))
                {
                    return;
                }

                _selectedCallLoading = value;
                OnPropertyChanged();
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

        public BatNode? Node
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

        public List<DataFileViewModel>? DataFiles { get; set; }

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

        public string MapPushpinText => $"Gerät {NodeNumber}";

        public Location? NodeLocation
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
                if (Node == null)
                {
                    return;
                }

                Node.Longitude = value?.Longitude;
                Node.Latitude = value?.Latitude;
                OnPropertyChanged();
            }
        }

        public PlotModel? BatteryPlot
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

        public PlotModel? TemperaturePlot
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

        public List<CallViewModel>? Calls
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

        public string? Time
        {
            get => Node?.StartTime.ToString("HH:mm:ss");
            set
            {
                TryParseTime(value);
                OnPropertyChanged();
            }
        }

        public DateTime? Date
        {
            get => Node?.StartTime.Date;
            set
            {
                if (Node != null && value.HasValue && value != Node.StartTime.Date)
                {
                    Node.StartTime = new DateTime(value.Value.Year, value.Value.Month, value.Value.Day, Node.StartTime.Hour, Node.StartTime.Minute, Node.StartTime.Second, Node.StartTime.Millisecond);
                    OnPropertyChanged();
                }
            }
        }

        public List<BarItemModel> BarItems { get; private set; } = new();


        private async Task RemoveFile(int dataFileId)
        {
            using (BusyState busyState = BeginBusy("Entferne Log Datei..."))
            {
                StackableProgress progress = busyState.GetProgress();
                await _removeLogFileCommand.Execute(_projectManager, dataFileId, progress, busyState.Token);
                await LoadNode(progress.Stack(50), busyState.Token);
            }
        }

        private async Task<CallViewModel> LoadCallData(CallViewModel callViewModel)
        {
            if (!callViewModel.Call.Entries.Any())
            {
                List<BatDataFileEntry> entries = await _projectManager.GetCall(callViewModel.Call.Id, StackableProgress.NoProgress, CancellationToken.None);
                callViewModel.SetEntries(entries);
            }

            return callViewModel;
        }


        private void TryParseTime(string? timeString)
        {
            if (string.IsNullOrWhiteSpace(timeString) || Node == null)
            {
                return;
            }

            Match match = TimeValidationRule.TimePattern.Match(timeString.Trim());
            if (match.Success)
            {
                int h = int.Parse(match.Groups["h"].Value);
                int m = int.Parse(match.Groups["m"].Value);
                int s = int.Parse(match.Groups["s"].Value);

                Node.StartTime = new DateTime(Node.StartTime.Year, Node.StartTime.Month, Node.StartTime.Day, h, m, s);
                OnPropertyChanged(nameof(Node));
            }
        }

        private async Task SaveNode()
        {
            if (Node == null)
            {
                return;
            }

            using (BusyState busyState = BeginBusy("Speichere Gerät..."))
            {
                UpdateNodeCommand cmd = new();
                await cmd.ExecuteAsync(_projectManager, Node, busyState.GetProgress(), busyState.Token);
                _nodeNumber = Node.NodeNumber;
            }
        }

        private async Task ProcessNode()
        {
            if (_node == null)
            {
                return;
            }

            using (BusyState busyState = BeginBusy("Analysiere Gerätedaten..."))
            {
                StackableProgress progress = busyState.GetProgress();
                await _analyzeNodeCommand.Process(_node.Id, progress.Stack(70), busyState.Token);
                await LoadNode(progress.Stack(30), busyState.Token);
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
            OnPropertyChanged(nameof(Time));
            OnPropertyChanged(nameof(Date));
            progress.Report(10);

            if (Node.Longitude.HasValue && Node.Latitude.HasValue)
            {
                NodeLocation = new Location(Node.Latitude.Value, Node.Longitude.Value);
                CurrentMapCenter = NodeLocation;
            }

            CallFilter filter = new();
            List<BatCall> calls = filter.Apply(await _projectManager.GetCalls(Node.Id, false, progress.Stack(80), cancellationToken)).ToList();
            Calls = calls.Select(c => new CallViewModel(c, _updateCallCommandFactory, filter)).ToList();
            BarItems = calls.Select(c => new BarItemModel((int)Math.Round(c.StartTimeMicros / 1000d), 1)).ToList();
            SelectedCall = Calls.FirstOrDefault();
            OnPropertyChanged(nameof(BarItems));
            progress.Report(90);

            List<DataFileViewModel> files = new(Node.DataFiles.Count);
            foreach (BatDataFile file in Node.DataFiles.OrderBy(d => d.FileCreateTime))
            {
                DataFileViewModel vm = new();
                files.Add(vm);
                int count = await _projectManager.CountDataFileEntries(file.Id, cancellationToken);
                vm.Load(file, count);
            }

            DataFiles = files;
            OnPropertyChanged(nameof(DataFiles));

            UpdateBatteryPlot(await _projectManager.GetBatteryData(Node.Id, cancellationToken));
            UpdateTemperaturePlot(await _projectManager.GetTemperatureData(Node.Id, cancellationToken));
            progress.Report(100);
        }

        private void UpdateBatteryPlot(List<BatteryData> batteryData)
        {
            if (!batteryData.Any() || Node == null)
            {
                return;
            }

            PlotModel pm = new();
            LineSeries lineSeries = new();
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
            if (!temperatureData.Any() || Node == null)
            {
                return;
            }

            PlotModel pm = new();
            LineSeries lineSeries = new();
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
            if (Calls == null)
            {
                return;
            }

            foreach (CallViewModel call in Calls)
            {
                if (call.Call.StartTimeMicros >= newPosition * 1000)
                {
                    SelectedCall = call;
                    break;
                }
            }
        }
    }
}