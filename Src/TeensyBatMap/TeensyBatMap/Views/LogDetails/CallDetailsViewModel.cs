using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using TeensyBatMap.Domain;
using TeensyBatMap.Domain.Bins;
using TeensyBatMap.Views.EditLog;

using WinRtLib;

namespace TeensyBatMap.Views.LogDetails
{
	public class CallDetailsViewModel : PivotModelBase<LogDetailsPageModel>
	{
		public CallDetailsViewModel(LogDetailsPageModel parentModel) : base(parentModel)
		{
			FrequencyRange = new Range<uint>(0, 100);
			FrequencyRange.PropertyChanged += async (s, e) => await UpdateBins();

			IntensityRange = new Range<uint>(0, 1024);
			IntensityRange.PropertyChanged += async (s, e) => await UpdateBins();

			DurationRange = new Range<uint>(0, 100);
			DurationRange.PropertyChanged += async (s, e) => await UpdateBins();

			TimeRange = new Range<uint>(0, 100);
			TimeRange.PropertyChanged += async (s, e) => await UpdateBins();

			Func<BatCall, bool> filter = c => IntensityRange.Contains(c.MaxPower) && FrequencyRange.Contains(c.MaxFrequency) && DurationRange.Contains(c.Duration / 1000) && TimeRange.Contains(c.StartTimeMs);
			FreqBins = new UintBinCollection(100, b => b.MaxFrequency, filter);
			IntensityBins = new UintBinCollection(200, b => b.MaxPower, filter);
			CallDurationBins = new UintBinCollection(100, b => b.Duration / 1000, filter);
			TimeBins = new TimeCallBinCollection(200, ParentViewModel.BatLog.LogStart, filter);
		}

		public Range<uint> FrequencyRange { get; }
		public Range<uint> IntensityRange { get; }
		public Range<uint> DurationRange { get; }
		public Range<uint> TimeRange { get; }
		public UintBinCollection FreqBins { get; private set; }
		public UintBinCollection IntensityBins { get; private set; }
		public UintBinCollection CallDurationBins { get; private set; }
		public TimeCallBinCollection TimeBins { get; private set; }


		public string FilterText
		{
			get
			{
				int filteredCount = TimeBins.Bins.Sum(b => b.FilteredCount);
				return string.Format(CultureInfo.CurrentCulture, "Resultat (Anzahl Rufe über die Zeit), Verwendete Daten: {0} / {1}", filteredCount, ParentViewModel?.BatCalls?.Count() ?? 0);
			}
		}

		public ObservableCollection<KeyValuePair<DateTime, uint>> Freq { get; set; }

		protected async Task UpdateBins()
		{
			await Task.Run(() =>
			{
				FreqBins.Refresh();
				CallDurationBins.Refresh();
				IntensityBins.Refresh();
				TimeBins.Refresh();
			});
			OnPropertyChanged(nameof(FilterText));
		}

		public override async Task Initialize()
		{
			if (ParentViewModel != null)
			{
				IEnumerable<BatCall> batCalls = ParentViewModel.BatCalls;
				await Task.Run(() =>
				{
					FreqBins.LoadBins(batCalls);
					IntensityBins.LoadBins(batCalls);
					CallDurationBins.LoadBins(batCalls);
					TimeBins.LoadBins(batCalls);
				});

				FrequencyRange.Set(FreqBins.Range);
				IntensityRange.Set(IntensityBins.Range);
				DurationRange.Set(CallDurationBins.Range);
				TimeRange.Set(TimeBins.Range);


				Freq = new ObservableCollection<KeyValuePair<DateTime, uint>>(batCalls.Select(c => new KeyValuePair<DateTime, uint>(c.StartTime, c.MaxFrequency)));
				OnPropertyChanged(nameof(Freq));
				OnPropertyChanged(nameof(FilterText));
			}
		}
	}
}