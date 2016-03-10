using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace TeensyBatMap.Domain.Bins
{
    public abstract class BinCollectionBase<TBin, TElement> : IEnumerable<TBin>, INotifyCollectionChanged, INotifyPropertyChanged where TBin : Bin<TElement>
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;
        private TBin[] _bins;

        public BinCollectionBase(int maxBinCount, Func<TElement, bool> filter)
        {
            MaxBinCount = maxBinCount;
            ActualBinCount = MaxBinCount;
            Filter = filter;
            _bins = new TBin[0];
        }

        protected int MaxBinCount { get; set; }
        public int ActualBinCount { get; protected set; }
        public Func<TElement, bool> Filter { get; private set; }

        public IEnumerable<TBin> Bins
        {
            get { return _bins; }
        }

        protected virtual void LoadBinsInternal(IEnumerable<TElement> elements)
        {
            _bins = new TBin[ActualBinCount];
            for (int i = 0; i < _bins.Length; i++)
            {
                _bins[i] = CreateBin(i);
            }

            foreach (TElement element in elements)
            {
                int bin = GetBinNumber(element);
                if (bin >= _bins.Length)
                {
                    throw new InvalidOperationException("Bin number must not be greater or equal than the bin size.");
                }
                if (bin < 0)
                {
                    throw new InvalidOperationException("Bin number must be greater or equal to 0.");
                }
                _bins[bin].Add(element);
            }
            OnBinsUpdated();
        }

        protected abstract TBin CreateBin(int binNumber);
        protected abstract int GetBinNumber(TElement element);

        public IEnumerator<TBin> GetEnumerator()
        {
            return ((IEnumerable<TBin>)_bins).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler onPropertyChanged = PropertyChanged;
            if (onPropertyChanged != null)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => onPropertyChanged(this, new PropertyChangedEventArgs(propertyName)));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        public void OnBinsUpdated()
        {
            NotifyCollectionChangedEventHandler onCollectionChanged = CollectionChanged;
            if (onCollectionChanged != null)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => onCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        public void Refresh()
        {
            foreach (TBin bin in Bins)
            {
                bin.Refresh();
            }
            OnBinsUpdated();
        }
    }
}