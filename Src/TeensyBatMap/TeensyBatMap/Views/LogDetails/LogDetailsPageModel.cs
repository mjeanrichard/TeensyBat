using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Windows.UI.Xaml.Navigation;

using TeensyBatMap.Common;
using TeensyBatMap.Database;
using TeensyBatMap.Domain;
using TeensyBatMap.ViewModels;
using TeensyBatMap.Views.EditLog;

namespace TeensyBatMap.Views.LogDetails
{
	public class LogDetailsPageModel : BaseViewModel
	{
		private readonly BatContext _db;
		private readonly NavigationService _navigationService;

		public LogDetailsPageModel(NavigationEventArgs navigation, BatContext db, NavigationService navigationService)
			: this((BatNodeLog)navigation.Parameter)
		{
			_db = db;
			_navigationService = navigationService;
		}

		public LogDetailsPageModel() : this(DesignData.CreateBatLog())
		{
			CallDetailsViewModel.Initialize().Wait();
			ViewInfosPivotModel.Initialize().Wait();
		}

		protected LogDetailsPageModel(BatNodeLog batLog)
		{
			EditLogCommand = new RelayCommand(() => _navigationService.EditLog(BatLog));

			BatLog = batLog;
			BatCalls = new List<BatCall>();

			CallDetailsViewModel = new CallDetailsViewModel(this);
			ViewInfosPivotModel = new ViewInfosPivotModel(this);
		}

		public BatNodeLog BatLog { get; }
		public IEnumerable<BatCall> BatCalls { get; set; }
		public IEnumerable<BatInfo> BatInfos { get; set; }

		protected override async Task InitializeInternal()
		{
			BatCalls = await _db.LoadCalls(BatLog, false, true);
			BatInfos = await _db.LoadInfos(BatLog);

			await CallDetailsViewModel.Initialize();
			await ViewInfosPivotModel.Initialize();
		}

		public override string Titel
		{
			get { return BatLog.Name; }
		}

		public RelayCommand EditLogCommand { get; private set; }

		public CallDetailsViewModel CallDetailsViewModel { get; }
		public ViewInfosPivotModel ViewInfosPivotModel { get; }
	}
}