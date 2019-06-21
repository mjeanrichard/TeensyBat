// 
// Teensy Bat Explorer - Copyright(C) 2018 Meinard Jean-Richard
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
using System.Linq;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.Pickers;

using Microsoft.Toolkit.Uwp.Helpers;

using OxyPlot;
using OxyPlot.Series;

using TeensyBatExplorer.Helpers.ViewModels;
using TeensyBatExplorer.Models;
using TeensyBatExplorer.Services;

namespace TeensyBatExplorer.Views.Project
{
    public class ProjectViewModel : BaseViewModel
    {
        private PlotModel _plotModel;
        private HeatMapSeries _heatMapSeries;
        private readonly LogReader _logReader;
        private List<BatLog2> _logs;
        private CallModel _selectedCall;
        private List<CallModel> _calls;

        public ProjectViewModel(LogReader logReader)
        {
            _logReader = logReader;
            OpenFileCommand = new AsyncCommand(OpenFile, this);
        }

        private async Task OpenFile()
        {
            using (MarkBusy())
            {
                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.ViewMode = PickerViewMode.List;
                openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
                openPicker.FileTypeFilter.Add(".dat");
                IReadOnlyList<StorageFile> files = await DispatcherHelper.ExecuteOnUIThreadAsync(async () => await openPicker.PickMultipleFilesAsync());
                if (files.Count > 0)
                {
                    foreach (StorageFile file in files)
                    {
                        await AddFile(file);
                    }
                }
            }
        }

        private async Task AddFile(StorageFile file)
        {
            _logs = await _logReader.Load(file);
            _calls = new List<CallModel>(_logs.Select(l => new CallModel(l)));
            OnPropertyChanged(nameof(Calls));
        }

        public IList<CallModel> Calls
        {
            get { return _calls; }
        }
        public CallModel SelectedCall { get{return _selectedCall;} set{Set(ref _selectedCall, value);} }

        public AsyncCommand OpenFileCommand { get; set; }
    }
}