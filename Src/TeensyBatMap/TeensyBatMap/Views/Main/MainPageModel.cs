using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Navigation;
using TeensyBatMap.Common;
using TeensyBatMap.Database;
using TeensyBatMap.Domain;
using TeensyBatMap.ViewModels;

namespace TeensyBatMap.Views.Main
{
    public class MainPageModel : BaseViewModel
    {
        private readonly DbManager _db;
        private readonly ObservableCollection<BatNodeLog> _logFiles = new ObservableCollection<BatNodeLog>();
        private readonly BatNodeLogReader _logReader;
        private readonly NavigationService _navigationService;
        private BatNodeLog _selectedItem;
        private bool _hasFiles;

        public MainPageModel()
        {
            _logFiles.Add(DesignData.CreateBatLog());
            _logFiles.Add(DesignData.CreateBatLog());
            OnPropertyChanged("HasFiles");
        }

        public MainPageModel(NavigationEventArgs navigation, DbManager db, NavigationService navigationService)
        {
            _logReader = new BatNodeLogReader();
            _db = db;
            _navigationService = navigationService;

            ImportFileCommand = new RelayCommand(async () => await ImportLogFile());
            DetailsCommand = new RelayCommand(() =>
            {
                if (SelectedItem != null)
                {
                    _navigationService.NavigateToLogDetails(SelectedItem);
                }
            }, () => SelectedItem != null);
        }

        public RelayCommand ImportFileCommand { get; private set; }
        public RelayCommand DetailsCommand { get; }

        public ObservableCollection<BatNodeLog> LogFiles
        {
            get { return _logFiles; }
        }

        public BatNodeLog SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                DetailsCommand.RaiseCanExecuteChanged();
            }
        }

        public bool HasFiles
        {
            get { return _logFiles.Count > 0; }
        }

        public override string Titel
        {
            get { return "Log Files"; }
        }

        private async Task RefreshLogFiles()
        {
            List<BatNodeLog> batNodeLogs = await _db.LoadAllLogs();
            _logFiles.Clear();
            foreach (BatNodeLog log in batNodeLogs)
            {
                _logFiles.Add(log);
            }
            OnPropertyChanged("HasFiles");
        }

        public async Task ImportLogFile()
        {
            using (MarkBusy())
            {
                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.ViewMode = PickerViewMode.List;
                openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
                openPicker.FileTypeFilter.Add(".csv");
                IReadOnlyList<StorageFile> files = await openPicker.PickMultipleFilesAsync();
                if (files.Count > 0)
                {
                    foreach (StorageFile file in files)
                    {
                        await AddFile(file);
                    }
                }
                OnPropertyChanged("HasFiles");
            }
        }

        private async Task AddFile(StorageFile file)
        {
            BatNodeLog batNodeLog = await _logReader.Load(file);
            batNodeLog.Name = file.DisplayName;
            await _db.InsertNewLog(batNodeLog);
            _logFiles.Add(batNodeLog);
            OnPropertyChanged("HasFiles");
        }

        public override async Task Initialize()
        {
            using (MarkBusy())
            {
                await RefreshLogFiles();
            }
        }
    }
}