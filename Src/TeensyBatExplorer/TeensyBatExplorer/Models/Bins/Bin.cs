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
using System.ComponentModel;
using System.Linq;

namespace TeensyBatExplorer.Models.Bins
{
    public abstract class Bin<TElement> : INotifyPropertyChanged
    {
        private readonly List<TElement> _elements = new List<TElement>();
        private int _filteredCount;

        protected Bin(Func<TElement, bool> filter)
        {
            Filter = filter;
        }

        public List<TElement> Elements
        {
            get { return _elements; }
        }

        public Func<TElement, bool> Filter { get; set; }

        public double Value
        {
            get { return FilteredCount; }
        }

        public double SecondaryValue
        {
            get { return Count; }
        }

        public string Label { get; set; }
        public bool IsHighlighted { get; }

        public int Count
        {
            get { return Elements.Count; }
        }

        public int FilteredCount
        {
            get { return _filteredCount; }
            protected set
            {
                _filteredCount = value;
                //OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void Add(TElement element)
        {
            _elements.Add(element);
            if (Filter(element))
            {
                FilteredCount++;
            }
            //OnPropertyChanged("Count");
        }

        public virtual void Clear()
        {
            _elements.Clear();
            FilteredCount = 0;
            //OnPropertyChanged("Count");
        }

        public void Refresh()
        {
            FilteredCount = Elements.Count(Filter);
        }

        //protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        //{
        //    PropertyChangedEventHandler onPropertyChanged = PropertyChanged;
        //    if (onPropertyChanged != null)
        //    {
        //        //await NavigationService.RunOnUI(() => onPropertyChanged(this, new PropertyChangedEventArgs(propertyName)));
        //        CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => onPropertyChanged(this, new PropertyChangedEventArgs(propertyName)));
        //    }
        //}
    }
}