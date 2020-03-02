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

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using TeensyBatExplorer.Business.Models;
using TeensyBatExplorer.Services;

namespace TeensyBatExplorer.Views.Project
{
    public sealed partial class CallDetailControl : UserControl
    {
        public static readonly DependencyProperty CallModelProperty = DependencyProperty.Register("CallModel", typeof(CallModel), typeof(CallDetailControl), new PropertyMetadata(null, OnCallModelPropertyChanged));

        private static void OnCallModelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as CallDetailControl;
            control.ForegroundElement.ChangeView(0, 0, 1);
        }

        public CallModel CallModel
        {
            get { return GetValue(CallModelProperty) as CallModel; }
            set { SetValue(CallModelProperty, value); }
        }

        public CallDetailControl()
        {
            InitializeComponent();
        }
    }

    public class CallModel
    {
        private HeatMapSeries _heatMapSeries;
        private PlotModel _plotModel;

        public CallModel(BatCall batLog)
        {
            Count = batLog.FftCount;
            Data = batLog;

            _heatMapSeries = new HeatMapSeries
            {
                X0 = 0,
                X1 = 470,
                Y0 = 0,
                Y1 = 70,
                Data = new double[1,1],
                //RenderMethod = HeatMapRenderMethod.Bitmap,
                Interpolate = false,
            };

            PlotModel = new PlotModel();
            PlotModel.Axes.Add(new LinearColorAxis
            {
                Palette = OxyPalettes.Rainbow(100)
            });
            PlotModel.Series.Add(_heatMapSeries);

            double[,] data = new double[batLog.FftCount, 128];
            int i = 0;
            foreach (FftBlock fftBlock in batLog.FftData)
            {
                for (int j = 0; j < fftBlock.Data.Length; j++)
                {
                    data[i, j] = fftBlock.Data[j];
                }
                i++;
            }

            _heatMapSeries.Data = data;
            _heatMapSeries.X1 = batLog.FftCount;
        }

        public int Count { get; set; }

        public BatCall Data { get; private set; }

        public PlotModel PlotModel
        {
            get => _plotModel;
            set => _plotModel = value;
        }
    }
}