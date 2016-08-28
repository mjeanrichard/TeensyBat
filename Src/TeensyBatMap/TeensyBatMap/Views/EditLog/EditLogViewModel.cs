using System;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.UI.Xaml.Navigation;

using TeensyBatMap.Common;
using TeensyBatMap.Database;
using TeensyBatMap.Domain;
using TeensyBatMap.Map;
using TeensyBatMap.ViewModels;

using UniversalMapControl;
using UniversalMapControl.Projections;
using UniversalMapControl.Interfaces;

namespace TeensyBatMap.Views.EditLog
{
	public class EditLogViewModel : BaseViewModel
	{
		private readonly NavigationHelper _navigationHelper;

		private DateTimeOffset _startDate;
		private TimeSpan _startTime;
		private ILocation _location;
		private SwissGridLocation _swissGridLocation;

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
			Location = new Wgs84Location(BatLog.Longitude, BatLog.Latitude);
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

		public ILocation Location
		{
			get { return _location; }
			set
			{
				_location = value;
				BatLog.Longitude = _location.Longitude;
				BatLog.Latitude = _location.Latitude;
				_swissGridLocation = SwissGridLocation.FromWgs84Approx(_location);
				OnPropertyChanged();
				OnPropertyChanged(nameof(SwissGridLocation));
			}
		}

		public SwissGridLocation SwissGridLocation
		{
			get { return _swissGridLocation; }
			set
			{
				_swissGridLocation = value;
				_location = SwissGridHelper.ToWgs84(value);
				OnPropertyChanged();
				OnPropertyChanged(nameof(Location));
			}
		}
	}
}