// 
// Teensy Bat Explorer - Copyright(C) 2020 Meinrad Jean-Richard
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MaterialDesignThemes.Wpf;

using Microsoft.Win32;

using TeensyBatExplorer.Core;
using TeensyBatExplorer.Core.Commands;
using TeensyBatExplorer.Core.Models;
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
        private readonly List<string> _filesToAdd = new List<string>();
        private BatLogViewModel _selectedLog;

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

        public BatLogViewModel SelectedLog
        {
            get => _selectedLog;
            set
            {
                if (!Equals(value, _selectedLog))
                {
                    _selectedLog = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<BatLogViewModel> BatLogs { get; }

        private async Task SaveLogs()
        {
            using (BusyState busyState = BeginBusy("Füge Logs zum Projekt hinzu..."))
            {
                await Task.Run(async () =>
                {
                    Progress<CountProgress> progress = new Progress<CountProgress>(async p => await busyState.Update(p));
                    await _addLogsCommand.ExecuteAsync(_projectManager, BatLogs.Where(b => b.Selected).Select(v => v.DataFile), progress, CancellationToken.None);
                });
            }
        }

        private async Task AddLogs()
        {
            using (BusyState busyState = BeginBusy("Logdateien öffnen..."))
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
                        BatDataFile batDataFile = new BatDataFile();
                        batDataFile.Filename = Path.GetFileName(file);
                        await busyState.Update($"Lade '{batDataFile.Filename}'...", i, openPicker.FileNames.Length);

                        try
                        {
                            await Task.Run(async () => await _logReader.Load(file, batDataFile));
                            BatLogs.Add(new BatLogViewModel(batDataFile));
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
            await AddLogs();
        }
    }
}