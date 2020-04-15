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
    public class BatDataFileEntry
    {
        public int Id { get; set; }
        public int FftCount { get; set; }
        
        /// <summary>
        /// This Value is read from the LogFile and never changed
        /// </summary>
        public long StartTimeMillis { get; set; }

        /// <summary>
        /// This value should be used for all Calculations. It can differ from StartTimeMillis if an Offset is used.
        /// </summary>
        public long StartTimeMicros { get; set; }

        public long? PauseFromPrevEntryMicros { get; set; }

        public double MaxPeakFrequency { get; set; }
        public double AvgPeakFrequency { get; set; }
        public bool IsBat { get; set; }

        public int HighFreqSampleCount { get; set; }
        public int HighPowerSampleCount { get; set; }
        public int MaxLevel { get; set; }

        public IList<FftBlock> FftData { get; set; } = new AutoSortingList<FftBlock, int>(b => b.Index);

        public int DataFileId { get; set; }
        public BatDataFile DataFile { get; set; }

        public int CallId { get; set; }
        public BatCall Call { get; set; }
    }

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