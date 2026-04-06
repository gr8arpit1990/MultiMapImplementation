using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace MultimapImplementation
{
    public static class MultiMapTestHelpers
    {
        public static Dictionary<TKey, List<TValue>> ToDictionary<TKey, TValue>(
            this MultiMap<TKey, TValue> map)
            where TKey : notnull
        {
            Dictionary<TKey, List<TValue>> result = new Dictionary<TKey, List<TValue>>();

            foreach (TKey key in map.Keys)
            {
                result[key] = map.GetValues(key).ToList();
            }

            return result;
        }

        public static void AssertMapEquals<TKey, TValue>(
            MultiMap<TKey, TValue> actual,
            Dictionary<TKey, List<TValue>> expected)
            where TKey : notnull
        {
            Dictionary<TKey, List<TValue>> actualDictionary = actual.ToDictionary();

            Assert.Equal(expected.Count, actualDictionary.Count);

            foreach (KeyValuePair<TKey, List<TValue>> expectedEntry in expected)
            {
                Assert.True(actualDictionary.ContainsKey(expectedEntry.Key));

                List<TValue> actualValues = actualDictionary[expectedEntry.Key];
                Assert.Equal(expectedEntry.Value, actualValues);
            }
        }
    }

    public class Tests
    {
        [Fact]
        public void UnionWith_ShouldAddUniquePairsFromSecond()
        {
            MultiMap<string, int> first = new MultiMap<string, int>();
            first.Add("fruit", 1);
            first.Add("color", 10);

            MultiMap<string, int> second = new MultiMap<string, int>();
            second.Add("fruit", 2);
            second.Add("shape", 20);

            var x = first.UnionWith1(second);

            MultiMapTestHelpers.AssertMapEquals(x, new Dictionary<string, List<int>>
            {
                ["fruit"] = new List<int> { 1, 2 },
                ["color"] = new List<int> { 10 },
                ["shape"] = new List<int> { 20 }
            });
        }

        [Fact]
        public void UnionWith_ShouldIgnoreDuplicatePairs()
        {
            MultiMap<string, int> first = new MultiMap<string, int>();
            first.Add("fruit", 1);
            first.Add("fruit", 1);

            MultiMap<string, int> second = new MultiMap<string, int>();
            second.Add("fruit", 1);
            second.Add("fruit", 2);
            second.Add("fruit", 2);

            var x = first.UnionWith1(second);

            MultiMapTestHelpers.AssertMapEquals(x, new Dictionary<string, List<int>>
            {
                ["fruit"] = new List<int> { 1, 2 }
            });
        }

        [Fact]
        public void UnionWith_ShouldNotChangeFirst_WhenSecondIsEmpty()
        {
            MultiMap<string, int> first = new MultiMap<string, int>();
            first.Add("fruit", 1);
            first.Add("color", 10);

            MultiMap<string, int> second = new MultiMap<string, int>();

            var x = first.UnionWith1(second);

            MultiMapTestHelpers.AssertMapEquals(x, new Dictionary<string, List<int>>
            {
                ["fruit"] = new List<int> { 1 },
                ["color"] = new List<int> { 10 }
            });
        }

        [Fact]
        public void IntersectWith_ShouldKeepOnlyPairsPresentInBoth()
        {
            MultiMap<string, int> first = new MultiMap<string, int>();
            first.Add("fruit", 1);
            first.Add("fruit", 2);
            first.Add("color", 10);

            MultiMap<string, int> second = new MultiMap<string, int>();
            second.Add("fruit", 2);
            second.Add("fruit", 3);
            second.Add("shape", 10);

            var x = first.IntersectWith1(second);

            MultiMapTestHelpers.AssertMapEquals(x, new Dictionary<string, List<int>>
            {
                ["fruit"] = new List<int> { 2 }
            });
        }

        [Fact]
        public void IntersectWith_ShouldCollapseDuplicatePairsInResult()
        {
            MultiMap<string, int> first = new MultiMap<string, int>();
            first.Add("fruit", 2);
            first.Add("fruit", 2);
            first.Add("fruit", 2);

            MultiMap<string, int> second = new MultiMap<string, int>();
            second.Add("fruit", 2);
            second.Add("fruit", 2);

            var x = first.IntersectWith1(second);

            MultiMapTestHelpers.AssertMapEquals(x, new Dictionary<string, List<int>>
            {
                ["fruit"] = new List<int> { 2 }
            });
        }

        [Fact]
        public void IntersectWith_ShouldBecomeEmpty_WhenThereAreNoCommonPairs()
        {
            MultiMap<string, int> first = new MultiMap<string, int>();
            first.Add("fruit", 1);
            first.Add("color", 10);

            MultiMap<string, int> second = new MultiMap<string, int>();
            second.Add("fruit", 2);
            second.Add("shape", 20);

            var x = first.IntersectWith1(second);

            Assert.Empty(x.Keys);
        }

        [Fact]
        public void ExceptWith_ShouldKeepOnlyPairsNotPresentInSecond()
        {
            MultiMap<string, int> first = new MultiMap<string, int>();
            first.Add("fruit", 1);
            first.Add("fruit", 2);
            first.Add("color", 10);

            MultiMap<string, int> second = new MultiMap<string, int>();
            second.Add("fruit", 2);
            second.Add("shape", 10);

            var x = first.ExceptWith1(second);

            MultiMapTestHelpers.AssertMapEquals(x, new Dictionary<string, List<int>>
            {
                ["fruit"] = new List<int> { 1 },
                ["color"] = new List<int> { 10 }
            });
        }

        [Fact]
        public void ExceptWith_ShouldCollapseDuplicatePairsInResult()
        {
            MultiMap<string, int> first = new MultiMap<string, int>();
            first.Add("fruit", 1);
            first.Add("fruit", 1);
            first.Add("color", 10);

            MultiMap<string, int> second = new MultiMap<string, int>();
            second.Add("fruit", 2);
            second.Add("fruit", 3);
            second.Add("shape", 10);

            var x = first.ExceptWith1(second);

            MultiMapTestHelpers.AssertMapEquals(x, new Dictionary<string, List<int>>
            {
                ["fruit"] = new List<int> { 1 },
                ["color"] = new List<int> { 10 }
            });
        }

        [Fact]
        public void ExceptWith_ShouldBecomeEmpty_WhenAllPairsExistInSecond()
        {
            MultiMap<string, int> first = new MultiMap<string, int>();
            first.Add("fruit", 1);
            first.Add("fruit", 2);

            MultiMap<string, int> second = new MultiMap<string, int>();
            second.Add("fruit", 1);
            second.Add("fruit", 2);
            second.Add("fruit", 3);

            var x = first.ExceptWith1(second);

            Assert.Empty(x.Keys);
        }

        [Fact]
        public void SymmetricExceptWith_ShouldKeepPairsPresentInExactlyOneMap()
        {
            MultiMap<string, int> first = new MultiMap<string, int>();
            first.Add("fruit", 1);
            first.Add("fruit", 2);
            first.Add("color", 10);

            MultiMap<string, int> second = new MultiMap<string, int>();
            second.Add("fruit", 2);
            second.Add("fruit", 3);
            second.Add("shape", 20);

            var x = first.SymmetricExceptWith1(second);

            MultiMapTestHelpers.AssertMapEquals(x, new Dictionary<string, List<int>>
            {
                ["fruit"] = new List<int> { 1, 3 },
                ["color"] = new List<int> { 10 },
                ["shape"] = new List<int> { 20 }
            });
        }

        [Fact]
        public void SymmetricExceptWith_ShouldCancelDuplicatePairsCompletely()
        {
            MultiMap<string, int> first = new MultiMap<string, int>();
            first.Add("fruit", 1);
            first.Add("fruit", 1);
            first.Add("fruit", 2);

            MultiMap<string, int> second = new MultiMap<string, int>();
            second.Add("fruit", 1);
            second.Add("fruit", 3);

            var x = first.SymmetricExceptWith1(second);

            MultiMapTestHelpers.AssertMapEquals(x, new Dictionary<string, List<int>>
            {
                ["fruit"] = new List<int> { 2, 3 }
            });
        }

        [Fact]
        public void SymmetricExceptWith_ShouldBecomeEmpty_WhenMapsContainSameUniquePairs()
        {
            MultiMap<string, int> first = new MultiMap<string, int>();
            first.Add("fruit", 1);
            first.Add("fruit", 1);
            first.Add("color", 10);

            MultiMap<string, int> second = new MultiMap<string, int>();
            second.Add("fruit", 1);
            second.Add("color", 10);
            second.Add("color", 10);

            var x = first.SymmetricExceptWith1(second);

            Assert.Empty(x.Keys);
        }

        [Fact]
        public void SymmetricExceptWith_ShouldNotChangeFirst_WhenSecondIsEmpty_ExceptForDuplicateCollapse()
        {
            MultiMap<string, int> first = new MultiMap<string, int>();
            first.Add("fruit", 1);
            first.Add("fruit", 1);
            first.Add("color", 10);

            MultiMap<string, int> second = new MultiMap<string, int>();

            var x = first.SymmetricExceptWith1(second);

            MultiMapTestHelpers.AssertMapEquals(x, new Dictionary<string, List<int>>
            {
                ["fruit"] = new List<int> { 1 },
                ["color"] = new List<int> { 10 }
            });
        }
    }
}
