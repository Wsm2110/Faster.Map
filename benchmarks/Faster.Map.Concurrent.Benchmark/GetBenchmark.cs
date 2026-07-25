using BenchmarkDotNet.Attributes;
using Faster.Map.Core;
using NonBlocking;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Faster.Map.Concurrent.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    public class GetBenchmark
    {
        CMap<uint, uint> _map = new CMap<uint, uint>();
        NonBlocking.ConcurrentDictionary<uint, uint> _block = new NonBlocking.ConcurrentDictionary<uint, uint>();
        System.Collections.Concurrent.ConcurrentDictionary<uint, uint> _dic            ;

        [Params(1000000)]
        public uint Length { get; set; }
        private uint[] keys;

        [Params(1, 2, 4, 8, 16)] // Example thread counts to test scalability
        public int NumberOfThreads { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _dic = new System.Collections.Concurrent.ConcurrentDictionary<uint, uint>();
            var output = File.ReadAllText("Numbers.txt");
            var splittedOutput = output.Split(',');

            keys = new uint[Length];

            for (var index = 0; index < Length; index++)
            {
                keys[index] = uint.Parse(splittedOutput[index]);
            }

            foreach (var key in keys)
            {
                _map.Emplace(key, key);
                _block.TryAdd(key, key);
                _dic.TryAdd(key, key);
            }
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
                    _dic.TryGetValue(keys[i], out _);
                }
            });
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
                    _block.TryGetValue(keys[i], out _);
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
                    _map.Get(keys[i], out _);
                }
            });
        }
    }
}