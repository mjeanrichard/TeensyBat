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
using System.Runtime.CompilerServices;

namespace TeensyBatExplorer.Models
{
    public class Range<T> : INotifyPropertyChanged
        where T : struct, IComparable
    {
        private T _maximum;
        private T _minimum;

        public Range(T minimum, T maximum)
        {
            _maximum = maximum;
            _minimum = minimum;
        }

        public Range(Range<T> range) : this(range.Minimum, range.Maximum)
        {
        }

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

        public event PropertyChangedEventHandler PropertyChanged;

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

        public override int GetHashCode()
        {
            var hashCode = -147281814;
            hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(_maximum);
            hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(_minimum);
            return hashCode;
        }
    }
}