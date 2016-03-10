using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinRtLib
{
    public class Range<T> : INotifyPropertyChanged
        where T : struct, IComparable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private T _maximum;
        private T _minimum;

        public Range(T minimum, T maximum)
        {
            _maximum = maximum;
            _minimum = minimum;
        }

        public Range(Range<T> range) : this(range.Minimum, range.Maximum)
        {}

        public T Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = value;
                OnPropertyChanged();
            }
        }

        public T Minimum
        {
            get { return _minimum; }
            set
            {
                _minimum = value;
                OnPropertyChanged();
            }
        }

        public void Set(Range<T> otherRange)
        {
            Minimum = otherRange.Minimum;
            Maximum = otherRange.Maximum;
        }

        protected bool Equals(Range<T> other)
        {
            return Maximum.Equals(other.Maximum) && Minimum.Equals(other.Minimum);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((Range<T>)obj);
        }

        public bool Contains(T value)
        {
            Comparer<T> comparer = Comparer<T>.Default;
            return comparer.Compare(value, Minimum) >= 0 && comparer.Compare(value, Maximum) <= 0;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}