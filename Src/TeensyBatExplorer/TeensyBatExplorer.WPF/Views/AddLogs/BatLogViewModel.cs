using System.ComponentModel;
using System.Runtime.CompilerServices;

using TeensyBatExplorer.Core.Models;
using TeensyBatExplorer.WPF.Annotations;

namespace TeensyBatExplorer.WPF.Views.AddLogs
{
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
                if (value == _selected)
                {
                    return;
                }

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