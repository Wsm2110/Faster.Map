using Faster.Map.Concurrent;
using System.Collections;
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

        [Fact]
        public static void TestTryUpdate()
        {
            var map = new CMap<string, int>();
        
            for (int i = 0; i < 10; i++)
            {
                map.Emplace(i.ToString(), i);
            }

            for (int i = 0; i < 10; i++)
            {
                Assert.True(map.Update(i.ToString(), i + 1, i), "TestTryUpdate:  FAILED.  TryUpdate failed!");
                Assert.Equal(i + 1, map[i.ToString()]);
            }

            //test TryUpdate concurrently
            map.Clear();
            for (int i = 0; i < 1000; i++)
            {
                map.Emplace(i.ToString(), i);
            }

            var mres = new ManualResetEventSlim();
            Task[] tasks = new Task[10];
            ThreadLocal<ThreadData> updatedKeys = new ThreadLocal<ThreadData>(true);
            for (int i = 0; i < tasks.Length; i++)
            {
                // We are creating the Task using TaskCreationOptions.LongRunning because...
                // there is no guarantee that the Task will be created on another thread.
                // There is also no guarantee that using this TaskCreationOption will force
                // it to be run on another thread.
                tasks[i] = Task.Factory.StartNew((obj) =>
                {
                    mres.Wait();
                    int index = (((int)obj) + 1) + 1000;
                    updatedKeys.Value = new ThreadData();
                    updatedKeys.Value.ThreadIndex = index;

                    for (int j = 0; j < map.Count; j++)
                    {
                        if (map.Update(j.ToString(), index, j))
                        {
                            if (map[j.ToString()] != index)
                            {
                                updatedKeys.Value.Succeeded = false;
                                return;
                            }
                            updatedKeys.Value.Keys.Add(j.ToString());
                        }
                    }
                }, i, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }

            mres.Set();
            Task.WaitAll(tasks);

            int numberSucceeded = 0;
            int totalKeysUpdated = 0;
            foreach (var threadData in updatedKeys.Values)
            {
                totalKeysUpdated += threadData.Keys.Count;
                if (threadData.Succeeded)
                {
                    numberSucceeded++;
                }
            }

            Assert.True(numberSucceeded == tasks.Length, "One or more threads failed!");
            Assert.True(totalKeysUpdated == map.Count, string.Format("TestTryUpdate:  FAILED.  The updated keys count doesn't match the dictionary count, expected {0}, actual {1}", map.Count, totalKeysUpdated));

            foreach (var value in updatedKeys.Values)
            {
                for (int i = 0; i < value.Keys.Count; i++)
                {
                    Assert.True(map[value.Keys[i]] == value.ThreadIndex, string.Format("TestTryUpdate:  FAILED.  The updated value doesn't match the thread index, expected {0} actual {1}", value.ThreadIndex, map[value.Keys[i]]));
                }
            }
        }

        [Fact]
        public void Update_NonExistentKey_ShouldReturnFalse()
        {
            var map = new CMap<int, string>();
            // Arrange
            int key = 2;
            string newValue = "Updated";

            // Act
            bool result = map.Update(key, newValue);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Update_DuringResize_ShouldHandleCorrectly()
        {
            // Arrange
            var map = new CMap<int, string>();

            int key = 4;
            string initialValue = "Initial";
            string newValue = "Updated";
            map.Emplace(key, initialValue);

            // Simulate resize
            var resizeTask = Task.Run(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    map.Emplace(i, $"Value {i}");
                }
            });

            // Act
            bool result = map.Update(key, newValue);
            resizeTask.Wait(TimeSpan.FromSeconds(10));

            // Assert
            Assert.True(result);
            Assert.Equal(newValue, map[key]);
        }

        [Fact]
        public void ConcurrentUpdates_ShouldHandleCorrectly()
        {
            var map = new CMap<int, string>();

            // Arrange
            int key = 3;
            string initialValue = "Initial";
            map.Emplace(key, initialValue);

            // Act
            var tasks = new Task[100];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() => map.Update(key, $"Updated {i}"));
            }

            Task.WaitAll(tasks, TimeSpan.FromSeconds(5));

            // Assert
            string finalValue = map[key];
            Assert.Contains("Updated", finalValue);
        }

        [Fact]
        public void ConcurrentAddOperations_ShouldAddAllElements()
        {
            var map = new CMap<string, int>();
            var tasks = new Task[100];
            var mres = new ManualResetEventSlim();

            for (int i = 0; i < tasks.Length; i++)
            {
                int taskIndex = i;
                tasks[taskIndex] = Task.Factory.StartNew(() =>
                {
                    mres.Wait();
                    map.Emplace(taskIndex.ToString(), taskIndex);
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }

            mres.Set();
            Task.WaitAll(tasks, TimeSpan.FromSeconds(10));

            for (int i = 0; i < tasks.Length; i++)
            {
                Assert.True(map.Get(i.ToString(), out var value));
                Assert.Equal(i, value);
            }
        }

        [Fact]
        public void ConcurrentRemoveOperations_ShouldRemoveAllElements()
        {
            var map = new CMap<string, int>();

            for (int i = 0; i < 100; i++)
            {
                map.Emplace(i.ToString(), i);
            }

            var tasks = new Task[100];
            var mres = new ManualResetEventSlim();

            for (int i = 0; i < tasks.Length; i++)
            {
                int taskIndex = i;
                tasks[taskIndex] = Task.Factory.StartNew(() =>
                {
                    mres.Wait();
                    map.Remove(taskIndex.ToString(), out var _);
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }

            mres.Set();
            Task.WaitAll(tasks, TimeSpan.FromSeconds(10));

            for (int i = 0; i < tasks.Length; i++)
            {
                Assert.False(map.Get(i.ToString(), out _));
            }
        }

        [Fact]
        public void ConcurrentMixedOperations_ShouldHandleCorrectly()
        {
            var map = new CMap<string, int>();

            var tasks = new Task[125];
            var mres = new ManualResetEventSlim();

            // Phase 1: Add operations
            for (int i = 0; i < 50; i++)
            {
                int taskIndex = i;
                tasks[taskIndex] = Task.Factory.StartNew(() =>
                {
                    map.Emplace(taskIndex.ToString(), taskIndex);
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }

            // Phase 2: Update operations
            for (int i = 50; i < 100; i++)
            {
                int taskIndex = i - 50;
                tasks[i] = Task.Factory.StartNew(() =>
                {
                    if (map.Update(taskIndex.ToString(), taskIndex + 100))
                    {
                        var result = map.Get(taskIndex.ToString(), out var value);
                        Assert.True(result);
                        Assert.Equal(taskIndex + 100, value);
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }

            // Phase 3: Remove operations (remove only a subset)
            for (int i = 100; i < 125; i++)
            {
                int taskIndex = i - 100;
                tasks[i] = Task.Factory.StartNew(() =>
                {
                    map.Remove(taskIndex.ToString(), out var _);
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }

            mres.Set();
            Task.WaitAll(tasks, TimeSpan.FromSeconds(10));

            // Verify results
            for (int i = 0; i < 25; i++)
            {
                Assert.False(map.Get(i.ToString(), out _), $"Key {i} should have been removed.");
            }

            for (int i = 25; i < 50; i++)
            {
                Assert.True(map.Get(i.ToString(), out var value), $"Key {i} should still exist.");
                Assert.Equal(i + 100, value);
            }
        }
        private class ThreadData
        {
            public int ThreadIndex;
            public bool Succeeded = true;
            public List<string> Keys = new List<string>();
        }
    }
}
