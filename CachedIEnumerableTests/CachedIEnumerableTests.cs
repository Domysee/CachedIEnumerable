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
        public void InvalidatingSourceEnumerator_AfterFullEnumeration_DoesntInvalidateEnumerators()
        {
            var length = 10;
            var list = Enumerable.Range(0, length).ToList();
            var e = list.Cache();

            e.Last();
            var enumerator1 = e.GetEnumerator();
            var enumerator2 = e.GetEnumerator();
            list.Add(10);

            enumerator1.MoveNext();
            enumerator2.MoveNext();
        }

        [Fact]
        public void CurrentShouldThrowInvalidOperationException_IfEnumeratedPastLastValue()
        {
            var length = 10;
            var e = Enumerable.Range(0, length).Cache();
            var enumerator = e.GetEnumerator();

            while (enumerator.MoveNext()) { }

            Assert.Throws<InvalidOperationException>(() => { var x = enumerator.Current; });
        }
    }
}
