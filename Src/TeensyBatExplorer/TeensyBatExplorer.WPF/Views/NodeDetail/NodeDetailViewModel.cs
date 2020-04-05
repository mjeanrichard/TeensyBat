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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using MaterialDesignThemes.Wpf;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using TeensyBatExplorer.Core;
using TeensyBatExplorer.Core.Models;
using TeensyBatExplorer.Core.Queries;
using TeensyBatExplorer.WPF.Infrastructure;

namespace TeensyBatExplorer.WPF.Views.NodeDetail
{
    public class NodeDetailViewModel : BaseViewModel
    {
        private readonly ProjectManager _projectManager;
        private readonly NodeProcessor _nodeProcessor;
        private readonly int _nodeNumber;
        private BatNode _node;
        private PlotModel _batteryPlot;
        private BatCall _selectedCall;
        private PlotModel _temperaturePlot;
        private List<BatCall> _calls;

        public NodeDetailViewModel(NavigationArgument<int> navigationArgument, ProjectManager projectManager, NodeProcessor nodeProcessor)
        {
            _projectManager = projectManager;
            _nodeProcessor = nodeProcessor;
            _nodeNumber = navigationArgument.Data;

            Title = $"Daten zu Gerät Nr. {_nodeNumber}";

            AddToolbarButton(new ToolBarButton(ProcessNode, PackIconKind.MessageProcessing, "Daten analysieren"));
        }

        public BatCall SelectedCall
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

        public BatNode Node
        {
            get => _node;
            set
            {
                if (!Equals(value, _node))
                {
                    _node = value;
                    OnPropertyChanged();
                }
            }
        }

        public IEnumerable<BatDataFile> DataFiles => Node?.DataFiles;

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

        public List<BatCall> Calls
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

        private async Task ProcessNode()
        {
            using (BusyState busyState = BeginBusy("Analysiere Gerätedaten..."))
            {
                await _nodeProcessor.Process(_node.Id, busyState.GetProgress(), busyState.Token);
                await busyState.Update("Daten neu laden...", 0, 10);
                await LoadNode(busyState);
            }
        }

        public override async Task Load()
        {
            using (BusyState busyState = BeginBusy("Lade Gerätedaten..."))
            {
                await LoadNode(busyState);
            }
        }

        private async Task LoadNode(BusyState busyState)
        {
            await busyState.Update(busyState.Text, 1, 10);

            Node = await _projectManager.GetBatNodeWithFiles(_nodeNumber, busyState.Token);
            OnPropertyChanged(nameof(DataFiles));
            await busyState.Update(busyState.Text, 1, 10);

            Calls = await _projectManager.GetCalls(Node.Id, busyState.Token);
            await busyState.Update(busyState.Text, 8, 10);

            UpdateBatteryPlot(await _projectManager.GetBatteryData(Node.Id, busyState.Token));
            await busyState.Update(busyState.Text, 9, 10);

            UpdateTemperaturePlot(await _projectManager.GetTemperatureData(Node.Id, busyState.Token));
            await busyState.Update(busyState.Text, 10, 10);
        }

        private void UpdateBatteryPlot(List<BatteryData> batteryData)
        {
            PlotModel pm = new PlotModel();
            LineSeries lineSeries = new LineSeries();
            pm.Series.Add(lineSeries);

            pm.Axes.Add(new LinearAxis { Minimum = 0, Maximum = 6, Position = AxisPosition.Left, Title = "Spannung [V]" });
            pm.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Title = "Zeit" });

            lineSeries.Points.AddRange(batteryData.Select((b, i) => DateTimeAxis.CreateDataPoint(b.DateTime, b.Voltage / 1000d)));

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

            lineSeries.Points.AddRange(temperatureData.Select((b, i) => DateTimeAxis.CreateDataPoint(b.DateTime, b.Temperature / 10d)));

            pm.PlotMargins = new OxyThickness(50, double.NaN, double.NaN, double.NaN);

            TemperaturePlot = pm;
        }
    }
}