using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Faster.Map.Concurrent.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    public class AddResizeBenchmark
    {
        private CMap<uint, uint> _map;
        private System.Collections.Concurrent.ConcurrentDictionary<uint, uint> _concurrentMap;
        private NonBlocking.ConcurrentDictionary<uint, uint> _nonBlocking;

        [Params(1000000)]
        public uint Length { get; set; }
        private uint[] keys;

        private const int N = 1000000; // Adjust as needed for your scale

        [Params(1, 2, 4, 6, 8 /*, 16, 32, 64, 128*/)] // Example thread counts to test scalability
        public int NumberOfThreads { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _map = new CMap<uint, uint>();
            _nonBlocking = new NonBlocking.ConcurrentDictionary<uint, uint>();
            _concurrentMap = new System.Collections.Concurrent.ConcurrentDictionary<uint, uint>();

            var output = File.ReadAllText("Numbers.txt");
            var splittedOutput = output.Split(',');

            keys = new uint[Length];

            for (var index = 0; index < Length; index++)
            {
                keys[index] = uint.Parse(splittedOutput[index]);
            }
        }

        [IterationCleanup]
        public void Clean()
        {
            _map = new CMap<uint, uint>();
            _nonBlocking = new NonBlocking.ConcurrentDictionary<uint, uint>();
            _concurrentMap = new System.Collections.Concurrent.ConcurrentDictionary<uint, uint>();
        }

        //[Benchmark]
        //public void NonBlocking()
        //{
        //    Parallel.For(0, N, new ParallelOptions { MaxDegreeOfParallelism = NumberOfThreads }, i =>
        //    {
        //        var key = keys[i];
        //        _nonBlocking.TryAdd(key, key);
        //    });
        //}

        [Benchmark]
        public void CMap()
        {
            Parallel.For(0, N, new ParallelOptions { MaxDegreeOfParallelism = NumberOfThreads }, i =>
            {
                var key = keys[i];
                _map.Emplace(keys[i], 0);
            });
        }

        //[Benchmark]
        //public void ConcurrentDictionary()
        //{
        //    Parallel.For(0, N, new ParallelOptions { MaxDegreeOfParallelism = NumberOfThreads }, i =>
        //    {
        //        var key = keys[i];
        //        _concurrentMap.TryAdd(key, key);
        //    });
        //}


    }
}