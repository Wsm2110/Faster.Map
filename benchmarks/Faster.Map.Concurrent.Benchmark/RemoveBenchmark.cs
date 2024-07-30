using BenchmarkDotNet.Attributes;
using NonBlocking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Faster.Map.Concurrent.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    public class RemoveBenchmark
    {
        CMap<uint, uint> _map = new CMap<uint, uint>();
        NonBlocking.ConcurrentDictionary<uint, uint> _block = new NonBlocking.ConcurrentDictionary<uint, uint>();
        System.Collections.Concurrent.ConcurrentDictionary<uint, uint> _dic;

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

        [IterationSetup]
        public void Clean()
        {
            _map = new CMap<uint, uint>(2000000);
            _block = new NonBlocking.ConcurrentDictionary<uint, uint>(NumberOfThreads, 2000000);
            _dic = new System.Collections.Concurrent.ConcurrentDictionary<uint, uint>(NumberOfThreads, 1000000);


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
            Parallel.For(0, Length, new ParallelOptions { MaxDegreeOfParallelism = NumberOfThreads }, i =>
            {
                var key = keys[i];
                _dic.Remove(key, out var _);
            });
        }

        [Benchmark]
        public void NonBlocking()
        {
            Parallel.For(0, Length, new ParallelOptions { MaxDegreeOfParallelism = NumberOfThreads }, i =>
            {
                var key = keys[i];
                _block.Remove(key, out var _);
            });
        }

        [Benchmark]
        public void CMap()
        {
            Parallel.For(0, Length, new ParallelOptions { MaxDegreeOfParallelism = NumberOfThreads }, i =>
            {
                var key = keys[i];
                _map.Remove(key, out var _);
            });
        }
    }
}