using Faster.Map.Concurrent;
using System.Collections.Concurrent;

namespace Faster.Map.CMap.Tests
{
    public class EmplaceTests
    {
        [Theory]
        //[InlineData(1, 128)]
        //[InlineData(2, 10000)]
        //[InlineData(8, 4)]
        //[InlineData(5, 2000)]
        //[InlineData(4, 2000)]
        [InlineData(256, 2000)]
        //[InlineData(5, 5000)]
        //[InlineData(5, 2500)]
        //[InlineData(20, 5000)]
        //[InlineData(64, 5000)]
        //[InlineData(16, 8)]

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
        [InlineData(256, 2000)]
        public static void Assert_Emplace_With_Resize_Strings(int threads, int addsPerThread)
        {
            CMap<int, string> dict = new CMap<int, string>();

            int count = threads;
            Task[] tasks = new Task[count];

            for (int i = 0; i < threads; i++)
            {
                var ii = i;
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < addsPerThread; j++)
                    {
                        dict.Emplace((j + ii * addsPerThread), (j + ii * addsPerThread).ToString());
                    }
                });
            }

            Task.WaitAll(tasks);

            foreach (var pair in dict.Entries)
            {
                Assert.Equal(pair.Key.ToString(), pair.Value);
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
        }

        [Theory]
        [InlineData(256)]
        public void Assert_Emplace_With_RandomNumbers(int numberOfThreads)
        {
            var map = new CMap<uint, uint>(2000000, 0.8);
            var N = 1000000;
            var output = File.ReadAllText("Numbers.txt");
            var splittedOutput = output.Split(',');

            var keys = new uint[N];

            for (var index = 0; index < N; index++)
            {
                keys[index] = uint.Parse(splittedOutput[index]);
            }

            Parallel.For(0, N, new ParallelOptions { MaxDegreeOfParallelism = numberOfThreads }, i =>
            {
                var key = keys[i];
                map.Emplace(key, key);
            });

            foreach (var key in keys)
            {
                var result = map.Get(key, out var value);
                if (!result)
                {
                    Assert.Fail();
                };
            }

        }

    
       
    }
}
