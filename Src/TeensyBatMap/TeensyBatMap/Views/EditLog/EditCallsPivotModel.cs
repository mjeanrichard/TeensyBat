using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using TeensyBatMap.Common;
using TeensyBatMap.Domain;
using TeensyBatMap.ViewModels;

namespace TeensyBatMap.Views.EditLog
{
	public class EditCallsPivotModel : PivotModelBase
	{
		private ObservableCollection<BatCallViewModel> _calls;
		private BatCallViewModel _selectedCall;
		private bool _isInitialized;

		private DateTimeOffset _startDate;
		private TimeSpan _startTime;

		public EditCallsPivotModel(BatNodeLog batLog, EditLogViewModel parentViewModel) : base(parentViewModel)
		{
			BatLog = batLog;
			StartDate = new DateTimeOffset(BatLog.LogStart.Date);
			StartTime = batLog.LogStart.TimeOfDay;
			ToggleEnabledCommand = new RelayCommand(() =>
			{
				if (SelectedCall != null)
				{
					SelectedCall.Enabled = !SelectedCall.Enabled;
				}
			});
		}

		public BatNodeLog BatLog { get; set; }

		public string Name
		{
			get { return BatLog.Name; }
			set
			{
				BatLog.Name = value;
				OnPropertyChanged();
			}
		}

		public string Description
		{
			get { return BatLog.Description; }
			set
			{
				BatLog.Description = value;
				OnPropertyChanged();
			}
		}

		public TimeSpan StartTime
		{
			get { return _startTime; }
			set
			{
				_startTime = value;
				OnPropertyChanged();
			}
		}

		public DateTimeOffset StartDate
		{
			get { return _startDate; }
			set
			{
				_startDate = value;
				OnPropertyChanged();
			}
		}

		public override string Titel
		{
			get { return "Details"; }
		}

		public override void BeforeSave()
		{
			BatLog.LogStart = StartDate.Add(StartTime).DateTime;
		}

		public RelayCommand ToggleEnabledCommand { get; private set; }

		public ObservableCollection<BatCallViewModel> Calls
		{
			get { return _calls; }
			private set
			{
				_calls = value;
				OnPropertyChanged();
			}
		}

		public override async Task Initialize()
		{
			if (!_isInitialized)
			{
				Calls = new ObservableCollection<BatCallViewModel>(ParentViewModel.Calls.Select((c, i) => new BatCallViewModel(BatLog, c, i + 1)));
				SelectedCall = Calls.FirstOrDefault();
				_isInitialized = true;
			}
		}

		public BatCallViewModel SelectedCall
		{
			get { return _selectedCall; }
			set
			{
				if (_selectedCall != value)
				{
					if (value != null)
					{
						value.Initialize();
					}
					_selectedCall = value;
					OnPropertyChanged();
				}
			}
		}

	}

	public class ViewInfosPivotModel : PivotModelBase
	{
		private bool _isInitialized;

		private ObservableCollection<BatInfo> _infos;
		
		public ViewInfosPivotModel(BatNodeLog batLog, EditLogViewModel parentViewModel) : base(parentViewModel)
		{
			BatLog = batLog;
		}

		public BatNodeLog BatLog { get; set; }



		public override string Titel
		{
			get { return "Infos"; }
		}

		public override void BeforeSave()
		{
		}

		public ObservableCollection<KeyValuePair<DateTime, decimal>> Voltage { get; private set; }

		public override async Task Initialize()
		{
			if (!_isInitialized)
			{
				Voltage = new ObservableCollection<KeyValuePair<DateTime, decimal>>(ParentViewModel.BatInfos.Select((b, i) => new KeyValuePair<DateTime, decimal>(b.Time, b.BatteryVoltage/1000.0m)));
				OnPropertyChanged(nameof(Voltage));
				_isInitialized = true;
			}
		}
	}

	public abstract class PivotModelBase : INotifyPropertyChanged
	{
		private readonly EditLogViewModel _parentViewModel;

		public EditLogViewModel ParentViewModel
		{
			get { return _parentViewModel; }
		}

		public event PropertyChangedEventHandler PropertyChanged;


		protected PivotModelBase(EditLogViewModel parentViewModel)
		{
			_parentViewModel = parentViewModel;
		}

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public abstract Task Initialize();

		protected IDisposable MarkBusy()
		{
			return _parentViewModel.MarkBusy();
		}

		public abstract string Titel { get; }

		public abstract void BeforeSave();

	}
}