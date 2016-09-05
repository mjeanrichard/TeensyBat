using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using TeensyBatMap.Common;
using TeensyBatMap.Domain;

using WinRtLib;

namespace TeensyBatMap.ViewModels
{
    public class SimpleIntBin : IBin
    {
        public SimpleIntBin(double value, string label, bool isPeak)
        {
            Value = value;
            Label = label;
            SecondaryValue = value;
            IsHighlighted = isPeak;
        }

        public double Value { get; }
        public double SecondaryValue { get; }
        public string Label { get; }
        public bool IsHighlighted { get; set; }
    }

    public class BatCallViewModel : INotifyPropertyChanged
    {
        private readonly BatNodeLog _log;
        private readonly BatCall _batCall;
        private readonly FftAnalyzer _fftAnalyzer;
        private bool _isInitialized;
        private ObservableCollection<SimpleIntBin> _frequencies;
        private PlotModel _power;

        public BatCallViewModel(BatNodeLog log, BatCall batCall, int index)
        {
            Index = index;
            _log = log;
            _batCall = batCall;
            _fftAnalyzer = new FftAnalyzer(2, 5);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool Enabled
        {
            get { return _batCall.Enabled; }
            set
            {
                if (_batCall.Enabled != value)
                {
                    _batCall.Enabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public string MainFrequencies
        {
            get { return string.Format(CultureInfo.CurrentCulture, "{0} kHz", _batCall.MaxFrequency); }
        }

        public string Duration
        {
            get { return string.Format(CultureInfo.CurrentCulture, "{0} ms", _batCall.Duration / 1000); }
        }

        public string StartTime
        {
            get { return _log.LogStart.AddMilliseconds(_batCall.StartTimeMs).ToString("HH:mm:ss.fff", CultureInfo.CurrentCulture); }
        }

        public string StartTimeFull
        {
            get { return _log.LogStart.AddMilliseconds(_batCall.StartTimeMs).ToString("dd.MM.yyyy HH:mm:ss.fff", CultureInfo.CurrentCulture); }
        }

        public BatCall BatCall
        {
            get { return _batCall; }
        }

        public int Index { get; private set; }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<SimpleIntBin> Frequencies
        {
            get { return _frequencies; }
            set
            {
                _frequencies = value;
                OnPropertyChanged();
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

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }
            _isInitialized = true;

            FftResult fftResult = _fftAnalyzer.Analyze(_batCall);

            SimpleIntBin[] simpleIntBins = new SimpleIntBin[fftResult.FftData.Length - 1];
            int iPeak = 0;
            for (int i = 1; i < fftResult.FftData.Length; i++)
            {
                //231kHz Sample Rate with 1024 Buffer -> 115.5 / 256 Bins => 0.451 kHz pro Bin
                simpleIntBins[i - 1] = new SimpleIntBin(fftResult.FftData[i], Math.Round(i * 0.451).ToString(CultureInfo.CurrentCulture), false);
                if ((iPeak < fftResult.Peaks.Length) && (fftResult.Peaks[iPeak] == i))
                {
                    simpleIntBins[i - 1].IsHighlighted = true;
                    iPeak++;
                }
            }

            Frequencies = new ObservableCollection<SimpleIntBin>(simpleIntBins);

            PlotModel pm = new PlotModel();
            LineSeries lineSeries = new LineSeries();
            pm.Series.Add(lineSeries);

            pm.Axes.Add(new LinearAxis { Maximum = 260, Minimum = 0, Position = AxisPosition.Left });

            if (_batCall.PowerData != null)
            {
                lineSeries.Points.AddRange(_batCall.PowerData.Select((b, i) => new DataPoint(i * 0.246, b)));
            }
            Power = pm.AddStyles();
        }
    }
}