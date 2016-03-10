using System;
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
        private readonly DbManager _db;
        private readonly NavigationHelper _navigationHelper;
        private string _description;
        private string _name;
        private DateTimeOffset _startDate;
        private TimeSpan _startTime;

        public EditLogPageModel()
            : this(DesignData.CreateBatLog())
        {}

        public EditLogPageModel(NavigationEventArgs navigation, DbManager db, NavigationHelper navigationHelper)
            : this((BatNodeLog)navigation.Parameter)
        {
            _db = db;
            _navigationHelper = navigationHelper;
        }

        protected EditLogPageModel(BatNodeLog batLog)
        {
            BatLog = batLog;
            Name = batLog.Name;
            Description = batLog.Description;
            StartDate = new DateTimeOffset(BatLog.LogStart.Date);
            StartTime = batLog.LogStart.TimeOfDay;
            SaveCommand = new RelayCommand(async () => await SaveAction());
            CancelCommand = new RelayCommand(() => _navigationHelper.GoBack());
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

        private async Task SaveAction()
        {
            using (MarkBusy())
            {
                BatLog.Name = Name;
                BatLog.Description = Description;
                BatLog.LogStart = StartDate.Add(StartTime).DateTime;
                await _db.UpdateLog(BatLog);
            }
            _navigationHelper.GoBack();
        }

        public override Task Initialize()
        {
            return Task.Delay(0);
        }
    }
}