using System.Collections.Generic;
using System.Linq;
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
		private readonly BatContext _db;
		private readonly NavigationHelper _navigationHelper;
		private BatNodeLog _batLog;

		public EditLogViewModel() : this(DesignData.CreateBatLog())
		{
			Calls = _batLog.Calls;
			BatInfos = new List<BatInfo>();
			EditCallsModel.Initialize().Wait();
			ViewInfosModel.Initialize().Wait();
		}

		private EditLogViewModel(BatNodeLog batLog)
		{
			_batLog = batLog;

			SaveCommand = new RelayCommand(async () => await SaveAction());
			CancelCommand = new RelayCommand(GoBack);

			EditCallsModel = new EditCallsPivotModel(_batLog, this);
			ViewInfosModel = new ViewInfosPivotModel(_batLog, this);
		}

		public EditLogViewModel(NavigationEventArgs navigation, BatContext db, NavigationHelper navigationHelper)
			: this((BatNodeLog)navigation.Parameter)
		{
			_db = db;
			_navigationHelper = navigationHelper;
		}

		public EditCallsPivotModel EditCallsModel { get; }
		public ViewInfosPivotModel ViewInfosModel { get; }

		protected override async Task InitializeInternal()
		{
			if (_db != null)
			{
				IEnumerable<BatCall> batCalls = await _db.LoadCalls(_batLog);
				Calls = batCalls.ToList();
				IEnumerable<BatInfo> batInfos = await _db.LoadInfos(_batLog);
				BatInfos = batInfos.ToList();

				await ViewInfosModel.Initialize();
				await EditCallsModel.Initialize();
			}
		}

		public List<BatCall> Calls { get; private set; }
		public List<BatInfo> BatInfos { get; private set; }

		public override string Titel
		{
			get { return _batLog.Name + " bearbeiten"; }
		}

		public void GoBack()
		{
			_navigationHelper.GoBack();
		}

		public RelayCommand SaveCommand { get; private set; }
		public RelayCommand CancelCommand { get; private set; }



		private async Task SaveAction()
		{
			using (MarkBusy())
			{
				EditCallsModel.BeforeSave();
				ViewInfosModel.BeforeSave();
				await _db.SaveChangesAsync();
			}
			GoBack();
		}
	}
}