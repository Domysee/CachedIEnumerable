using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using CachedIEnumerable;

namespace CachedIEnumerableTests
{
    public class CachedIEnumerableTests
    {
        private int generatorLength = 0;
        private int evaluateCount = 0;
        private IEnumerable<int> generator()
        {
            for (int i = 0; i < generatorLength; i++)
            {
                evaluateCount++;
                yield return i;
            }
        }

        [Fact]
        public void GetEnumeratorShouldReturnEnumerator()
        {
            var e = Enumerable.Range(0, 1).Cache().GetEnumerator();

            Assert.NotNull(e);
            Assert.IsType(typeof(CachedIEnumerator<int>), e);
        }

        [Fact]
        public void GetEnumeratorShouldReturnDifferentEnumerators()
        {
            var e1 = Enumerable.Range(0, 1).Cache().GetEnumerator();
            var e2 = Enumerable.Range(0, 1).Cache().GetEnumerator();

            Assert.NotEqual(e1, e2);
        }

        [Fact]
        public void ShouldEvaluateOnce_IfEnumerableIsGeneratorFunction()
        {
            generatorLength = 2;
            evaluateCount = 0;
            var e = generator().Cache();

            var enumerator1 = e.GetEnumerator();
            var enumerator2 = e.GetEnumerator();

            for (var i = 0; i < generatorLength; i++)
            {
                enumerator1.MoveNext();
                enumerator2.MoveNext();
            }

            Assert.Equal(generatorLength, evaluateCount);
        }

        [Fact]
        public void ShouldNotThrowException_IfEnumerableIsEmpty()
        {
            var evaluateCount = 0;
            var e = Enumerable.Empty<int>().Select(i =>
            {
                evaluateCount++;
                return i * i;
            }).Cache();

            e.Select(i => i * i).ToList();

            Assert.Equal(0, evaluateCount);
        }

        //this test does not work any more because of the IList implementation
        //Linq's First implementation checks if IList is implemented, then checks if Count > 0 to return the first value
        //[Fact]
        //public void ShouldEvaluateOnlyFirstEntry_IfOnlyFirstEntryIsNeeded()
        //{
        //    var length = 10;
        //    var evaluateCount = 0;
        //    var e = Enumerable.Range(0, length).Select(i =>
        //    {
        //        evaluateCount++;
        //        return i * i;
        //    }).Cache();

        //    e.First();

        //    Assert.Equal(1, evaluateCount);
        //}

        [Fact]
        public void ShouldCallDisposeOfUnderlyingEnumerable()
        {

        }

        [Fact]
        public void ShouldEvaluateOnce_IfCompletelyEnumeratingOneByOne()
        {
            var length = 1;
            var evaluateCount = 0;
            var e = Enumerable.Range(0, length).Select(i =>
            {
                evaluateCount++;
                return i * i;
            }).Cache();

            e.ToList();
            e.ToList();

            Assert.Equal(length, evaluateCount);
        }

        [Fact]
        public void ShouldEvaluateOnce_IfEnumeratingAtTheSameTime()
        {
            var length = 2;
            var evaluateCount = 0;
            var e = new[] { 0, 1 }.Select(i =>
            {
                evaluateCount++;
                return i * i;
            }).Cache();

            var enumerator1 = e.GetEnumerator();
            var enumerator2 = e.GetEnumerator();

            for (var i = 0; i < length; i++)
            {
                enumerator1.MoveNext();
                enumerator2.MoveNext();
            }

            Assert.Equal(2, evaluateCount);
        }

        [Fact]
        public void ShouldEvaluateOnce_IfEnumeratingNestedForEach()
        {
            var length = 1;
            var evaluateCount = 0;
            var e = Enumerable.Range(0, length).Select(i =>
            {
                evaluateCount++;
                return i * i;
            }).Cache();

            foreach (var i in e)
            {
                foreach (var j in e)
                {
                    var k = i * j;
                }
            }

            Assert.Equal(length, evaluateCount);
        }

        [Fact]
        public void ShouldNotEvaluate_IfUsingWithSubsequentLinqSelect()
        {
            var length = 1;
            var evaluateCount = 0;
            var e = Enumerable.Range(0, length).Select(i =>
            {
                evaluateCount++;
                return i * i;
            }).Cache();

            e.Select(i => i * i);

            Assert.Equal(0, evaluateCount);
        }

        [Fact]
        public void ShouldEvaluateOnce_IfUsingWithSubsequentLinqSelectToList()
        {
            var length = 1;
            var evaluateCount = 0;
            var e = Enumerable.Range(0, length).Select(i =>
            {
                evaluateCount++;
                return i * i;
            }).Cache();

            e.Select(i => i * i).ToList();

            Assert.Equal(length, evaluateCount);
        }

        [Fact]
        public void InvalidatingSourceEnumeratorInvalidatesEnumerator()
        {
            var length = 10;
            var list = Enumerable.Range(0, length).ToList();
            var e = list.Cache();

            list.Add(10);

            Assert.Throws<InvalidOperationException>(() => { e.ElementAt(5); });
        }

        [Fact]
        public void InvalidatingSourceEnumeratorInvalidatesEnumerator_AfterFullEnumeration()
        {
            var length = 10;
            var list = Enumerable.Range(0, length).ToList();
            var e = list.Cache();

            e.Last();
            list.Add(10);

            Assert.Throws<InvalidOperationException>(() => { e.ElementAt(5); });
        }

        [Fact]
        public void InvalidatingSourceEnumeratorInvalidatesAllEnumerators()
        {
            var length = 10;
            var list = Enumerable.Range(0, length).ToList();
            var e = list.Cache();

            var enumerator1 = e.GetEnumerator();
            var enumerator2 = e.GetEnumerator();
            list.Add(10);

            Assert.Throws<InvalidOperationException>(() => { enumerator1.MoveNext(); });
            Assert.Throws<InvalidOperationException>(() => { enumerator2.MoveNext(); });
        }

        [Fact]
        public void InvalidatingSourceEnumeratorInvalidatesAllEnumerators_AfterFullEnumeration()
        {
            var length = 10;
            var list = Enumerable.Range(0, length).ToList();
            var e = list.Cache();

            e.Last();
            var enumerator1 = e.GetEnumerator();
            var enumerator2 = e.GetEnumerator();
            list.Add(10);

            Assert.Throws<InvalidOperationException>(() => { enumerator1.MoveNext(); });
            Assert.Throws<InvalidOperationException>(() => { enumerator2.MoveNext(); });
        }

        #region IList Tests
        [Fact]
        public void SettingIndexerThrowsNotSupportedException()
        {
            var e = Enumerable.Range(0, 1).Cache();
            Assert.Throws<NotSupportedException>(() => e[0] = 1);
        }

        [Fact]
        public void AddThrowsNotSupportedException()
        {
            var e = Enumerable.Range(0, 1).Cache();
            Assert.Throws<NotSupportedException>(() => e.Add(1));
        }

        [Fact]
        public void InsertThrowsNotSupportedException()
        {
            var e = Enumerable.Range(0, 1).Cache();
            Assert.Throws<NotSupportedException>(() => e.Insert(0, 1));
        }

        [Fact]
        public void RemoveThrowsNotSupportedException()
        {
            var e = Enumerable.Range(0, 1).Cache();
            Assert.Throws<NotSupportedException>(() => e.Remove(0));
        }

        [Fact]
        public void RemoveAtThrowsNotSupportedException()
        {
            var e = Enumerable.Range(0, 1).Cache();
            Assert.Throws<NotSupportedException>(() => e.RemoveAt(0));
        }

        [Fact]
        public void ClearThrowsNotSupportedException()
        {
            var e = Enumerable.Range(0, 1).Cache();
            Assert.Throws<NotSupportedException>(() => e.Clear());
        }

        [Fact]
        public void CopyToThrowsArgumentNullException_IfArrayIsNull()
        {
            var e = Enumerable.Range(0, 1).Cache();
            Assert.Throws<ArgumentNullException>(() => e.CopyTo(null, 1));
        }

        [Fact]
        public void CopyToThrowsArgumentOutOfRangeException_IfIndexIsSmallerThanZero()
        {
            var e = Enumerable.Range(0, 1).Cache();
            Assert.Throws<ArgumentOutOfRangeException>(() => e.CopyTo(new int[1], -1));
        }

        [Fact]
        public void CopyToThrowsArgumentException_IfEnumerableExceedsArraySize()
        {
            var e = Enumerable.Range(0, 1).Cache();
            Assert.Throws<ArgumentException>(() => e.CopyTo(new int[0], 0));
        }

        [Fact]
        public void CopyToCopiesToAvailableSpace_IfEnumerableExceedsArraySize()
        {
            var enumerableSize = 6;
            var arraySize = 3;
            var array = new int[arraySize];
            var e = Enumerable.Range(1, enumerableSize).Cache();

            try
            {
                e.CopyTo(array, 0);
            }
            catch (ArgumentException) { }

            foreach (var x in array)
                Assert.NotEqual(x, default(int));
        }

        [Fact]
        public void CopyToCopiesFirstValuesInOrderToAvailableSpace_IfEnumerableExceedsArraySize()
        {
            var enumerableSize = 6;
            var arraySize = 3;
            var array = new int[arraySize];
            var e = Enumerable.Range(1, enumerableSize).Cache();

            try
            {
                e.CopyTo(array, 0);
            }
            catch (ArgumentException) { }

            for (var i = 0; i < arraySize; i++)
                Assert.Equal(array[i], e.ElementAt(i));
        }

        [Fact]
        public void CopyToCopiesValuesInOrderToArray()
        {
            var enumerableSize = 6;
            var arraySize = 6;
            var array = new int[arraySize];
            var e = Enumerable.Range(1, enumerableSize).Cache();

            e.CopyTo(array, 0);

            for (var i = 0; i < arraySize; i++)
                Assert.Equal(array[i], e.ElementAt(i));
        }

        [Fact]
        public void CopyToCopiesValuesToArray_StartingWithTheGivenIndex()
        {
            var enumerableSize = 6;
            var arraySize = 8;
            var startIndex = 2;
            var array = new int[arraySize];
            var e = Enumerable.Range(1, enumerableSize).Cache();

            e.CopyTo(array, startIndex);

            for (var i = startIndex; i < arraySize; i++)
                Assert.Equal(array[i], e.ElementAt(i - startIndex));
            for (var i = 0; i < startIndex; i++)
                Assert.Equal(array[i], default(int));
        }

        [Fact]
        public void CopyToEnumeratesValuesOnlyOnce()
        {
            var array = new int[1000];
            var length = 1;
            var evaluateCount = 0;
            var e = Enumerable.Range(0, length).Select(i =>
            {
                evaluateCount++;
                return i * i;
            }).Cache();

            e.CopyTo(array, 0);
            e.CopyTo(array, length);

            Assert.Equal(length, evaluateCount);
        }

        [Fact]
        public void ContainsReturnsTrue_IfValueExists()
        {
            var length = 10;
            var e = Enumerable.Range(0, length).Cache();

            var result = e.Contains(1);

            Assert.True(result);
        }

        [Fact]
        public void ContainsReturnsFalse_IfValueDoesntExist()
        {
            var length = 10;
            var e = Enumerable.Range(0, length).Cache();

            var result = e.Contains(-1);

            Assert.False(result);
        }

        [Fact]
        public void ContainsEnumeratesValuesOnlyOnce()
        {
            var length = 1;
            var evaluateCount = 0;
            var e = Enumerable.Range(0, length).Select(i =>
            {
                evaluateCount++;
                return i;
            }).Cache();

            e.Contains(0);
            e.Contains(0);

            Assert.Equal(length, evaluateCount);
        }

        [Fact]
        public void ContainsEnumeratesUntilFirstValueIsFound()
        {
            var evaluateCount = 0;
            var e = new[] { 1, 2, 2, 3 }.Select(i =>
            {
                evaluateCount++;
                return i;
            }).Cache();

            e.Contains(1);

            Assert.Equal(1, evaluateCount);
        }

        [Fact]
        public void ContainsEnumeratesUntilFirstValueIsFound_AndDoesNotEnumerateMultipleTimesOnSubsequentCalls()
        {
            var evaluateCount = 0;
            var e = new[] { 1, 2, 2, 3 }.Select(i =>
            {
                evaluateCount++;
                return i;
            }).Cache();

            e.Contains(1);
            Assert.Equal(1, evaluateCount);
            e.Contains(2);
            Assert.Equal(2, evaluateCount);
            e.Contains(3);
            Assert.Equal(4, evaluateCount);
        }

        [Fact]
        public void ContainsEnumeratesAllValues_IfValueDoesntExist()
        {
            var evaluateCount = 0;
            var e = new[] { 1, 2, 2, 3 }.Select(i =>
            {
                evaluateCount++;
                return i;
            }).Cache();

            e.Contains(-1);
            Assert.Equal(4, evaluateCount);
        }

        [Fact]
        public void IndexOfReturnsCorrectIndex_IfValueExists()
        {
            var length = 10;
            var e = Enumerable.Range(0, length).Cache();

            var result = e.IndexOf(5);

            Assert.Equal(5, result);
        }

        [Fact]
        public void IndexOfReturnsMinus1_IfValueDoesntExist()
        {
            var length = 10;
            var e = Enumerable.Range(0, length).Cache();

            var result = e.IndexOf(-1345);

            Assert.Equal(-1, result);
        }

        [Fact]
        public void IndexOfEnumeratesValuesOnlyOnce()
        {
            var length = 1;
            var evaluateCount = 0;
            var e = Enumerable.Range(0, length).Select(i =>
            {
                evaluateCount++;
                return i;
            }).Cache();

            e.IndexOf(0);
            e.IndexOf(0);

            Assert.Equal(length, evaluateCount);
        }

        [Fact]
        public void IndexOfEnumeratesUntilFirstValueIsFound()
        {
            var evaluateCount = 0;
            var e = new[] { 1, 2, 2, 3 }.Select(i =>
            {
                evaluateCount++;
                return i;
            }).Cache();

            e.IndexOf(1);

            Assert.Equal(1, evaluateCount);
        }

        [Fact]
        public void IndexOfEnumeratesUntilFirstValueIsFound_AndDoesNotEnumerateMultipleTimesOnSubsequentCalls()
        {
            var evaluateCount = 0;
            var e = new[] { 1, 2, 2, 3 }.Select(i =>
            {
                evaluateCount++;
                return i;
            }).Cache();

            e.IndexOf(1);
            Assert.Equal(1, evaluateCount);
            e.IndexOf(2);
            Assert.Equal(2, evaluateCount);
            e.IndexOf(3);
            Assert.Equal(4, evaluateCount);
        }

        [Fact]
        public void IndexOfEnumeratesAllValues_IfValueDoesntExist()
        {
            var evaluateCount = 0;
            var e = new[] { 1, 2, 2, 3 }.Select(i =>
            {
                evaluateCount++;
                return i;
            }).Cache();

            e.IndexOf(-1);
            Assert.Equal(4, evaluateCount);
        }

        [Fact]
        public void IndexerThrowsArgumentOutOfRangeException_IfIndexIsLowerThanZero()
        {
            var length = 10;
            var e = Enumerable.Range(0, length).Cache();

            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = e[-1]; });
        }

        [Fact]
        public void IndexerThrowsArgumentOutOfRangeException_IfIndexExceedsSize()
        {
            var length = 10;
            var e = Enumerable.Range(0, length).Cache();

            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = e[100]; });
        }

        [Fact]
        public void IndexerReturnsCorrectValue()
        {
            var length = 10;
            var e = Enumerable.Range(0, length).Cache();

            var result = e[5];

            Assert.Equal(5, result);
        }

        [Fact]
        public void IndexerEnumeratesValuesOnlyOnce()
        {
            var length = 1;
            var evaluateCount = 0;
            var e = Enumerable.Range(0, length).Select(i =>
            {
                evaluateCount++;
                return i;
            }).Cache();

            var x1 = e[0];
            var x2 = e[0];

            Assert.Equal(length, evaluateCount);
        }

        [Fact]
        public void IndexerEnumeratesUntilGivenIndex()
        {
            var evaluateCount = 0;
            var e = new[] { 1, 2, 3, 4 }.Select(i =>
            {
                evaluateCount++;
                return i;
            }).Cache();

            var x1 = e[0];

            Assert.Equal(1, evaluateCount);
        }

        [Fact]
        public void IndexerEnumeratesUntilFirstValueIsFound_AndDoesNotEnumerateMultipleTimesOnSubsequentCalls()
        {
            var evaluateCount = 0;
            var e = new[] { 1, 2, 3, 4 }.Select(i =>
            {
                evaluateCount++;
                return i;
            }).Cache();

            var x1 = e[0];
            Assert.Equal(1, evaluateCount);
            var x2 = e[1];
            Assert.Equal(2, evaluateCount);
            var x3 = e[3];
            Assert.Equal(4, evaluateCount);
        }

        [Fact]
        public void CountReturnsCorrectValue()
        {
            var length = 10;
            var e = Enumerable.Range(0, length).Cache();

            var count = e.Count;

            Assert.Equal(length, count);
        }

        [Fact]
        public void CountEvaluatesOnlyOnce()
        {
            var length = 10;
            var evaluateCount = 0;
            var e = Enumerable.Range(0, length).Select(i =>
            {
                evaluateCount++;
                return i;
            }).Cache();

            var count1 = e.Count;
            var count2 = e.Count;

            Assert.Equal(length, evaluateCount);
        }
        #endregion
    }
}
