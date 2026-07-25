using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Faster.Map.Benchmark.Utilities;
using Faster.Map.Core;
using Faster.Map.Hashing;
using Faster.Map.Hashing.Algorithm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, launchCount: 1, iterationCount: 5, warmupCount: 3, invocationCount: 1)]
    public class RemoveBenchmark
    {
        #region Fields

        private DenseMap<uint, uint, FastHasher.UInt> _denseMap;
        private BlitzMap<uint, uint, FastHasher.UInt> _blitz;
        private Dictionary<uint, uint> _dictionary;
        private RobinhoodMap<uint, uint, FastHasher.UInt> _robinHoodMap;

        private uint[] keys;
        private uint[] shuffledRemoveKeys;

        #endregion

        #region Properties

        [Params(0.5)]
        public double LoadFactor { get; set; }

        [Params(16_777_216)]
        public uint Length { get; set; }

        #endregion

        [GlobalSetup]
        public void Setup()
        {
            var rnd = new FastRandom(3);
            var uni = new HashSet<uint>((int)Length * 2);
            while (uni.Count < (uint)(Length * LoadFactor))
            {
                uni.Add((uint)rnd.Next());
            }

            keys = uni.ToArray();

            // This ensures we are testing random-access removal, not sequential removal
            shuffledRemoveKeys = new uint[keys.Length];
            Array.Copy(keys, shuffledRemoveKeys, keys.Length);
            Random.Shared.Shuffle(shuffledRemoveKeys);
        }

        [IterationSetup(Target = nameof(BlitzMap))]
        public void SetupBlitz()
        {
            uint length = BitOperations.RoundUpToPowerOf2(Length);
            _blitz = new BlitzMap<uint, uint, FastHasher.UInt>((int)length, LoadFactor);
            foreach (var key in keys) _blitz.Insert(key, key);
        }

        [IterationSetup(Target = nameof(DenseMap))]
        public void SetupDense()
        {
            uint length = BitOperations.RoundUpToPowerOf2(Length);
            _denseMap = new DenseMap<uint, uint, FastHasher.UInt>(length);
            foreach (var key in keys) _denseMap.InsertOrUpdate(key, key);
        }

        //[IterationSetup(Target = nameof(RobinhoodMap))]
        //public void SetupRobinhood()
        //{
        //    uint length = BitOperations.RoundUpToPowerOf2(Length);
        //    _robinHoodMap = new RobinhoodMap<uint, uint, FastHasher.UInt>(length, LoadFactor);
        //    foreach (var key in keys) _robinHoodMap.Emplace(key, key);
        //}

        //[IterationSetup(Target = nameof(Dictionary))]
        //public void SetupDictionary()
        //{
        //    int dicLength = HashHelpers.GetPrime((int)Length);
        //    _dictionary = new Dictionary<uint, uint>(dicLength);
        //    foreach (var key in keys) _dictionary.Add(key, key);
        //}

        #region Benchmarks

      
        [Benchmark]
        public int BlitzMap()
        {
            int count = 0;
            var localKeys = shuffledRemoveKeys;
            for (int i = 0; i < localKeys.Length; i++)
            {
                if (_blitz.Remove(localKeys[i])) count++;
            }
            return count;
        }

        [Benchmark]
        public int DenseMap()
        {
            int count = 0;
            var localKeys = shuffledRemoveKeys;
            for (int i = 0; i < localKeys.Length; i++)
            {
                if (_denseMap.Remove(localKeys[i])) count++;
            }
            return count;
        }

        //[Benchmark]
        //public int RobinhoodMap()
        //{
        //    int count = 0;
        //    var localKeys = shuffledRemoveKeys;
        //    for (int i = 0; i < localKeys.Length; i++)
        //    {
        //        if (_robinHoodMap.Remove(localKeys[i])) count++;
        //    }
        //    return count;
        //}

        //[Benchmark]
        //public int Dictionary()
        //{
        //    int count = 0;
        //    var localKeys = shuffledRemoveKeys;
        //    for (int i = 0; i < localKeys.Length; i++)
        //    {
        //        if (_dictionary.Remove(localKeys[i])) count++;
        //    }
        //    return count;
        //}

        #endregion
    }
}