using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;
using TeensyBatMap.Common;
using TeensyBatMap.Database;
using TeensyBatMap.Domain;
using TeensyBatMap.Domain.Bins;
using TeensyBatMap.ViewModels;
using WinRtLib;

namespace TeensyBatMap.Views.LogDetails
{
    public class LogDetailsPageModel : BaseViewModel
    {
        private readonly BatContext _db;
        private readonly NavigationService _navigationService;
        private IEnumerable<BatCall> _batCalls;

        public LogDetailsPageModel()
            : this(DesignData.CreateBatLog())
        {
            FrequencyRange.Minimum = 20;
            FrequencyRange.Maximum = 80;
            IntensityRange.Minimum = 300;
            IntensityRange.Maximum = 800;
        }

        public LogDetailsPageModel(NavigationEventArgs navigation, BatContext db, NavigationService navigationService)
            : this((BatNodeLog)navigation.Parameter)
        {
            _db = db;
            _navigationService = navigationService;
        }

        protected LogDetailsPageModel(BatNodeLog batLog)
        {
            EditLogCommand = new RelayCommand(() => _navigationService.EditLog(BatLog));

            BatLog = batLog;
            _batCalls = new List<BatCall>();

            FrequencyRange = new Range<uint>(0, 100);
            FrequencyRange.PropertyChanged += async (s, e) => await UpdateBins();

            IntensityRange = new Range<uint>(0, 1024);
            IntensityRange.PropertyChanged += async (s, e) => await UpdateBins();

            DurationRange = new Range<uint>(0, 100);
            DurationRange.PropertyChanged += async (s, e) => await UpdateBins();

            TimeRange = new Range<uint>(0, 100);
            TimeRange.PropertyChanged += async (s, e) => await UpdateBins();

			//BUG!
            Func<BatCall, bool> filter = c => IntensityRange.Contains(c.MaxPower) && FrequencyRange.Contains(c.MaxFrequency) && DurationRange.Contains(c.Duration / 1000) && TimeRange.Contains(c.StartTimeMs);
            FreqBins = new UintBinCollection(100, b => (uint)b.MaxFrequency, filter);
            IntensityBins = new UintBinCollection(200, b => (uint)b.MaxPower, filter);
            CallDurationBins = new UintBinCollection(100, b => b.Duration / 1000, filter);
            TimeBins = new TimeCallBinCollection(200, batLog.LogStart, filter);
        }

        public Range<uint> FrequencyRange { get; }
        public Range<uint> IntensityRange { get; }
        public Range<uint> DurationRange { get; }
        public Range<uint> TimeRange { get; }
        public BatNodeLog BatLog { get; }
        public UintBinCollection FreqBins { get; }
        public UintBinCollection IntensityBins { get; }
        public UintBinCollection CallDurationBins { get; }
        public TimeCallBinCollection TimeBins { get; }

        public override string Titel
        {
            get { return BatLog.Name; }
        }

        public RelayCommand EditLogCommand { get; private set; }

        public string FilterText
        {
            get
            {
                int filteredCount = TimeBins.Bins.Sum(b => b.FilteredCount);
                return string.Format(CultureInfo.CurrentCulture, "Resultat (Anzahl Rufe über die Zeit), Verwendete Daten: {0} / {1}", filteredCount, _batCalls.Count());
            }
        }

        protected async Task UpdateBins()
        {
            await Task.Run(() =>
            {
                FreqBins.Refresh();
                CallDurationBins.Refresh();
                IntensityBins.Refresh();
                TimeBins.Refresh();
            });
            OnPropertyChanged("FilterText");
        }

        public override async Task Initialize()
        {
            if (_db != null)
            {
                _batCalls = await _db.LoadCalls(BatLog);
                await Task.Run(() =>
                {
                    FreqBins.LoadBins(_batCalls);
                    IntensityBins.LoadBins(_batCalls);
                    CallDurationBins.LoadBins(_batCalls);
                    TimeBins.LoadBins(_batCalls);
                });

                FrequencyRange.Set(FreqBins.Range);
                IntensityRange.Set(IntensityBins.Range);
                DurationRange.Set(CallDurationBins.Range);
                TimeRange.Set(TimeBins.Range);

                OnPropertyChanged("FilterText");
            }
        }
    }
}