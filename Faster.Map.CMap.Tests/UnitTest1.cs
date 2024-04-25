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

        [Theory]
        [InlineData(1, 1, 1, 10000)]
        //[InlineData(5, 1, 1, 10000)]
        [InlineData(1, 1, 2, 5000)]
        [InlineData(1, 1, 5, 2000)]
        [InlineData(4, 0, 4, 2000)]
        [InlineData(16, 31, 4, 2000)]
        [InlineData(64, 5, 5, 5000)]
        [InlineData(5, 5, 5, 2500)]
        [InlineData(5, 5, 20, 5000)]
        [InlineData(5, 5, 64, 5000)]
        public static void Assert_Emplace_Without_Resize(int cLevel, int initSize, int threads, int addsPerThread)
        {
            CMap<int, int> dict = new CMap<int, int>(1000000);

            int count = threads;
            using (ManualResetEvent mre = new ManualResetEvent(false))
            {
                for (int i = 0; i < threads; i++)
                {
                    int ii = i;
                    Task.Run(() =>
                        {
                            for (int j = 0; j < addsPerThread; j++)
                            {
                                dict.Emplace(j + ii * addsPerThread, -(j + ii * addsPerThread));
                            }

                            if (Interlocked.Decrement(ref count) == 0)
                            {
                                mre.Set();
                            }
                        });
                }
                mre.WaitOne();
            }

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
        [InlineData(1, 16)]

        public static void Assert_Emplace_With_Resize(int threads, int addsPerThread)
        {

            CMap<int, int> dict = new CMap<int, int>();

            int count = threads;
            using (ManualResetEvent mre = new ManualResetEvent(false))
            {
                for (int i = 0; i < threads; i++)
                {
                    int ii = i;
                    Task.Run(() =>
                    {
                        for (int j = 0; j < addsPerThread; j++)
                        {
                            try
                            {
                                if (dict.Emplace(j + ii * addsPerThread, -(j + ii * addsPerThread)) == false) 
                                {
                                
                                }
                            }
                            catch (Exception ex)
                            {
                                Debugger.Launch();
                            }
                        }

                        if (Interlocked.Decrement(ref count) == 0)
                        {
                            mre.Set();
                        }
                    });
                }
                mre.WaitOne();
            }

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
                   $"The set of keys in the dictionary is are not the same as the expected" + Environment.NewLine +
                            "TestAdd1(threads={threads}, addsPerThread={addsPerThread})");
            }

            // Finally, let's verify that the count is reported correctly.
            int expectedCount = threads * addsPerThread;
            Assert.Equal(expectedCount, (int)dict.Count);
            // Assert.Equal(expectedCount, dictConcurrent.ToArray().Length);

        }


    }
}
