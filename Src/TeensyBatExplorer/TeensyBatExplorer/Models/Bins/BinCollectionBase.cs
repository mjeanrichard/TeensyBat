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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace TeensyBatExplorer.Models.Bins
{
    public abstract class BinCollectionBase<TBin, TElement> : IEnumerable<TBin>, INotifyCollectionChanged, INotifyPropertyChanged where TBin : Bin<TElement>
    {
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

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void LoadBinsInternal(IEnumerable<TElement> elements)
        {
            _bins = new TBin[ActualBinCount];
            for (uint i = 0; i < _bins.Length; i++)
            {
                _bins[i] = CreateBin(i);
            }

            foreach (TElement element in elements)
            {
                uint bin = GetBinNumber(element);
                if (bin >= _bins.Length)
                {
                    throw new InvalidOperationException("Bin number must not be greater or equal than the bin size.");
                }
                _bins[bin].Add(element);
            }
            OnBinsUpdated();
        }

        protected abstract TBin CreateBin(uint binNumber);
        protected abstract uint GetBinNumber(TElement element);

        public TBin GetBin(TElement element)
        {
            uint binNumber = GetBinNumber(element);
            return _bins[binNumber];
        }

        public TBin GetBinByIndex(uint index)
        {
            return _bins[index];
        }

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