using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CachedIEnumerable
{
    public class CachedIEnumerable<T> : IEnumerable<T>
    {
        private List<T> cache;
        private IEnumerable<T> source;
        private IEnumerator<T> enumerator;
        private List<WeakReference<CachedIEnumerator<T>>> enumerators;

        public IReadOnlyCollection<T> CachedValues => cache;

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
    }
}
