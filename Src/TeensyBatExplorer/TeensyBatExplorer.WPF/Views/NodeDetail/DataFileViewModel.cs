// 
// Teensy Bat Explorer - Copyright(C)  Meinrad Jean-Richard
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

using System.ComponentModel;
using System.Runtime.CompilerServices;

using TeensyBatExplorer.Core;
using TeensyBatExplorer.Core.Models;
using TeensyBatExplorer.WPF.Annotations;

namespace TeensyBatExplorer.WPF.Views.NodeDetail
{
    public class DataFileViewModel : INotifyPropertyChanged
    {
        public BatDataFile DataFile { get; private set; }

        public string Filename => DataFile.Filename;
        public string FileCreateTime => DataFile.FileCreateTime.ToFormattedString();
        public string ReferenceTime => DataFile.ReferenceTime.ToFormattedString();
        public string Version => $"FW:{DataFile.FirmwareVersion}, HW: {DataFile.HardwareVersion}";

        public int EntryCount { get; private set; }

        public void Load(BatDataFile dataFile, int entryCount)
        {
            DataFile = dataFile;
            EntryCount = entryCount;
            OnPropertyChanged(nameof(Filename));
            OnPropertyChanged(nameof(ReferenceTime));
            OnPropertyChanged(nameof(FileCreateTime));
            OnPropertyChanged(nameof(Version));
            OnPropertyChanged(nameof(DataFile));
            OnPropertyChanged(nameof(EntryCount));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}