﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CachedIEnumerable
{
    public class CachedIEnumerator<T> : IEnumerator<T>
    {
        private List<T> sharedEnumeratedValues;
        private IEnumerator<T> enumerator;
        private int currentIndex = -1;
        private bool fullyEnumerated;

        public CachedIEnumerator(List<T> sharedEnumeratedValues, IEnumerator<T> enumerator, ref bool fullyEnumerated)
        {
            this.sharedEnumeratedValues = sharedEnumeratedValues;
            this.enumerator = enumerator;
            this.fullyEnumerated = fullyEnumerated;
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
            if (currentIndex < sharedEnumeratedValues.Count)
            {
                Current = sharedEnumeratedValues[currentIndex];
                return true;
            }

            var success = enumerator.MoveNext();
            if (success)
            {
                Current = enumerator.Current;
                sharedEnumeratedValues.Add(Current);
                return true;
            }
            else
            {
                fullyEnumerated = true;
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
