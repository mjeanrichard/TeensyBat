using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MaterialDesignThemes.Wpf;

using Microsoft.Win32;

using Nito.Mvvm;

using TeensyBatExplorer.Core;
using TeensyBatExplorer.Core.Commands;
using TeensyBatExplorer.Core.Models;
using TeensyBatExplorer.WPF.Annotations;
using TeensyBatExplorer.WPF.Infrastructure;

namespace TeensyBatExplorer.WPF.Views.AddLogs
{
    public class AddLogsViewModel : BaseViewModel
    {   
        private readonly NavigationService _navigationService;
        private readonly LogReader _logReader;
        private readonly ProjectManager _projectManager;
        private readonly AddLogsCommand _addLogsCommand;
        private readonly ISnackbarMessageQueue _snackbarMessageQueue;
        private BatProject _batProject;
        private List<string> _filesToAdd = new List<string>();

        public AddLogsViewModel(NavigationService navigationService, LogReader logReader, ProjectManager projectManager, AddLogsCommand addLogsCommand, ISnackbarMessageQueue snackbarMessageQueue)
        {
            _navigationService = navigationService;
            _logReader = logReader;
            _projectManager = projectManager;
            _addLogsCommand = addLogsCommand;
            _snackbarMessageQueue = snackbarMessageQueue;

            BatLogs = new ObservableCollection<BatLogViewModel>();

            AddToolbarButton(new ToolBarButton(AddLogs, PackIconKind.FilePlusOutline, "Add Logs"));
            AddToolbarButton(new ToolBarButton(SaveLogs, PackIconKind.ContentSave, "Save"));
        }

        public BatProject BatProject { get; private set; }

        public ObservableCollection<BatLogViewModel> BatLogs { get; }

        private async Task SaveLogs()
        {
            using (BusyState busyState = BeginBusy())
            {
                await Task.Run(async () =>
                {
                    Progress<CountProgress> progress = new Progress<CountProgress>(async p => await busyState.Update(p));
                    await _addLogsCommand.ExecuteAsync(_projectManager, BatLogs.Select(v => v.Log), progress, CancellationToken.None);
                });
            }
        }

        private async Task AddLogs()
        {
            using (BusyState busyState = BeginBusy())
            {
                OpenFileDialog openPicker = new OpenFileDialog();
                openPicker.Multiselect = true;
                openPicker.DefaultExt = ".dat";
                openPicker.Filter = "Logfiles (*.dat)|*.dat|Alle Dateien|*.*";
                bool? pickerResult = openPicker.ShowDialog();
                if (pickerResult.HasValue && pickerResult.Value && openPicker.FileNames.Length > 0)
                {
                    int i = 0;
                    foreach (string file in openPicker.FileNames)
                    {
                        _filesToAdd.Add(file);
                        BatLog batLog = new BatLog();
                        batLog.Filename = Path.GetFileName(file);
                        await busyState.Update($"Lade '{batLog.Filename}'...", i, openPicker.FileNames.Length);

                        try
                        {
                            await Task.Run(async () => await _logReader.Load(file, batLog));
                            BatLogs.Add(new BatLogViewModel(batLog));
                        }
                        catch (LogFileFormatException ex)
                        {
                            _snackbarMessageQueue.Enqueue($"Die Datei '{Path.GetFileName(file)}' konnte nicht geladen werden:\n{ex.Message}");
                        }

                        i++;
                    }
                }
            }
        }


        public override async Task Load()
        {
            await RunOnUiThread(AddLogs);
        }

    }

    public class BatLogViewModel : INotifyPropertyChanged
    {
        private bool _selected;

        public BatLogViewModel(BatLog batLog)
        {
            Log = batLog;
        }

        public string Node => Log.NodeNumber.ToString();
        public string Datum => Log.StartTime.ToString("dd.MM.yy hh:mm:ss");
        public string CallCount => Log.Calls.Count.ToString();

        public bool Selected
        {
            get => _selected;
            set
            {
                if (value == _selected) return;
                _selected = value;
                OnPropertyChanged();
            }
        }

        public BatLog Log { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
