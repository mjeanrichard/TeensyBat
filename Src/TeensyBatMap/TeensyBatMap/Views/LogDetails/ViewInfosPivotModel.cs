using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using TeensyBatMap.Views.EditLog;

namespace TeensyBatMap.Views.LogDetails
{
	public class ViewInfosPivotModel : PivotModelBase<LogDetailsPageModel>
	{
		private bool _isInitialized;

		public ViewInfosPivotModel(LogDetailsPageModel parentViewModel) : base(parentViewModel)
		{
		}

		public ObservableCollection<KeyValuePair<DateTime, decimal>> Voltage { get; private set; }
		public ObservableCollection<KeyValuePair<DateTime, uint>> SampleTime { get; private set; }

		public override async Task Initialize()
		{
			if (!_isInitialized)
			{
				Voltage = new ObservableCollection<KeyValuePair<DateTime, decimal>>(ParentViewModel.BatInfos.Select((b, i) => new KeyValuePair<DateTime, decimal>(b.Time, b.BatteryVoltage / 1000.0m)));
				SampleTime = new ObservableCollection<KeyValuePair<DateTime, uint>>(ParentViewModel.BatInfos.Select((b, i) => new KeyValuePair<DateTime, uint>(b.Time, b.SampleDuration)));
				OnPropertyChanged(nameof(Voltage));
				OnPropertyChanged(nameof(SampleTime));
				_isInitialized = true;
			}
		}
	}
}