using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Windows.UI.Xaml.Navigation;

using TeensyBatMap.Common;
using TeensyBatMap.Database;
using TeensyBatMap.Domain;
using TeensyBatMap.ViewModels;

namespace TeensyBatMap.Views.EditLog
{
	public class EditLogViewModel : BaseViewModel
	{
		private BatNodeLog _batLog;
		private readonly BatContext _db;
		private readonly NavigationHelper _navigationHelper;
		private PivotModelBase _selectedItem;

		public EditLogViewModel(NavigationEventArgs navigation, BatContext db, NavigationHelper navigationHelper)
		{
			_db = db;
			_navigationHelper = navigationHelper;

			_batLog = (BatNodeLog)navigation.Parameter;

			SaveCommand = new RelayCommand(async () => await SaveAction());
			CancelCommand = new RelayCommand(() => GoBack());


			PivotItems = new ObservableCollection<PivotModelBase>();
			PivotItems.Add(new EditCallsPivotModel(_batLog, this));
			PivotItems.Add(new EditCallsPivotModel(_batLog, this));
		}

		public PivotModelBase SelectedItem
		{
			get { return _selectedItem; }
			set
			{
				if (_selectedItem != value)
				{
					_selectedItem = value;
					using (MarkBusy())
					{
						_selectedItem.Initialize();
						OnPropertyChanged();
					}
				}
			}
		}

		public ObservableCollection<PivotModelBase> PivotItems { get; set; }

		public override async Task Initialize()
		{
			if (_db != null)
			{
				IEnumerable<BatCall> batCalls = await _db.LoadCalls(_batLog);
				Calls = batCalls.ToList();
			}
		}

		public List<BatCall> Calls { get; private set; }

		public override string Titel { get { return "asdfasdf"; } }

		public void GoBack()
		{
			_navigationHelper.GoBack();
		}

		public RelayCommand SaveCommand { get; private set; }
		public RelayCommand CancelCommand { get; private set; }


		private async Task SaveAction()
		{
			//using (MarkBusy())
			//{
			//	BatLog.Name = Name;
			//	BatLog.Description = Description;
			//	BatLog.LogStart = StartDate.Add(StartTime).DateTime;
			//	await _db.SaveChangesAsync();
			//}
			//ParentViewModel.GoBack();
		}

	}


	public class EditCallsPivotModel : PivotModelBase
	{
		private string _description;
		private string _name;
		private DateTimeOffset _startDate;
		private TimeSpan _startTime;
		private ObservableCollection<BatCallViewModel> _calls;
		private BatCallViewModel _selectedCall;
		private bool _isInitialized;

		//public EditLogPageModel()
		//	: this(DesignData.CreateBatLog())
		//{
		//	Calls = new ObservableCollection<BatCallViewModel>(BatLog.Calls.Select((c, i) => new BatCallViewModel(BatLog, c, i+1)));
		//	if (Calls.Any())
		//	{
		//		SelectedCall = Calls.First();
		//	}
		//}


		public EditCallsPivotModel(BatNodeLog batLog, EditLogViewModel parentViewModel) : base(parentViewModel)
		{
			BatLog = batLog;
			Name = batLog.Name;
			Description = batLog.Description;
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
			get { return "Details"; }
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

	}
}