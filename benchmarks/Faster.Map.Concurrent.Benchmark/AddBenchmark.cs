using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Faster.Map.Concurrent.Benchmark
{
    public class AddBenchmark
    {
        private CMap<int, int> _map;
        private System.Collections.Concurrent.ConcurrentDictionary<int, int> _concurrentMap;
        private NonBlocking.ConcurrentDictionary<int, int> _nonBlocking;

        private const int N = 10000000; // Adjust as needed for your scale

        [Params(1, 8, 16, 32, 64, 128, 256, 512)] // Example thread counts to test scalability
        public int NumberOfThreads { get; set; }

        [GlobalSetup]
        public void Setup()
        {

            _map = new CMap<int, int>(10000000);

            // _nonBlocking = new NonBlocking.ConcurrentDictionary<int, int>(NumberOfThreads, 1000000);
            // _concurrentMap = new System.Collections.Concurrent.ConcurrentDictionary<int, int>(NumberOfThreads, 1000000);
        }

        //[Benchmark]
        //public void NonBlocking()
        //{
        //    Parallel.For(0, N, new ParallelOptions { MaxDegreeOfParallelism = NumberOfThreads }, i =>
        //    {
        //        _nonBlocking.TryAdd(i, i);
        //    });
        //}

        [Benchmark]
        public void CMap()
        {
            Parallel.For(0, N, new ParallelOptions { MaxDegreeOfParallelism = NumberOfThreads }, i =>
                    {
                        _map.Emplace(i, i);
                    });
        }

        //[Benchmark]
        //public void ConcurrentDictionary()
        //{
        //    Parallel.For(0, N, new ParallelOptions { MaxDegreeOfParallelism = NumberOfThreads }, i =>
        //    {
        //        _concurrentMap.TryAdd(i, i);
        //    });
        //}


    }
}