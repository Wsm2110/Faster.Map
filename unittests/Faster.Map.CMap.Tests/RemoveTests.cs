using Faster.Map.Concurrent;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Map.CMap.Tests
{
    public class RemoveTests
    {
        [Theory]
        [InlineData(1, 1, 10000)]
        [InlineData(5, 1, 1000)]
        [InlineData(1, 5, 2001)]
        [InlineData(4, 4, 2001)]
        [InlineData(15, 5, 2001)]
        [InlineData(64, 5, 5000)]
        [InlineData(64, 256, 2000)]
        [InlineData(64, 128, 5000)]
        public static void TestRemove1(int cLevel, int threads, int removesPerThread)
        {
            var map = new CMap<int, int>();
            string methodparameters = string.Format("* TestRemove1(cLevel={0}, threads={1}, removesPerThread={2})", cLevel, threads, removesPerThread);
            int N = 2 * threads * removesPerThread;

            for (int i = 0; i < N; i++)
            {
                map.Emplace(i, -i);
            }

            // The dictionary contains keys [0..N), each key mapped to a value equal to the key.
            // Threads will cooperatively remove all even keys

            int running = threads;
            using (ManualResetEvent mre = new ManualResetEvent(false))
            {
                for (int i = 0; i < threads; i++)
                {
                    int ii = i;
                    Task.Run(
                        () =>
                        {
                            for (int j = 0; j < removesPerThread; j++)
                            {
                                int value;
                                int key = 2 * (ii + j * threads);
                                Assert.True(map.Remove(key, out value), "Failed to remove an element! " + methodparameters);

                                Assert.Equal(-key, value);
                            }

                            if (Interlocked.Decrement(ref running) == 0) mre.Set();
                        });
                }
                mre.WaitOne();
            }

            foreach (var pair in map.Entries)
            {
                Assert.Equal(pair.Key, -pair.Value);
            }

            List<int> gotKeys = new List<int>();
            foreach (var pair in map.Keys)
            {
                gotKeys.Add(pair);
            }

            gotKeys.Sort();

            List<int> expectKeys = new List<int>();

            for (int i = 0; i < (threads * removesPerThread); i++)
            {
                expectKeys.Add(2 * i + 1);
            }

            Assert.Equal(expectKeys.Count, gotKeys.Count);

            for (int i = 0; i < expectKeys.Count; i++)
            {
                Assert.True(expectKeys[i].Equals(gotKeys[i]), "  > Unexpected key value! " + methodparameters);
            }

            // Finally, let's verify that the count is reported correctly.
            Assert.Equal(expectKeys.Count, map.Count);
            Assert.Equal(expectKeys.Count, map.Entries.ToArray().Length);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(5000)]
        public static void TestRemove2(int removesPerThread)
        {
            var map = new CMap<int, int>();

            for (int i = 0; i < removesPerThread; i++)
            {
                map.Emplace(i, -i);
            }

            // The dictionary contains keys [0..N), each key mapped to a value equal to the key.
            // Threads will cooperatively remove all even keys.
            const int SIZE = 2;
            int running = SIZE;

            bool[][] seen = new bool[SIZE][];
            for (int i = 0; i < SIZE; i++) seen[i] = new bool[removesPerThread];

            using (ManualResetEvent mre = new ManualResetEvent(false))
            {
                for (int t = 0; t < SIZE; t++)
                {
                    int thread = t;
                    Task.Run(
                        () =>
                        {
                            for (int key = 0; key < removesPerThread; key++)
                            {
                                int value;
                                if (map.Remove(key, out value))
                                {
                                    seen[thread][key] = true;

                                    Assert.Equal(-key, value);
                                }
                            }
                            if (Interlocked.Decrement(ref running) == 0)
                            {
                                mre.Set();
                            }
                        });
                }
                mre.WaitOne();
            }

            Assert.Equal(0, map.Count);

            for (int i = 0; i < removesPerThread; i++)
            {
                Assert.False(seen[0][i] == seen[1][i],
                    string.Format("> FAILED. Two threads appear to have removed the same element. TestRemove2(removesPerThread={0})", removesPerThread)
                    );
            }
        }

        //[Fact]
        //public static void TestRemove3()
        //{
        //    var dict = new CMap<int, int>();

        //    dict[99] = -99;

        //    ICollection<KeyValuePair<int, int>> col = dict;

        //    // Make sure we cannot "remove" a key/value pair which is not in the dictionary
        //    for (int i = 0; i < 200; i++)
        //    {
        //        if (i != 99)
        //        {
        //            Assert.False(col.Remove(new KeyValuePair<int, int>(i, -99)), "Should not remove not existing a key/value pair - new KeyValuePair<int, int>(i, -99)");
        //            Assert.False(col.Remove(new KeyValuePair<int, int>(99, -i)), "Should not remove not existing a key/value pair - new KeyValuePair<int, int>(99, -i)");
        //        }
        //    }

        //    // Can we remove a key/value pair successfully?
        //    Assert.True(col.Remove(new KeyValuePair<int, int>(99, -99)), "Failed to remove existing key/value pair");

        //    // Make sure the key/value pair is gone
        //    Assert.False(col.Remove(new KeyValuePair<int, int>(99, -99)), "Should not remove the key/value pair which has been removed");

        //    // And that the dictionary is empty. We will check the count in a few different ways:
        //    Assert.Equal(0, dict.Count);
        //    Assert.Equal(0, dict.ToArray().Length);
        //}

        //[Fact]
        //public static void TryRemove_KeyValuePair_ArgumentValidation()
        //{
        //    AssertExtensions.Throws<ArgumentNullException>("item", () => new ConcurrentDictionary<string, int>().TryRemove(new KeyValuePair<string, int>(null, 42)));
        //    new ConcurrentDictionary<int, int>().TryRemove(new KeyValuePair<int, int>(0, 0)); // no error when using default value type
        //    new ConcurrentDictionary<int?, int>().TryRemove(new KeyValuePair<int?, int>(0, 0)); // or nullable
        //}

        //[Fact]
        //public static void TryRemove_KeyValuePair_RemovesSuccessfullyAsAppropriate()
        //{
        //    var dict = new CMap<string, int>();

        //    for (int i = 0; i < 2; i++)
        //    {
        //        Assert.False(dict.TRemove(KeyValuePair.Create("key", 42)));
        //        Assert.Equal(0, dict.Count);
        //        Assert.True(dict.Emplace("key", 42));
        //        Assert.Equal(1, dict.Count);
        //        Assert.True(dict.Remove(KeyValuePair.Create("key", 42)));
        //        Assert.Equal(0, dict.Count);
        //    }

        //    Assert.True(dict.Emplace("key", 42));
        //    Assert.False(dict.Remove("key", 43))); // value doesn't match
        //}

        //[Fact]
        //public static void TryRemove_KeyValuePair_MatchesKeyWithDefaultComparer()
        //{
        //    var dict = new CMap<string, string>(5, 0.5, StringComparer.OrdinalIgnoreCase);
        //    dict.Emplace("key", "value");

        //    Assert.False(dict.TryRemove(KeyValuePair.Create("key", "VALUE")));
        //    Assert.True(dict.TryRemove(KeyValuePair.Create("KEY", "value")));
        //}


    }
}
