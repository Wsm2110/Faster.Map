using Faster.Map.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Map.CMap.Tests
{
    public class GetTests
    {
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
