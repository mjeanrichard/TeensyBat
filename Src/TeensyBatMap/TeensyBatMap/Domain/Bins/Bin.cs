using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using WinRtLib;

namespace TeensyBatMap.Domain.Bins
{
    public abstract class Bin<TElement> : INotifyPropertyChanged, IBin
    {
        public event PropertyChangedEventHandler PropertyChanged;
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