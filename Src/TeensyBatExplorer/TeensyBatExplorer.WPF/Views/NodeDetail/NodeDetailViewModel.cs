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
        private readonly int _nodeNumber;
        private BatNode _node;
        private PlotModel _batteryPlot;
        private BatCall _selectedCall;
        private List<TemperatureData> _temperatureData;
        private PlotModel _temperaturePlot;

        public NodeDetailViewModel(NavigationArgument<int> navigationArgument, ProjectManager projectManager)
        {
            _projectManager = projectManager;
            _nodeNumber = navigationArgument.Data;

            Title = $"Daten zu Gerät Nr. {_nodeNumber}";
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

        public IEnumerable<BatLog> Logs => Node?.Logs;

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

        public override async Task Load()
        {
            using (BusyState busyState = BeginBusy("Lade Gerätedaten..."))
            {
                await LoadNode(busyState);
            }
        }

        private async Task LoadNode(BusyState busyState)
        {
            await busyState.Update(busyState.Text, 0, 3);

            Node = await _projectManager.GetBatNodeWithLogs(_nodeNumber, busyState.Token);
            OnPropertyChanged(nameof(Logs));

            await busyState.Update(busyState.Text, 1, 3);

            UpdateBatteryPlot(await _projectManager.GetBatteryData(Node.Id, busyState.Token));
            await busyState.Update(busyState.Text, 2, 3);

            UpdateTemperaturePlot(await _projectManager.GetTemperatureData(Node.Id, busyState.Token));
            await busyState.Update(busyState.Text, 3, 3);
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