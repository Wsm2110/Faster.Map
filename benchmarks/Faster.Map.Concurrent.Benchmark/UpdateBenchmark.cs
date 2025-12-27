using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Faster.Map.Core;

namespace Faster.Map.Concurrent.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    public class UpdateBenchmark
    {
        private CMap<uint, uint> _map;
        private System.Collections.Concurrent.ConcurrentDictionary<uint, uint> _concurrentMap;
        private NonBlocking.ConcurrentDictionary<uint, uint> _nonBlocking;

        [Params(1000000)]
        public uint Length { get; set; }
        private uint[] keys;

        private const int N = 1000000; // Adjust as needed for your scale

        [Params(1, 8, 16, 32, 64, 128 /*, 256, 512*/)] // Example thread counts to test scalability
        public int NumberOfThreads { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _map = new CMap<uint, uint>(2000000);
            _nonBlocking = new NonBlocking.ConcurrentDictionary<uint, uint>(NumberOfThreads, 2000000);
            _concurrentMap = new System.Collections.Concurrent.ConcurrentDictionary<uint, uint>(NumberOfThreads, 1000000);

            var output = File.ReadAllText("Numbers.txt");
            var splittedOutput = output.Split(',');

            keys = new uint[Length];

            for (var index = 0; index < Length; index++)
            {
                keys[index] = uint.Parse(splittedOutput[index]);
            }
        }

        [IterationSetup]
        public void Clean()
        {
            _map = new CMap<uint, uint>(2000000);
            _nonBlocking = new NonBlocking.ConcurrentDictionary<uint, uint>(NumberOfThreads, 2000000);
            _concurrentMap = new System.Collections.Concurrent.ConcurrentDictionary<uint, uint>(NumberOfThreads, 1000000);

        }


        [Benchmark]
        public void NonBlocking()
        {
            int numKeys = 1000000;
            int segmentSize = numKeys / NumberOfThreads;

            Parallel.For(0, NumberOfThreads, threadIndex =>
            {
                int start = threadIndex * segmentSize;
                int end = (threadIndex == NumberOfThreads - 1) ? numKeys : start + segmentSize;

                for (uint i = (uint)start; i < end; i++)
                {
                    var key = keys[i];
                    _nonBlocking[key] = 0;
                }
            });
        }

        [Benchmark]
        public void CMap()
        {
            int numKeys = 1000000;
            int segmentSize = numKeys / NumberOfThreads;

            Parallel.For(0, NumberOfThreads, threadIndex =>
            {
                int start = threadIndex * segmentSize;
                int end = (threadIndex == NumberOfThreads - 1) ? numKeys : start + segmentSize;

                for (uint i = (uint)start; i < end; i++)
                {
                    var key = keys[i];
                     _map.Update(key, 0);                
                }
            });
        }

        [Benchmark]
        public void ConcurrentDictionary()
        {
            int numKeys = 1000000;
            int segmentSize = numKeys / NumberOfThreads;

            Parallel.For(0, NumberOfThreads, threadIndex =>
            {
                int start = threadIndex * segmentSize;
                int end = (threadIndex == NumberOfThreads - 1) ? numKeys : start + segmentSize;

                for (uint i = (uint)start; i < end; i++)
                {
                    var key = keys[i];
                    _concurrentMap[key] = 0;
                }
            });
        }


    }
}