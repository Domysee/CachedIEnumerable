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
        private bool valid = true;
        private bool pastEnd = false;

        private Action invalidateAll;

        public CachedIEnumerator(List<T> sharedCache, IEnumerator<T> enumerator, Action invalidateAll)
        {
            this.sharedCache = sharedCache;
            this.enumerator = enumerator;
            this.invalidateAll = invalidateAll;
        }

        private T current;
        public T Current
        {
            get
            {
                if (!pastEnd) return current;
                else throw new InvalidOperationException("The enumerator reached past the end of the collection");
            }
            private set
            {
                current = value;
            }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public void Dispose()
        {
        }

        public void Invalidate()
        {
            valid = false;
        }

        public bool MoveNext()
        {
            if (!valid)
                throw new InvalidOperationException("The collection was modified after the enumerator was created.");

            currentIndex++;
            if (currentIndex < sharedCache.Count)
            {
                Current = sharedCache[currentIndex];
                return true;
            }

            try
            {
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
                    pastEnd = true;
                    return false;
                }
            }
            catch (InvalidOperationException)
            {
                invalidateAll();
                throw;
            }
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }
    }
}
