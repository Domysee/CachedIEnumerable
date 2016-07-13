using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CachedIEnumerable
{
    public class CachedIEnumerable<T> : IEnumerable<T>, IDisposable, IList<T>
    {
        private List<T> enumeratedValues;
        private IEnumerable<T> enumerable;
        private IEnumerator<T> enumerator;
        private bool fullyEnumerated = false;

        public CachedIEnumerable(IEnumerable<T> enumerable)
        {
            this.enumeratedValues = new List<T>();
            this.enumerable = enumerable;
            this.enumerator = enumerable.GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new CachedIEnumerator<T>(enumeratedValues, enumerator, ref fullyEnumerated);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            (enumerable as IDisposable)?.Dispose();
        }

        #region IList
        public int Count
        {
            get
            {
                if (fullyEnumerated)
                    return enumeratedValues.Count;

                if (enumerable is ICollection<T>)
                    return ((ICollection<T>)enumerable).Count;

                var count = 0;
                var enumerator = this.GetEnumerator();
                while (enumerator.MoveNext()) count++;
                return count;
            }
        }
        public bool IsReadOnly => true;

        public T this[int index]
        {
            get
            {
                if (index < 0) throw new ArgumentOutOfRangeException("The given index is lower than 0");

                if (index < enumeratedValues.Count)
                    return enumeratedValues[index];

                var enumerator = this.GetEnumerator();
                for (var i = 0; i <= index; i++)
                    if (!enumerator.MoveNext()) throw new ArgumentOutOfRangeException("The given index exceeds the collection");
                return enumerator.Current;
            }
            set { throw new NotSupportedException(); }
        }

        public int IndexOf(T item)
        {
            var index = -1;
            var enumerator = this.GetEnumerator();
            while (enumerator.MoveNext())
            {
                index++;
                if (enumerator.Current.Equals(item))
                    return index;
            }
            return -1;
        }

        public bool Contains(T item)
        {
            var enumerator = this.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Equals(item))
                    return true;
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException("The given array is null");
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException("The given start index is smaller than 0");

            var enumerator = this.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (arrayIndex >= array.Length) throw new ArgumentException("The number of elements is greater than the available space in the target array. Keep in mind that the available space was filled with objects of this collection");
                array[arrayIndex] = enumerator.Current;
                arrayIndex++;
            }
        }

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }
        #endregion
    }
}
