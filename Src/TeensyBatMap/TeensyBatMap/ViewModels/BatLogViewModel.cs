using System.ComponentModel;
using System.Runtime.CompilerServices;
using TeensyBatMap.Domain;
using TeensyBatMap.Domain.Bins;
using WinRtLib;

namespace TeensyBatMap.ViewModels
{
	public class BatLogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangedEventHandler BinsChanged;
        private readonly BatNodeLog _batNodeLog;
        private readonly TimeCallBinCollection _bins;
        private Range<int> _frequencyRange;
        private Range<int> _intensityRange;

        public BatLogViewModel(BatNodeLog batNodeLog)
        {
            FrequencyRange = new Range<int>(0, 100);
            IntensityRange = new Range<int>(0, 1024);

            _batNodeLog = batNodeLog;
            _bins = new TimeCallBinCollection(300, batNodeLog.LogStart, c => true);
        }

        public BatNodeLog BatNodeLog
        {
            get { return _batNodeLog; }
        }

        public string Titel
        {
            get { return _batNodeLog.Name; }
        }

        public int CallCount
        {
            get { return _batNodeLog.CallCount; }
        }

        public TimeCallBinCollection Bins
        {
            get { return _bins; }
        }

        public Range<int> FrequencyRange
        {
            get { return _frequencyRange; }
            set
            {
                _frequencyRange = value;
                OnPropertyChanged();
                OnBinsChanged();
            }
        }

        public Range<int> IntensityRange
        {
            get { return _intensityRange; }
            set
            {
                _intensityRange = value;
                OnPropertyChanged();
                OnBinsChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnBinsChanged()
        {
            if (Bins == null)
            {
                return;
            }
            foreach (UintBin bin in Bins.Bins)
            {
                if (bin != null)
                {
                    bin.Refresh();
                }
            }

            BinsChanged?.Invoke(this, new PropertyChangedEventArgs("Bins"));
        }
    }
}