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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.Toolkit.Uwp.Helpers;

using TeensyBatExplorer.Core.BatLog;
using TeensyBatExplorer.Models.Bins;

using UniversalMapControl.Interfaces;

namespace TeensyBatExplorer.Views.Project
{
    public class TimeLogsModel : INotifyPropertyChanged
    {
        private readonly BatProject _project;
        private double _selectedBin;

        public TimeLogsModel(BatProject project)
        {
            Logs = new ObservableCollection<TimeLogModel>();
            _project = project;
            MaxBins = 100;
        }

        public double MaxBins { get; set; }

        public double SelectedBin
        {
            get { return _selectedBin; }
            set
            {
                _selectedBin = value;
                OnPropertyChanged();
                UpdateLogs();
            }
        }

        public ObservableCollection<TimeLogModel> Logs { get; private set; }

        public void Refresh()
        {
            ObservableCollection<TimeLogModel> logs = new ObservableCollection<TimeLogModel>();
            foreach (BatNode node in _project.Nodes)
            {
                logs.Add(new TimeLogModel(node, 100));
            }
            Logs = logs;
            OnPropertyChanged(nameof(Logs));
        }

        private void UpdateLogs()
        {
            foreach (TimeLogModel logModel in Logs)
            {
                logModel.UpdateTime((uint)SelectedBin);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual async void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            await DispatcherHelper.ExecuteOnUIThreadAsync(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }
    }

    public class TimeLogModel : INotifyPropertyChanged, IHasLocation
    {
        private readonly BatNode _node;
        private readonly TimeCallBinCollection _timeCallBins;
        private double _size;

        public TimeLogModel(BatNode node, int binCount)
        {
            _node = node;
            //_timeCallBins = new TimeCallBinCollection(binCount, node.LogStart, c => true);
            //_timeCallBins.LoadBins(node.Calls);
        }

        public double Size
        {
            get { return _size; }
            set
            {
                _size = Math.Max(value, 10);
                OnPropertyChanged();
            }
        }

        public ILocation Location
        {
            get { return _node.Location; }
        }

        public void UpdateTime(uint index)
        {
            UintBin bin = _timeCallBins.GetBinByIndex(index);
            Size = 10 + bin.Value;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}