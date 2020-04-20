using System;
using System.Collections;
using System.Collections.Generic;

namespace TeensyBatExplorer.Core.Models
{
    public class AutoSortingList<TElement, TKey> : IList<TElement>
    {
        private readonly Func<TElement, TKey> _keyLoader;
        private SortedList<TKey, TElement> _list = new SortedList<TKey, TElement>();

        public AutoSortingList(Func<TElement, TKey> keyLoader)
        {
            _keyLoader = keyLoader;
        }

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

        public int Count => _list.Count;

        public bool IsReadOnly => false;

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