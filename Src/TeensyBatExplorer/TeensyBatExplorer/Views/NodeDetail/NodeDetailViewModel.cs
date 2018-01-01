using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Windows.UI.Xaml.Navigation;

using MathNet.Filtering;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

using TeensyBatExplorer.Common;
using TeensyBatExplorer.Core;
using TeensyBatExplorer.Core.BatLog;
using TeensyBatExplorer.Views.Main;

using UniversalMapControl.Interfaces;
using UniversalMapControl.Projections;

namespace TeensyBatExplorer.Views.NodeDetail
{
    public class NodeDetailViewModel : BaseViewModel
    {
        private readonly BatProject _project;
        private readonly BatNodeLogReader _batNodeLogReader;
        private readonly LogAnalyzer _logAnalyzer;
        private readonly BatProjectManager _batProjectManager;
        private List<CallModel> _calls;
        private BatNode _node;
        private CallModel _selectedCall;
        private PlotModel _power;
        private PlotModel _frequency;

        public NodeDetailViewModel(NavigationEventArgs navigation, BatNodeLogReader batNodeLogReader, LogAnalyzer logAnalyzer, BatProjectManager batProjectManager) : this((Tuple<BatProject,BatNode>)navigation.Parameter)
        {
            _batNodeLogReader = batNodeLogReader;
            _logAnalyzer = logAnalyzer;
            _batProjectManager = batProjectManager;
        }

        public NodeDetailViewModel() : this(new BatProject(), new BatNode())
        {
        }

        public NodeDetailViewModel(Tuple<BatProject,BatNode> tuple) : this(tuple.Item1, tuple.Item2)
        {
        }

        private NodeDetailViewModel(BatProject project, BatNode node)
        {
            _project = project;
            Node = node;
        }

        public RelayCommand ImportCommand { get; private set; }

        public List<CallModel> Calls
        {
            get { return _calls; }
            set
            {
                _calls = value;
                OnPropertyChanged();
            }
        }

        public BatNode Node
        {
            get { return _node; }
            set
            {
                _node = value;
                OnPropertyChanged();
            }
        }

        public ILocation Location
        {
            get { return Node.Location; }
            set
            {
                Node.Location = value;
                OnPropertyChanged();
            }
        }

        public CallModel SelectedCall
        {
            get { return _selectedCall; }
            set
            {
                _selectedCall = value;
                OnPropertyChanged();
                if (value != null)
                {
                    InitPlots(value);
                }
            }
        }

        public PlotModel Power
        {
            get { return _power; }
            set
            {
                _power = value;
                OnPropertyChanged();
            }
        }

        public PlotModel Frequency
        {
            get { return _frequency; }
            set
            {
                _frequency = value;
                OnPropertyChanged();
            }
        }

        protected override async Task InitializeInternalAsync()
        {
            await _batProjectManager.LoadNodeData(_project, _node);
            Calls = _node.NodeData.Calls.Select(c => new CallModel(c, _node.NodeData)).ToList();
        }

        private void InitPlots(CallModel callModel)
        {
            BatCall call = callModel.Call;

            PlotModel pmPower = new PlotModel();
            LineSeries pwrLineSeries = new LineSeries();
            pwrLineSeries.LineJoin = LineJoin.Round;
            pmPower.Series.Add(pwrLineSeries);
            pmPower.Axes.Add(new LinearAxis { Maximum = 260, Minimum = 0, Position = AxisPosition.Left, Title = "Intensität" });
            pmPower.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Dauer [ms]" });

            if (call.PowerData != null)
            {
                pwrLineSeries.Points.AddRange(call.PowerData.Select((b, i) => new DataPoint(i * 0.251, b)));
            }

            //if (call.OriginalCalls.Count > 1)
            //{
            //    int leftSum = call.OriginalCalls[0].PowerData.Length;
            //    for (int i = 0; i < call.OriginalCalls.Count-1; i++)
            //    {
            //        int width = (int)Math.Round((call.OriginalCalls[i + 1].StartTimeMs - call.OriginalCalls[i].EndTimeMs) / 0.251);
            //        double left = leftSum* 0.251;
            //        double right = left + (width* 0.251);
            //        RectangleAnnotation annotation = new RectangleAnnotation { Fill = OxyColors.LightGray, Layer = AnnotationLayer.BelowAxes, MinimumX = left, MaximumX = right, MinimumY = 0, MaximumY = 260 };
            //        //LineAnnotation lineAnnotation = new LineAnnotation { Color = OxyColors.Red, StrokeThickness = 2, LineStyle = LineStyle.Dash, Type = LineAnnotationType.Vertical, X = linePos };
            //        pmPower.Annotations.Add(annotation);
            //        leftSum = leftSum + width + call.OriginalCalls[i].PowerData.Length;
            //    }
            //}


            OnlineFilter filter = OnlineFilter.CreateBandstop(ImpulseResponse.Infinite, 20, 20, 150);
            
            LineSeries pwrLineSeriesAvg = new LineSeries();
            pwrLineSeriesAvg.LineJoin = LineJoin.Round;
            pwrLineSeriesAvg.Color = OxyColors.Blue;
            pmPower.Series.Add(pwrLineSeriesAvg);

            //double[] input = call.PowerData.Select(d => (double)d).ToArray();
            //double[] data = filter.ProcessSamples(input);
            //pwrLineSeriesAvg.Points.AddRange(data.Select((d, i) => new DataPoint(i * 0.251, d)));

            int[] window = new int[] { 2, 4, 8, 4, 2 };
            int divisor = window.Sum();

            byte[] input = call.PowerData;
            for (int i = 0; i < input.Length; i++)
            {
                double sum = 0;
                for (int j = 0; j < 5; j++)
                {
                    int x = i + (j - 2);
                    double q;
                    if (x < 0)
                    {
                        q = input[0];
                    }
                    else if (x >= input.Length)
                    {
                        q = input[input.Length - 1];
                    }
                    else
                    {
                        q = input[x];
                    }
                    sum += q * window[j];
                }
                pwrLineSeriesAvg.Points.Add(new DataPoint(i * 0.251, sum / divisor));
            }


            LineSeries pwrLineSeriesAvg2 = new LineSeries();
            pwrLineSeriesAvg2.LineJoin = LineJoin.Round;
            pwrLineSeriesAvg2.Color = OxyColors.Crimson;
            pmPower.Series.Add(pwrLineSeriesAvg2);

            int avgCount = 7;
            int[] ringBuffer = Enumerable.Repeat(-1, avgCount).ToArray();
            int bufferIndex = 0;
            for (int i = 0; i < call.PowerData.Length; i++)
            {
                ringBuffer[bufferIndex++] = call.PowerData[i];
                if (bufferIndex >= ringBuffer.Length)
                {
                    bufferIndex = 0;
                }

                if (i > 4)
                {
                    int c = 0;
                    double mAvg = 0;
                    for (int j = 0; j < ringBuffer.Length; j++)
                    {
                        if (ringBuffer[j] >= 0)
                        {
                            c++;
                            mAvg += ringBuffer[j];
                        }
                    }
                    pwrLineSeriesAvg2.Points.Add(new DataPoint((i-4) * 0.251, mAvg/c));
                }
            }


            LineAnnotation lineAnnotation = new LineAnnotation { Color = OxyColors.Red, StrokeThickness = 2, LineStyle = LineStyle.Dash, Type = LineAnnotationType.Horizontal, Y = call.AveragePower };
            pmPower.Annotations.Add(lineAnnotation);

            Power = pmPower.AddStyles();


            PlotModel pmFreq = new PlotModel();
            ColumnSeries freqSeries = new ColumnSeries();
            pmFreq.Series.Add(freqSeries);
            CategoryAxis item = new CategoryAxis();
            item.Position = AxisPosition.Bottom;
            item.Title = "Frequenz [kHz]";
            item.GapWidth = 0.1;
            item.IsTickCentered = true;
            item.LabelFormatter = i => LogAnalyzer.GetFrequencyFromFftBin((int)i).ToString(CultureInfo.CurrentCulture);
            item.MajorGridlineThickness = 0;
            pmFreq.Axes.Add(item);

            if (call.FftData != null)
            {
                freqSeries.Items.AddRange(call.FftData.Select((f, i) =>
                {
                    ColumnItem columnItem = new ColumnItem(f, i);
                    columnItem.Color = i == call.MaxFrequencyBin ? OxyColors.Red : OxyColors.Green;
                    return columnItem;
                }));
            }
            Frequency = pmFreq.AddStyles();
        }
    }
}