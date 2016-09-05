using System;
using System.Linq;
using System.Threading.Tasks;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using TeensyBatMap.Common;
using TeensyBatMap.Views.EditLog;

namespace TeensyBatMap.Views.LogDetails
{
    public class ViewInfosPivotModel : PivotModelBase<LogDetailsPageModel>
    {
        private bool _isInitialized;

        public ViewInfosPivotModel(LogDetailsPageModel parentViewModel) : base(parentViewModel)
        {
        }

        public PlotModel Voltage { get; private set; }
        public PlotModel SampleTime { get; private set; }

        public override async Task Initialize()
        {
            if (!_isInitialized)
            {
                Voltage = BuildVoltagePlotModel();
                OnPropertyChanged(nameof(Voltage));

                SampleTime = BuildSampleTimePlotModel();
                OnPropertyChanged(nameof(SampleTime));
                _isInitialized = true;
            }
        }

        private PlotModel BuildVoltagePlotModel()
        {
            PlotModel pm = new PlotModel();
            LineSeries lineSeries = new LineSeries();
            pm.Series.Add(lineSeries);

            pm.Axes.Add(new LinearAxis { Minimum = 0, Maximum = 5, Position = AxisPosition.Left, Title = "Spannung [V]" });
            pm.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Title = "Zeit"});

            lineSeries.Points.AddRange(ParentViewModel.BatInfos.Select((b, i) => new DataPoint(DateTimeAxis.ToDouble(b.Time), b.BatteryVoltage / 1000.0)));

            pm.PlotMargins = new OxyThickness(50, double.NaN, double.NaN, double.NaN);
            return pm.AddStyles();
        }

        private PlotModel BuildSampleTimePlotModel()
        {
            PlotModel pm = new PlotModel();
            LineSeries lineSeries = new LineSeries();
            pm.Series.Add(lineSeries);

            pm.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Dauer [ms]" });
            pm.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Title = "Zeit" });

            lineSeries.Points.AddRange(ParentViewModel.BatInfos.Select((b, i) => new DataPoint(DateTimeAxis.ToDouble(b.Time), b.SampleDuration)));
            pm.PlotMargins = new OxyThickness(50, double.NaN, double.NaN, double.NaN);
            return pm.AddStyles();
        }
    }
}