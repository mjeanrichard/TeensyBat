using System.ComponentModel;
using System.Runtime.CompilerServices;

using TeensyBatExplorer.Core.Models;
using TeensyBatExplorer.WPF.Annotations;

namespace TeensyBatExplorer.WPF.Views.AddLogs
{
    public class BatLogViewModel : INotifyPropertyChanged
    {
        private bool _selected;

        public BatLogViewModel(BatDataFile batDataFile)
        {
            DataFile = batDataFile;
            Selected = true;
        }

        public string Node => DataFile.NodeNumber.ToString();
        public string Datum => DataFile.StartTime.ToString("dd.MM.yy hh:mm:ss");
        public string CallCount => DataFile.Entries.Count.ToString();
        public string MessageCount => DataFile.LogMessages.Count.ToString();

        public bool Selected
        {
            get => _selected;
            set
            {
                if (value == _selected)
                {
                    return;
                }

                _selected = value;
                OnPropertyChanged();
            }
        }

        public BatDataFile DataFile { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}