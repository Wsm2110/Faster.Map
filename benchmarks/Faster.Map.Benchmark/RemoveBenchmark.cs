using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    //[SimpleJob(RunStrategy.Monitoring, 1, 10, 50)]
    public class RemoveBenchmark
    {
        #region Fields

        private DenseMap<uint, uint> _denseMap;
        private Dictionary<uint, uint> _dictionary;
        private RobinhoodMap<uint, uint> _robinhoodMap;
        private DenseMap<uint, uint> _denseMapxxHash;
        private DenseMap<uint, uint> _denseMapGxHash;
        private DenseMap<uint, uint> _denseMapFastHash;

        private uint[] keys;

        #endregion

        #region Properties

        [Params(100, 10000, 100000, 400000, 900000, 1000000)]
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

            keys = new uint[Length];

            for (var index = 0; index < Length; index++)
            {
                keys[index] = uint.Parse(splittedOutput[index]);
            }          
        }

        [IterationSetup]
        public void IterationSetupX()
        {
            // round of length to power of 2 prevent resizing
            uint length = BitOperations.RoundUpToPowerOf2(Length);
            int dicLength = HashHelpers.GetPrime((int)Length);

            _denseMap = new DenseMap<uint, uint>(length);
            _dictionary = new Dictionary<uint, uint>(dicLength);
            _robinhoodMap = new RobinhoodMap<uint, uint>(length * 2);

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

        #region Benchmarks

        [Benchmark]
        public void DenseMap()
        {
            foreach (var key in keys)
            {
                _denseMap.Remove(key);
            }
        }

        [Benchmark]
        public void DenseMap_Xxhash3()
        {
            foreach (var key in keys)
            {
                _denseMapxxHash.Remove(key);
            }
        }

        [Benchmark]
        public void DenseMap_GxHash()
        {
            foreach (var key in keys)
            {
                _denseMapGxHash.Remove(key);
            }
        }

        [Benchmark]
        public void DenseMap_FastHash()
        {
            foreach (var key in keys)
            {
                _denseMapFastHash.Remove(key);
            }
        }

        [Benchmark]
        public void RobinhoodMap()
        {
            foreach (var key in keys)
            {
                _robinhoodMap.Remove(key);
            }
        }

        [Benchmark]
        public void Dictionary()
        {
            foreach (var key in keys)
            {
                _dictionary.Remove(key, out var result);
            }
        }
        #endregion
    }
}