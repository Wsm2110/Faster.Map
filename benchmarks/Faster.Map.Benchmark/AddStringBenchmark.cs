using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using System.Linq;
using System.Numerics;

using System.Collections;
using Faster.Map.Hasher;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    public class AddStringBenchmark
    {
        #region Fields

        //fixed size, dont want to measure resize()
        private DenseMap<string, string> _dense;
        private Dictionary<string, string> dic;
        private RobinhoodMap<string, string> _robinhoodMap;
        private DenseMap<string, string> _denseMapxxHash;
        private DenseMap<string, string> _denseMapGxHash;
        private DenseMap<string, string> _denseMapFastHash;

        private string[] keys;

        #endregion

        #region Properties

        [Params(800, 10000, 100000, 200000, 400000, 800000)]

        public uint Length { get; set; }

        #endregion

        /// <summary>
        /// Generate a million Keys and shuffle them afterwards
        /// </summary>
        [GlobalSetup]
        public void Add()
        {
            var output = File.ReadAllText("Numbers.txt");
            var splittedOutput = output.Split(',');

            keys = new string[1000000];

            for (var index = 0; index < Length; index++)
            {
                keys[index] = splittedOutput[index];
            }
        }

        [IterationSetup]
        public void Setup()
        {
            // round of length to power of 2 prevent resizing
            uint length = BitOperations.RoundUpToPowerOf2(Length);
            int dicLength = HashHelpers.GetPrime((int)Length);

            _dense = new DenseMap<string, string>(length, 0.875, new XxHash3StringHasher());
            _denseMapxxHash = new DenseMap<string, string>(length, 0.875, new XxHash3StringHasher());
             _denseMapFastHash = new DenseMap<string, string>(length, 0.875, new FastHasher());

            dic = new Dictionary<string, string>(dicLength);
            _robinhoodMap = new RobinhoodMap<string, string>(length * 2);
        }

        #region Benchmarks

        [Benchmark]
        public void DenseMap()
        {
            for (int i = 0; i < Length; i++)
            {
                var key = keys[i];
                _dense.Emplace(key, key);
            }
        }


        [Benchmark]
        public void DenseMap_Xxhash3()
        {
            for (int i = 0; i < Length; i++)
            {
                var key = keys[i];
                _denseMapxxHash.Emplace(key, key);
            }
        }

        [Benchmark]
        public void DenseMap_GxHash()
        {
            for (int i = 0; i < Length; i++)
            {
                var key = keys[i];
                _denseMapGxHash.Emplace(key, key);
            }
        }

        [Benchmark]
        public void DenseMap_FastHash()
        {
            for (int i = 0; i < Length; i++)
            {
                var key = keys[i];
                _denseMapFastHash.Emplace(key, key);
            }
        }

        [Benchmark]
        public void RobinhoodMap()
        {
            for (int i = 0; i < Length; i++)
            {
                var key = keys[i];
                _robinhoodMap.Emplace(key, key);
            }
        }

        [Benchmark]
        public void Dictionary()
        {
            for (int i = 0; i < Length; i++)
            {
                var key = keys[i];
                dic.Add(key, key);
            }
        }

        #endregion
    }
}