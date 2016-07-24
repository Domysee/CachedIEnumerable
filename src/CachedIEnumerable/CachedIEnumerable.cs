using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CachedIEnumerable
{
    public class CachedIEnumerable<T> : IEnumerable<T>, IList<T>
    {
        private List<T> cache;
        private IEnumerable<T> source;
        private IEnumerator<T> enumerator;
        private List<WeakReference<CachedIEnumerator<T>>> enumerators;

        public CachedIEnumerable(IEnumerable<T> source)
        {
            this.cache = new List<T>();
            this.source = source;
            this.enumerator = source.GetEnumerator();
            this.enumerators = new List<WeakReference<CachedIEnumerator<T>>>();
        }

        ~CachedIEnumerable()
        {
            enumerator.Dispose();
        }

        public IEnumerator<T> GetEnumerator()
        {
            var e = new CachedIEnumerator<T>(cache, enumerator, reset);
            enumerators.Add(new WeakReference<CachedIEnumerator<T>>(e));
            return e;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// resets the cached IEnumerable and invalidates all existing enumerators 
        /// called when the underlying collection changed
        /// </summary>
        private void reset()
        {
            cache.Clear();  //the cache is not valid at this point
            //the enumerator of the underlying collection must be renewed (it threw the exception)
            enumerator.Dispose();   
            enumerator = source.GetEnumerator();
            //invalidate all enumerators of the cached enumerable
            foreach(var weakEnumeratorReference in enumerators)
            {
                CachedIEnumerator<T> e;
                if (weakEnumeratorReference.TryGetTarget(out e)) { 
                    e.Invalidate();
                    e.Dispose();
                }
            }
            enumerators.Clear();    //since all enumerators are invalidated, there is no need to reference them any more
        }

        #region IList
        public int Count
        {
            get
            {
                if (source is ICollection<T>)
                    return ((ICollection<T>)source).Count;

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

                if (index < cache.Count)
                    return cache[index];

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
