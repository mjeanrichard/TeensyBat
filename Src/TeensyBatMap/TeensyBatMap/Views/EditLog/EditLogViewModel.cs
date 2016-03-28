using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Windows.UI.Xaml.Navigation;

using Syncfusion.Data.Extensions;

using TeensyBatMap.Common;
using TeensyBatMap.Database;
using TeensyBatMap.Domain;
using TeensyBatMap.ViewModels;

namespace TeensyBatMap.Views.EditLog
{
	public class EditLogViewModel : BaseViewModel
	{
		private readonly BatContext _db;
		private readonly NavigationHelper _navigationHelper;

		private ObservableCollection<BatCallViewModel> _calls;
		private BatCallViewModel _selectedCall;

		private DateTimeOffset _startDate;
		private TimeSpan _startTime;

		public EditLogViewModel() : this(DesignData.CreateBatLog())
		{
			Calls = BatLog.Calls.Select((c, i) => new BatCallViewModel(BatLog, c, i)).ToObservableCollection();
		}

		private EditLogViewModel(BatNodeLog batLog)
		{
			BatLog = batLog;

			SaveCommand = new RelayCommand(async () => await SaveAction());
			CancelCommand = new RelayCommand(GoBack);
			ToggleEnabledCommand = new RelayCommand(() =>
			{
				if (SelectedCall != null)
				{
					SelectedCall.Enabled = !SelectedCall.Enabled;
				}
			});

			StartDate = new DateTimeOffset(BatLog.LogStart.Date);
			StartTime = batLog.LogStart.TimeOfDay;
		}

		public EditLogViewModel(NavigationEventArgs navigation, BatContext db, NavigationHelper navigationHelper)
			: this((BatNodeLog)navigation.Parameter)
		{
			_db = db;
			_navigationHelper = navigationHelper;
		}

		protected override async Task InitializeInternal()
		{
			if (_db != null)
			{
				IEnumerable<BatCall> batCalls = await _db.LoadCalls(BatLog, true);
				Calls = batCalls.Select((c, i) => new BatCallViewModel(BatLog, c, i)).ToObservableCollection();
			}
		}

		private async Task SaveAction()
		{
			using (MarkBusy())
			{
				BatLog.LogStart = StartDate.Add(StartTime).DateTime;
				await _db.SaveChangesAsync();
			}
			GoBack();
		}

		public void GoBack()
		{
			_navigationHelper.GoBack();
		}

		public override string Titel
		{
			get { return BatLog.Name + " bearbeiten"; }
		}

		public RelayCommand SaveCommand { get; private set; }

		public RelayCommand CancelCommand { get; private set; }

		public RelayCommand ToggleEnabledCommand { get; private set; }

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

		public ObservableCollection<BatCallViewModel> Calls
		{
			get { return _calls; }
			private set
			{
				_calls = value;
				OnPropertyChanged();
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
}