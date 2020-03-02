// 
// Teensy Bat Explorer - Copyright(C) 2018 Meinrad Jean-Richard
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
using System.Threading.Tasks;

using Windows.Media.Audio;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;

using Microsoft.Toolkit.Uwp.Helpers;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using TeensyBatExplorer.Helpers.ViewModels;
using TeensyBatExplorer.Services;
using TeensyBatExplorer.Views.Project;

namespace TeensyBatExplorer.Views.Log
{
    public class LogViewModel : BaseViewModel
    {
        private readonly LogReader _logReader;
        private BatLog _log;
        private CallModel _selectedCall;
        private List<CallModel> _calls;

        public LogViewModel(LogReader logReader)
        {
            _logReader = logReader;
            OpenFileCommand = new AsyncCommand(OpenFile, this);
            BatteryData = new BatteryViewModel();
            TemperatureData = new TemperatureViewModel();
        }

        private async Task OpenFile()
        {
            using (MarkBusy())
            {
                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.ViewMode = PickerViewMode.List;
                openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
                openPicker.FileTypeFilter.Add(".dat");
                IReadOnlyList<StorageFile> files = await DispatcherHelper.ExecuteOnUIThreadAsync(async () => await openPicker.PickMultipleFilesAsync());
                if (files.Count > 0)
                {
                    _log = new BatLog();
                    foreach (StorageFile file in files)
                    {
                        await _logReader.Load(file, _log);
                    }
                    _calls = _log.Calls.OrderBy(l => l.StartTimeMS).Where(c => c.IsBat).Select(l => new CallModel(l)).ToList();

                    await BatteryData.Update(_log);
                    await TemperatureData.Update(_log);

                    OnPropertyChanged(nameof(Calls));
                }
            }
        }

        public IList<CallModel> Calls
        {
            get { return _calls; }
        }
        public CallModel SelectedCall { get{return _selectedCall;} set{Set(ref _selectedCall, value);} }

        public AsyncCommand OpenFileCommand { get; set; }

        public BatteryViewModel BatteryData { get; set; }
        public TemperatureViewModel TemperatureData { get; set; }
    }

    public class BatteryViewModel : Observable
    {
        private BatLog _log;
        private PlotModel _voltage;

        public PlotModel Voltage
        {
            get => _voltage;
            set => Set(ref _voltage, value);
        }

        public async Task Update(BatLog log)
        {
            _log = log;

            PlotModel pm = new PlotModel();
            LineSeries lineSeries = new LineSeries();
            pm.Series.Add(lineSeries);

            pm.Axes.Add(new LinearAxis { Minimum = 0, Maximum = 6, Position = AxisPosition.Left, Title = "Spannung [V]" });
            //pm.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Title = "Zeit" });
            pm.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Zeit" });

            lineSeries.Points.AddRange(_log.BatteryData.Select((b, i) => new DataPoint(DateTimeAxis.ToDouble(b.DateTime), b.Voltage / 100.0)));

            pm.PlotMargins = new OxyThickness(50, double.NaN, double.NaN, double.NaN);
            ApplicationTheme applicationTheme = await DispatcherHelper.ExecuteOnUIThreadAsync(() => Application.Current.RequestedTheme);
            Voltage = pm.AddStyles(applicationTheme);
        }

    }

    public class TemperatureViewModel : Observable
    {
        private BatLog _log;
        private PlotModel _temp;

        public PlotModel Temp
        {
            get => _temp;
            set => Set(ref _temp, value);
        }

        public async Task Update(BatLog log)
        {
            _log = log;
            PlotModel pm = new PlotModel();
            LineSeries lineSeries = new LineSeries();
            pm.Series.Add(lineSeries);

            pm.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Temperatur [°C]" });
            pm.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Title = "Zeit" });

            lineSeries.Points.AddRange(_log.TemperatureData.Select((b, i) => new DataPoint(DateTimeAxis.ToDouble(b.DateTime), b.Temperature)));

            pm.PlotMargins = new OxyThickness(50, double.NaN, double.NaN, double.NaN);

            ApplicationTheme applicationTheme = await DispatcherHelper.ExecuteOnUIThreadAsync(() => Application.Current.RequestedTheme);
            Temp = pm.AddStyles(applicationTheme);
        }

    }

    public static class OxyHelper
    {
        public static PlotModel AddStyles(this PlotModel plotModel, ApplicationTheme applicationTheme)
        {
            OxyColor background;
            OxyColor foreground;
            OxyColor softForeground;
            if (applicationTheme == ApplicationTheme.Dark)
            {
                background = OxyColors.Black;
                foreground = OxyColors.LightGray;
                softForeground = OxyColors.Gray;
            }
            else
            {
                background = OxyColors.White;
                foreground = OxyColors.Black;
                softForeground = OxyColors.Gray;
            }
            plotModel.Background = background;
            plotModel.LegendBackground = background;
            plotModel.PlotAreaBackground = background;

            plotModel.PlotAreaBorderThickness = new OxyThickness(1);
            plotModel.PlotAreaBorderColor = foreground;

            plotModel.TextColor = foreground;
            plotModel.LegendTextColor = foreground;
            plotModel.SubtitleColor = foreground;
            plotModel.TitleColor = foreground;

            foreach (Axis axis in plotModel.Axes)
            {
                axis.AxislineColor = foreground;
                axis.ExtraGridlineColor = softForeground;
                axis.MajorGridlineColor = softForeground;
                axis.MinorGridlineColor = softForeground;
                axis.TicklineColor = foreground;
                axis.TitleColor = foreground;
                axis.TextColor = foreground;

                axis.AxisTitleDistance = 10;
                axis.MajorGridlineStyle = LineStyle.Solid;
            }
            return plotModel;
        }
    }

}