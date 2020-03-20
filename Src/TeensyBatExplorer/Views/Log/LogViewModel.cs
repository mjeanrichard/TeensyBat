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
using Windows.UI.Xaml.Navigation;

using Microsoft.Toolkit.Uwp.Helpers;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using TeensyBatExplorer.Business;
using TeensyBatExplorer.Business.Models;
using TeensyBatExplorer.Business.Queries;
using TeensyBatExplorer.Helpers.ViewModels;
using TeensyBatExplorer.Services;
using TeensyBatExplorer.Views.Project;

namespace TeensyBatExplorer.Views.Log
{
    public class LogViewModel : BaseViewModel
    {
        private readonly LogReader _logReader;
        private readonly ProjectManager _projectManager;
        private BatLog _log;
        private CallModel _selectedCall;
        private List<CallModel> _calls;
        private int _nodeNumber;

        public AsyncCommand AnalyzeCallsCommand { get; }

        public LogViewModel(NavigationEventArgs navigationEventArgs, LogReader logReader, ProjectManager projectManager)
        {
            _logReader = logReader;
            _projectManager = projectManager;

            _nodeNumber = (int)navigationEventArgs.Parameter;

            AnalyzeCallsCommand = new AsyncCommand(AnalyzeCalls, this);
            BatteryData = new BatteryViewModel();
            TemperatureData = new TemperatureViewModel();
        }

        private async Task AnalyzeCalls()
        {
            //using (BusyState busyState = await MarkBusy("SDF"))
            //{
            //    GetLogDetailsQuery query = new GetLogDetailsQuery();
            //    IEnumerable<BatCall> calls = await query.Execute(_projectFile);
            //    //foreach (BatCall call in calls)
            //    //{
            //    //    int maxLen = 0;
            //    //    int count = 0;
            //    //    foreach (FftBlock fftBlock in call.FftData)
            //    //    {
            //    //        if (fftBlock.Loudness > 2400)
            //    //        {
            //    //            count++;
            //    //        }
            //    //        else
            //    //        {
            //    //            if (count > 0)
            //    //            {
            //    //                if (maxLen < count)
            //    //                {
            //    //                    maxLen = count;
            //    //                }

            //    //                count = 0;
            //    //            }
            //    //        }
            //    //    }
            //    //    //call.HighPowerSampleCount = call.FftData.Count(f => f.Loudness > 2400);
            //    //    call.HighPowerSampleCount = maxLen;
            //    //    call.IsBat = (call.HighPowerSampleCount >= 2 && call.HighPowerSampleCount < 5) && call.HighFreqSampleCount < 1;
            //    //}

            //    foreach (BatCall call in calls)
            //    {
            //        int noisiness = 0;
            //        bool[] check = new bool[40];
            //        int[] values = new int[40];
            //        int lastVal = 0;
            //        bool lastDeltaPositive = true;
            //        for (int i = 0; i < call.FftData.Count; i++)
            //        {
            //            int value = call.FftData[i].Loudness;
            //            int delta = value - lastVal;
            //            if (delta > 0 && !lastDeltaPositive)
            //            {
            //                noisiness++;
            //            }
            //            else if (delta < 0 && lastDeltaPositive)
            //            {
            //                noisiness++;
            //            }

            //            lastVal = value;
            //            lastDeltaPositive = delta > 0;
            //        }
            //        //for (int i = 0; i < call.FftData.Count; i++)
            //        //{
            //        //    FftBlock fftBlock = call.FftData[i];
            //        //    for (int j = 10; j < 25; j++)
            //        //    {
            //        //        byte value = fftBlock.Data[j];
            //        //        if (i == 0)
            //        //        {
            //        //            values[j] = value;
            //        //        }
            //        //        else
            //        //        {
            //        //            int delta = values[j] - value;
            //        //            if (delta > 10)
            //        //            {
            //        //                noisiness++;
            //        //            }
            //        //            else if (delta < -10)
            //        //            {
            //        //                noisiness++;
            //        //            }

            //        //            values[j] = value;
            //        //            check[j] = delta < 0;
            //        //        }
            //        //    }
            //        //}

            //        int maxLen = 0;
            //        int count = 0;
            //        foreach (FftBlock fftBlock in call.FftData)
            //        {
            //            if (fftBlock.Loudness > 2400)
            //            {
            //                count++;
            //            }
            //            else
            //            {
            //                if (count > 0)
            //                {
            //                    if (maxLen < count)
            //                    {
            //                        maxLen = count;
            //                    }

            //                    count = 0;
            //                }
            //            }
            //        }


            //        call.AvgPeakFrequency = noisiness;
            //        call.HighPowerSampleCount = maxLen;
            //        call.IsBat = (call.HighPowerSampleCount >= 2 && call.HighPowerSampleCount < 5) && call.HighFreqSampleCount < 1;

            //    }

            //    _calls = calls.OrderBy(l => l.StartTimeMS).Where(c => c.IsBat).Select(l => new CallModel(l)).ToList();
            //    OnPropertyChanged(nameof(Calls));
            //}
        }

        public IList<CallModel> Calls
        {
            get { return _calls; }
        }

        public CallModel SelectedCall { get{return _selectedCall;} set{Set(ref _selectedCall, value);} }

        public BatteryViewModel BatteryData { get; set; }
        public TemperatureViewModel TemperatureData { get; set; }

        protected override async Task LoadData()
        {
            IEnumerable<BatCall> calls = _projectManager.GetCallsForNode(_nodeNumber);
            _calls = calls.OrderBy(l => l.StartTimeMS).Where(c => c.IsBat).Select(l => new CallModel(l)).ToList();
            OnPropertyChanged(nameof(Calls));
            //await BatteryData.Update(_log);
            //await TemperatureData.Update(_log);
        }
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