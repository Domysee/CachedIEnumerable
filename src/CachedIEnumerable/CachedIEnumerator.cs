using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CachedIEnumerable
{
    public class CachedIEnumerator<T> : IEnumerator<T>
    {
        private List<T> sharedCache;
        private IEnumerator<T> enumerator;
        private int currentIndex = -1;

        public CachedIEnumerator(List<T> sharedCache, IEnumerator<T> enumerator)
        {
            this.sharedCache = sharedCache;
            this.enumerator = enumerator;
        }

        public T Current { get; set; }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            currentIndex++;
            if (currentIndex < sharedCache.Count)
            {
                Current = sharedCache[currentIndex];
                return true;
            }

            var success = enumerator.MoveNext();
            if (success)
            {
                Current = enumerator.Current;
                sharedCache.Add(Current);
                return true;
            }
            else
            {
                Current = default(T);
                return false;
            }
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }
    }
}
