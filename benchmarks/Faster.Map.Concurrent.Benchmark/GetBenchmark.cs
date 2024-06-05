using BenchmarkDotNet.Attributes;
using Faster.Map.DenseMap;
using Faster.Map.QuadMap;
using Faster.Map.RobinHoodMap;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Map.Concurrent.Benchmark
{
    public class GetBenchmark
    {
        CMap<uint, uint> _map = new CMap<uint, uint>();
        NonBlocking.ConcurrentDictionary<uint, uint> _dic = new NonBlocking.ConcurrentDictionary<uint, uint>();
        ConcurrentDictionary<uint, uint> _dic2;

        [Params(1000000)]
        public uint Length { get; set; }
        private uint[] keys;


        [Params(1, 8, 16, 32, 64, 128)] // Example thread counts to test scalability
        public int NumberOfThreads { get; set; }


        [GlobalSetup]
        public void Setup()
        {
            _dic2 = new ConcurrentDictionary<uint, uint>(NumberOfThreads, 1);
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
                _dic.TryAdd(key, key);
                _dic2.TryAdd(key, key);
            }
        }

        [Benchmark]
        public void ConcurrentDictionary()
        {
            Parallel.For(0, NumberOfThreads, i =>
            {
                for (int j = 0; j < Length; j++)
                {
                    var key = keys[j];
                    _dic2.TryGetValue(key, out _);
                }
            });
        }


        [Benchmark]
        public void NonBlocking()
        {
            Parallel.For(0, NumberOfThreads, i =>
            {
                for (int j = 0; j < Length; j++)
                {
                    var key = keys[j];
                    _dic.TryGetValue(key, out _);
                }
            });
        }

        [Benchmark]
        public void GetCmapBenchmark()
        {
            Parallel.For(0, NumberOfThreads, i =>
            {
                for (int j = 0; j < Length; j++)
                {
                    var key = keys[j];
                    _map.Get(key, out _);
                }
            });
        }
    }
}
