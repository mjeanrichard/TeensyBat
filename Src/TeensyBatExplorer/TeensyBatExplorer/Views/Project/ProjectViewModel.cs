// 
// Teensy Bat Explorer - Copyright(C) 2017 Meinard Jean-Richard
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
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Navigation;

using Microsoft.Toolkit.Uwp.Helpers;

using TeensyBatExplorer.Common;
using TeensyBatExplorer.Core;
using TeensyBatExplorer.Core.BatLog;
using TeensyBatExplorer.Core.BatLog.Raw;

using UniversalMapControl.Interfaces;
using UniversalMapControl.Projections;

namespace TeensyBatExplorer.Views.Project
{
    public class ProjectViewModel : BaseViewModel
    {
        private readonly BatNodeLogReader _batNodeLogReader;
        private readonly LogAnalyzer _logAnalyzer;
        private readonly NavigationService _navigationService;
        private readonly BatProjectManager _batProjectManager;

        private BatProject _project;
        private ObservableCollection<NodeViewModel> _logs;
        private NodeViewModel _selectedNode;

        public ProjectViewModel(NavigationEventArgs parameter, BatNodeLogReader batNodeLogReader, LogAnalyzer logAnalyzer, NavigationService navigationService, BatProjectManager batProjectManager) : this()
        {
            _batNodeLogReader = batNodeLogReader;
            _logAnalyzer = logAnalyzer;
            _navigationService = navigationService;
            _batProjectManager = batProjectManager;


            if (parameter.Parameter != null)
            {
                _project = (BatProject)parameter.Parameter;
            }
            else
            {
                _project = new BatProject() { Name = "Neues Projekt" };
            }
        }

        public ProjectViewModel()
        {
            AddLogCommand = new AsyncCommand(AddLog, this);
            SaveCommand = new AsyncCommand(Save, this);
            TempCommand = new AsyncCommand(Temp, this);
            AnalyzeCommand = new AsyncCommand(Analyze, this);
        }

        public RelayCommand AddLogCommand { get; private set; }
        public RelayCommand AnalyzeCommand { get; private set; }
        public RelayCommand SaveCommand { get; private set; }
        public RelayCommand TempCommand { get; private set; }

        public ObservableCollection<NodeViewModel> Logs
        {
            get { return _logs; }
        }

        public TimeLogsModel TimeLogsModel { get; private set; }

        public NodeViewModel SelectedNode
        {
            get { return _selectedNode; }
            set
            {
                if (_selectedNode != value)
                {
                    if (_selectedNode != null)
                    {
                        _selectedNode.IsHighlighted = false;
                    }

                    _selectedNode = value;
                    if (_selectedNode != null)
                    {
                        _selectedNode.IsHighlighted = true;
                    }

                    OnPropertyChanged();
                }
            }
        }

        public BatProject Project
        {
            get { return _project; }
            set
            {
                _project = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get { return _project.Name; }
            set
            {
                _project.Name = value;
                OnPropertyChanged();
            }
        }

        private Task Temp()
        {
            int x = 649500;
            int y = 250500;
            int i = 0;
            foreach (BatNode node in _project.Nodes)
            {
                node.Location = new SwissGridLocation(x+i*100, y).ToWgs84Approx();
                i++;
                if (i > 9)
                {
                    i = 0;
                    y -= 100;
                }
            }
            return Task.CompletedTask;
        }

        private async Task Analyze()
        {
            Busy.MaxProgressValue = _project.Nodes.Count;
            Busy.ProgressValue = 0;
            foreach (BatNode node in _project.Nodes)
            {
                Busy.Message = $"Analysiere Node {Busy.ProgressValue+1} / {Busy.MaxProgressValue}...";
                await _batProjectManager.AnalyzeNode(_project, node);
                Busy.ProgressValue++;
            }

            _project.Refresh();
            TimeLogsModel.Refresh();
        }

        protected override Task InitializeInternalAsync()
        {
            _logs = new ObservableCollection<NodeViewModel>(_project.Nodes.Select(l => new NodeViewModel(_project, l, _navigationService, this)));

            TimeLogsModel = new TimeLogsModel(_project);

            return Task.CompletedTask;
        }

        private async Task Save()
        {
            await _batProjectManager.Save(_project);
        }

        private async Task AddLog()
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.List;
            openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            openPicker.FileTypeFilter.Add(".dat");

            IReadOnlyList<StorageFile> files = ImmutableArray<StorageFile>.Empty;
            await DispatcherHelper.ExecuteOnUIThreadAsync(async () => { files = await openPicker.PickMultipleFilesAsync(); });

            if (files.Count > 0)
            {
                Busy.MaxProgressValue = files.Count;
                Busy.ProgressValue = 0;
                foreach (StorageFile file in files)
                {
                    Busy.Message = $"Importiere Datei {file.Name}";
                    BatNode node = await _batProjectManager.AddNode(_project, file);
                    await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
                    {
                        _logs.Add(new NodeViewModel(_project, node, _navigationService, this));
                    });
                    Busy.ProgressValue++;
                }
            }
        }
    }
}