using Faster.Map.ConMap;
using Faster.Map.QuadMap;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Faster.Map.CMap.Tests
{
    public class UnitTest1
    {

        [Fact]
        public void Bench()
        {
            var map = new CMap<int, int>(2000000, 0.5);
            Parallel.For(0, 10000000, new ParallelOptions { MaxDegreeOfParallelism = 256 }, i =>
            {
                map.Emplace(i, i);
            });


        }



        [Theory]
        //[InlineData(1, 1, 1, 10000)]
        ////[InlineData(5, 1, 1, 10000)]
        //[InlineData(1, 1, 2, 5000)]
        //[InlineData(1, 1, 5, 2000)]
        //[InlineData(4, 0, 4, 2000)]
        //[InlineData(16, 31, 4, 2000)]
        //[InlineData(64, 5, 5, 5000)]
        //[InlineData(5, 5, 5, 2500)]
        //[InlineData(5, 5, 20, 5000)]
        //[InlineData(5, 5, 64, 5000)]
        [InlineData(5, 5, 256, 5000)]
        public static void Assert_Emplace_Without_Resize(int cLevel, int initSize, int threads, int addsPerThread)
        {
            CMap<int, int> dict = new CMap<int, int>(1000000);

            int count = threads;
            Task[] tasks = new Task[count];
            for (int i = 0; i < threads; i++)
            {
                var ii = i;
                tasks[i] = Task.Run(() =>
                {

                    for (int j = 0; j < addsPerThread; j++)
                    {
                        dict.Emplace(j + ii * addsPerThread, -(j + ii * addsPerThread));
                    }

                });
            }

            Task.WaitAll(tasks);

            foreach (var pair in dict.Entries)
            {
                Assert.Equal(pair.Key, -pair.Value);
            }

            List<int> gotKeys = new List<int>();
            foreach (var pair in dict.Entries)
            {
                gotKeys.Add(pair.Key);
            }

            gotKeys.Sort();

            List<int> expectKeys = new List<int>();
            int itemCount = threads * addsPerThread;
            for (int i = 0; i < itemCount; i++)
            {
                expectKeys.Add(i);
            }


            Assert.Equal(expectKeys.Count, gotKeys.Count);

            for (int i = 0; i < expectKeys.Count; i++)
            {
                Assert.True(expectKeys[i].Equals(gotKeys[i]),
                    string.Format("The set of keys in the dictionary is are not the same as the expected" + Environment.NewLine +
                            "TestAdd1(cLevel={0}, initSize={1}, threads={2}, addsPerThread={3})", cLevel, initSize, threads, addsPerThread)
                   );
            }

            // Finally, let's verify that the count is reported correctly.
            int expectedCount = threads * addsPerThread;
            Assert.Equal(expectedCount, (int)dict.Count);
        }

        [Theory]
        //[InlineData(1, 128)]
        //[InlineData(2, 10000)]
        //[InlineData(4, 16)]
        //[InlineData(5, 2000)]
        //[InlineData(4, 2000)]
        //[InlineData(256, 2000)]
        //[InlineData(5, 5000)]
        //[InlineData(5, 2500)]
        //[InlineData(20, 5000)]
        //[InlineData(64, 5000)]
        //[InlineData(16, 8)]
        [InlineData(256, 2000)]

        public static void Assert_Emplace_With_Resize(int threads, int addsPerThread)
        {
            CMap<int, int> dict = new CMap<int, int>();

            int count = threads;
            Task[] tasks = new Task[count];

            for (int i = 0; i < threads; i++)
            {
                var ii = i;
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < addsPerThread; j++)
                    {
                        dict.Emplace((j + ii * addsPerThread), (-(j + ii * addsPerThread)));
                    }
                });
            }

            Task.WaitAll(tasks);

            foreach (var pair in dict.Entries)
            {
                Assert.Equal(pair.Key, -pair.Value);
            }

            List<int> gotKeys = new List<int>();
            foreach (var pair in dict.Entries)
            {
                gotKeys.Add(pair.Key);
            }

            gotKeys.Sort();


            List<int> expectKeys = new List<int>();
            int itemCount = threads * addsPerThread;
            for (int i = 0; i < itemCount; i++)
            {
                expectKeys.Add(i);
            }

            Assert.True(expectKeys.Count == gotKeys.Count, $"Keys {expectKeys.Count} - {gotKeys.Count}");

            for (int i = 0; i < expectKeys.Count; i++)
            {

                Assert.True(expectKeys[i].Equals(gotKeys[i]),
                   $"The set of keys in the dictionary is are not the same {expectKeys[i]} - {gotKeys[i]}" + Environment.NewLine +
                            "TestAdd1(threads={threads}, addsPerThread={addsPerThread})");
            }

            ////// Finally, let's verify that the count is reported correctly.
            int expectedCount = threads * addsPerThread;
            Assert.Equal(expectedCount, (int)dict.Count);
            // Assert.Equal(expectedCount, dictConcurrent.ToArray().Length);

        }

        [Theory]
        [InlineData(1, 10000)]
        [InlineData(2, 5000)]
        [InlineData(5, 2001)]
        [InlineData(8, 2001)]
        [InlineData(16, 2001)]
        [InlineData(32, 5000)]
        [InlineData(64, 25000)]
        [InlineData(128, 2000)]
        [InlineData(256, 2000)]
        [InlineData(512, 2000)]

        public static void TestRead1(int threads, int readsPerThread)
        {
            var dict = new CMap<int, int>();

            for (int i = 0; i < readsPerThread; i += 2)
            {
                dict.Emplace(i, i);
            }

            int count = threads;
            using (ManualResetEvent mre = new ManualResetEvent(false))
            {
                for (int i = 0; i < threads; i++)
                {
                    int ii = i;
                    Task.Run(
                        () =>
                        {
                            for (int j = 0; j < readsPerThread; j++)
                            {
                                int val = 0;
                                if (dict.Get(j, out val))
                                {
                                    Assert.Equal(0, j % 2);
                                    Assert.Equal(j, val);
                                }
                                else
                                {
                                    Assert.Equal(1, j % 2);
                                }
                            }
                            if (Interlocked.Decrement(ref count) == 0) mre.Set();
                        });
                }
                mre.WaitOne();
            }
        }
    }
}
