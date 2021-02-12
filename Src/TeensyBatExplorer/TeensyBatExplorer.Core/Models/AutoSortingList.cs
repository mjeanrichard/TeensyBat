// 
// Teensy Bat Explorer - Copyright(C) 2020 Meinrad Jean-Richard
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

namespace TeensyBatExplorer.Core.Models
{
    public class AutoSortingList<TElement, TKey> : IList<TElement>
        where TKey : notnull
    {
        private readonly Func<TElement, TKey> _keyLoader;
        private readonly SortedList<TKey, TElement> _list = new();

        public AutoSortingList(Func<TElement, TKey> keyLoader)
        {
            _keyLoader = keyLoader;
        }

        public int Count => _list.Count;

        public bool IsReadOnly => false;

        IEnumerator<TElement> IEnumerable<TElement>.GetEnumerator()
        {
            return _list.Values.GetEnumerator();
        }

        public IEnumerator GetEnumerator()
        {
            return _list.Values.GetEnumerator();
        }

        public void Add(TElement item)
        {
            _list.Add(_keyLoader(item), item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(TElement item)
        {
            return _list.ContainsKey(_keyLoader(item));
        }

        public void CopyTo(TElement[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(TElement item)
        {
            return _list.Remove(_keyLoader(item));
        }

        public int IndexOf(TElement item)
        {
            return _list.IndexOfKey(_keyLoader(item));
        }

        public void Insert(int index, TElement item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        public TElement this[int index]
        {
            get => _list.Values[index];
            set => throw new NotImplementedException();
        }
    }
}