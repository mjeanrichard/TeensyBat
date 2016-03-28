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
	public class CallDetailsPivotModel : PivotModelBase<EditLogViewModel>
	{
		public CallDetailsPivotModel(EditLogViewModel parentViewModel) : base(parentViewModel)
		{
			ToggleEnabledCommand = new RelayCommand(() =>
			{
				if (SelectedCall != null)
				{
					SelectedCall.Enabled = !SelectedCall.Enabled;
				}
			});
		}

		private ObservableCollection<BatCallViewModel> _calls;
		private BatCallViewModel _selectedCall;

		public override async Task Initialize()
		{
			IEnumerable<BatCall> batCalls;
			if (ParentViewModel.Db != null)
			{
				batCalls = await ParentViewModel.Db.LoadCalls(ParentViewModel.BatLog, true);
			}
			else
			{
				batCalls = ParentViewModel.BatLog.Calls;
			}
			Calls = batCalls.Select((c, i) => new BatCallViewModel(ParentViewModel.BatLog, c, i)).ToObservableCollection();
			if (Calls.Any())
			{
				SelectedCall = Calls.First();
			}
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


	public class EditLogViewModel : BaseViewModel
	{
		private readonly NavigationHelper _navigationHelper;

		private DateTimeOffset _startDate;
		private TimeSpan _startTime;

		public EditLogViewModel() : this(DesignData.CreateBatLog())
		{
			CallDetailsPivotModel.Initialize().Wait();
		}

		private EditLogViewModel(BatNodeLog batLog)
		{
			BatLog = batLog;

			SaveCommand = new RelayCommand(async () => await SaveAction());
			CancelCommand = new RelayCommand(GoBack);

			StartDate = new DateTimeOffset(BatLog.LogStart.Date);
			StartTime = batLog.LogStart.TimeOfDay;

			CallDetailsPivotModel = new CallDetailsPivotModel(this);
		}

		public EditLogViewModel(NavigationEventArgs navigation, BatContext db, NavigationHelper navigationHelper)
			: this((BatNodeLog)navigation.Parameter)
		{
			Db = db;
			_navigationHelper = navigationHelper;
		}

		protected override async Task InitializeInternal()
		{
			await CallDetailsPivotModel.Initialize();
		}

		private async Task SaveAction()
		{
			using (MarkBusy())
			{
				BatLog.LogStart = StartDate.Add(StartTime).DateTime;
				await Db.SaveChangesAsync();
			}
			GoBack();
		}

		public void GoBack()
		{
			_navigationHelper.GoBack();
		}

		public RelayCommand SaveCommand { get; private set; }

		public RelayCommand CancelCommand { get; private set; }

		public BatNodeLog BatLog { get; set; }

		public CallDetailsPivotModel CallDetailsPivotModel { get; }

		public BatContext Db { get; }

		public override string Titel
		{
			get { return BatLog.Name + " bearbeiten"; }
		}


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
	}
}