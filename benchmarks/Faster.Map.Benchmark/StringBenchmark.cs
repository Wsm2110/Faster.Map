using BenchmarkDotNet.Attributes;
using Faster.Map.Hash;
using Faster.Map.Hasher;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    public class StringBenchmark
    {
        #region Fields

        private DenseMap<string, string> _denseMap;
        private DenseMap<string, string> _denseMapxxHash;
        private DenseMap<string, string> _denseMapGxHash;
        private DenseMap<string, string> _denseMapFastHash;
        private Dictionary<string, string> _dictionary;
        private RobinhoodMap<string, string> _robinhoodMap;

        private string[] keys;

        #endregion

        #region Properties

        [Params(/*100, 1000, 10000, 100000, 200000, 400000, 800000,*/ 1000000)]
        public uint Length { get; set; }

        #endregion

        /// <summary>
        /// Generate a million Keys and shuffle them afterwards
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            var output = File.ReadAllText("Numbers.txt");
            var splittedOutput = output.Split(',');

            keys = new string[Length];

            for (var index = 0; index < Length; index++)
            {
                keys[index] = splittedOutput[index];
            }

            // round of length to power of 2 prevent resizing
            uint length = BitOperations.RoundUpToPowerOf2(Length);
            int dicLength = HashHelpers.GetPrime((int)Length);

            _denseMap = new DenseMap<string, string>(length, 0.875);

            _denseMapxxHash = new DenseMap<string, string>(length, 0.875, new XxHash3StringHasher());
            _denseMapGxHash = new DenseMap<string, string>(length, 0.875, new GxHasher());
            _denseMapFastHash = new DenseMap<string, string>(length, 0.875, new FastHasher());
            _dictionary = new Dictionary<string, string>(dicLength);
            _robinhoodMap = new RobinhoodMap<string, string>(length * 2);

            foreach (var key in keys)
            {
                _dictionary.Add(key, key);
                _denseMap.Emplace(key, key);
                _denseMapxxHash.Emplace(key, key);
                _denseMapGxHash.Emplace(key, key);
                _denseMapFastHash.Emplace(key, key);
                _robinhoodMap.Emplace(key, key);
            }

        }

        //[Benchmark]
        //public void DenseMap_Default()
        //{
        //    foreach (var key in keys)
        //    {
        //        _denseMap.Get(key, out var result);
        //    }
        //}

        //[Benchmark]
        //public void DenseMap_Xxhash3()
        //{
        //    foreach (var key in keys)
        //    {
        //        _denseMapxxHash.Get(key, out var result);
        //    }
        //}

        [Benchmark]
        public void DenseMap_GxHash()
        {
            foreach (var key in keys)
            {
                _denseMapGxHash.Get(key, out var result);
            }
        }

        [Benchmark]
        public void DenseMap_FastHash()
        {
            foreach (var key in keys)
            {
                _denseMapFastHash.Get(key, out var result);
            }  
        }

        [Benchmark]
        public void RobinhoodMap()
        {
            foreach (var key in keys)
            {
                _robinhoodMap.Get(key, out var result);
            }
        }

        [Benchmark]
        public void Dictionary()
        {
            foreach (var key in keys)
            {
                _dictionary.TryGetValue(key, out var result);
            }
        }
    }
}
