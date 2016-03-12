using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Windows.UI.Xaml.Navigation;

using TeensyBatMap.Common;
using TeensyBatMap.Database;
using TeensyBatMap.Domain;
using TeensyBatMap.ViewModels;

namespace TeensyBatMap.Views.EditLog
{
	public class EditLogPageModel : BaseViewModel
	{
		private readonly BatContext _db;
		private readonly NavigationHelper _navigationHelper;
		private string _description;
		private string _name;
		private DateTimeOffset _startDate;
		private TimeSpan _startTime;
		private ObservableCollection<BatCallViewModel> _calls;
		private BatCallViewModel _selectedCall;

		public EditLogPageModel()
			: this(DesignData.CreateBatLog())
		{
			Calls = new ObservableCollection<BatCallViewModel>(BatLog.Calls.Select((c, i) => new BatCallViewModel(BatLog, c, i+1)));
			if (Calls.Any())
			{
				SelectedCall = Calls.First();
			}
		}

		public EditLogPageModel(NavigationEventArgs navigation, BatContext db, NavigationHelper navigationHelper)
			: this((BatNodeLog)navigation.Parameter)
		{
			_db = db;
			_navigationHelper = navigationHelper;
		}

		private EditLogPageModel(BatNodeLog batLog)
		{
			BatLog = batLog;
			Name = batLog.Name;
			Description = batLog.Description;
			StartDate = new DateTimeOffset(BatLog.LogStart.Date);
			StartTime = batLog.LogStart.TimeOfDay;
			SaveCommand = new RelayCommand(async () => await SaveAction());
			CancelCommand = new RelayCommand(() => _navigationHelper.GoBack());
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
			get { return _name; }
			set
			{
				_name = value;
				OnPropertyChanged();
			}
		}

		public string Description
		{
			get { return _description; }
			set
			{
				_description = value;
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
			get { return BatLog.Name + " bearbeiten"; }
		}

		public RelayCommand SaveCommand { get; private set; }
		public RelayCommand CancelCommand { get; private set; }
		public RelayCommand ToggleEnabledCommand { get; private set; }

		private async Task SaveAction()
		{
			using (MarkBusy())
			{
				BatLog.Name = Name;
				BatLog.Description = Description;
				BatLog.LogStart = StartDate.Add(StartTime).DateTime;
				await _db.SaveChangesAsync();
			}
			_navigationHelper.GoBack();
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

		public override async Task Initialize()
		{
			if (_db != null)
			{
				IEnumerable<BatCall> calls = await _db.LoadCalls(BatLog);
				Calls = new ObservableCollection<BatCallViewModel>(calls.Select((c, i) => new BatCallViewModel(BatLog, c, i+1)));
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