using Faster.Map.Concurrent;
using System.Collections.Concurrent;

namespace Faster.Map.CMap.Tests
{
    public class UpdateTests
    {
        [Theory]
        [InlineData(1, 10000)]
        [InlineData(2, 5000)]
        [InlineData(5, 2001)]
        [InlineData(4, 2001)]
        [InlineData(5, 5000)]
        [InlineData(5, 25000)]
        [InlineData(16, 25000)]
        [InlineData(32, 2000)]
        [InlineData(64, 25000)]
        [InlineData(128, 2000)]
        [InlineData(256, 2000)]

        public static void TestUpdatePart1(int threads, int updatesPerThread)
        {
            CMap<int, int> dict = new CMap<int, int>();

            for (int i = 1; i <= updatesPerThread; i++)
            {
                dict.Emplace(i, i);
            }

            int running = threads;
            using (ManualResetEvent mre = new ManualResetEvent(false))
            {
                for (int i = 0; i < threads; i++)
                {
                    int ii = i;
                    Task.Run(
                        () =>
                        {
                            for (int j = 1; j <= updatesPerThread; j++)
                            {
                                dict.Update(j, (ii + 2) * j);
                            }
                            if (Interlocked.Decrement(ref running) == 0)
                            {
                                mre.Set();
                            }
                        });
                }
                mre.WaitOne();
            }

            foreach (var pair in dict.Entries)
            {
                var div = pair.Value / pair.Key;
                var rem = pair.Value % pair.Key;

                Assert.Equal(0, rem);
                Assert.True(div > 1 && div <= threads + 1,
                    string.Format("* Invalid value={2}! TestUpdate1(threads={0}, updatesPerThread={1})", threads, updatesPerThread, div));
            }

            List<int> gotKeys = new List<int>();
            foreach (var pair in dict.Entries)
            {
                gotKeys.Add(pair.Key);
            }

            gotKeys.Sort();

            List<int> expectKeys = new List<int>();
            for (int i = 1; i <= updatesPerThread; i++)
                expectKeys.Add(i);

            Assert.Equal(expectKeys.Count, gotKeys.Count);

            for (int i = 0; i < expectKeys.Count; i++)
            {
                Assert.True(expectKeys[i].Equals(gotKeys[i]),
                   string.Format("The set of keys in the dictionary is are not the same as the expected." + Environment.NewLine +
                           "TestUpdate1(threads={0}, updatesPerThread={1})", threads, updatesPerThread)
                  );
            }
        }

        [Fact]
        public void UpdateTestsPart2()
        {
            var map = new CMap<string, int>();
            const int numThreads = 10;
            const int iterationsPerThread = 1000;
            int counter = 0;

            var tasks = new Task[numThreads];
            map.Emplace("key", counter);

            // Simulate concurrent updates to the same key
            for (int i = 0; i < numThreads; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < iterationsPerThread; j++)
                    {
                        Interlocked.Increment(ref counter);
                        map.Update("key", counter);
                    }
                });
            }

            Task.WaitAll(tasks, TimeSpan.FromSeconds(10));

            // Check if the final count matches the expected number of updates
            int expectedUpdates = numThreads * iterationsPerThread;
            if (!map.Get("key", out int finalValue) && finalValue == expectedUpdates)
            {
                Assert.Fail($"Test failed! Expected updates: {expectedUpdates}, Final value: {finalValue}");
            }
        }
    }
}
