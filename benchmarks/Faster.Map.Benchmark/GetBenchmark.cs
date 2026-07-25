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
    public class GetBenchmark
    {
        #region Fields

        private DenseMap<uint, uint, FastHasher.UInt> _denseMap;
        private BlitzMap<uint, uint, FastHasher.UInt> _blitz;
        private Dictionary<uint, uint> _dictionary;
        private RobinhoodMap<uint, uint, FastHasher.UInt> _robinHoodMap;

        private uint[] keys;

        #endregion

        #region Properties

        [Params(0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8)]
        public static double LoadFactor { get; set; }

        [Params(1024 * 1024)]
        public static uint Length { get; set; }

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

            Random.Shared.Shuffle(keys);

            uint length = BitOperations.RoundUpToPowerOf2(Length);
            int dicLength = HashHelpers.GetPrime((int)Length);

            _denseMap = new DenseMap<uint, uint, FastHasher.UInt>(length);
            _blitz = new BlitzMap<uint, uint, FastHasher.UInt>((int)length, LoadFactor);
            _dictionary = new Dictionary<uint, uint>(dicLength);
            _robinHoodMap = new RobinhoodMap<uint, uint, FastHasher.UInt>(length, LoadFactor);

            foreach (var key in keys)
            {
                _dictionary.Add(key, key);
                _denseMap.InsertOrUpdate(key, key);
                _blitz.Insert(key, key);
                _robinHoodMap.Emplace(key, key);
            }
        }

        [Benchmark]
        public uint BlitzMap()
        {
            uint sum = 0;
            var localKeys = keys; // Cache array reference locally
            for (int i = 0; i < localKeys.Length; i++)
            {
                _blitz.Get(localKeys[i], out var val);
                sum += val;
            }
            return sum;
        }

        [Benchmark]
        public uint DenseMap()
        {
            uint sum = 0;
            var localKeys = keys;
            for (int i = 0; i < localKeys.Length; i++)
            {
                _denseMap.Get(localKeys[i], out var val);
                sum += val;
            }
            return sum;
        }

        //[Benchmark]
        //public uint Dictionary()
        //{
        //    uint sum = 0;
        //    var localKeys = keys;
        //    for (int i = 0; i < localKeys.Length; i++)
        //    {
        //        _dictionary.TryGetValue(localKeys[i], out var val);
        //        sum += val;
        //    }
        //    return sum;
        //}

        //[Benchmark]
        //public uint RobinhoodMap()
        //{
        //    uint sum = 0;
        //    var localKeys = keys;
        //    for (int i = 0; i < localKeys.Length; i++)
        //    {
        //        _robinHoodMap.Get(localKeys[i], out var val);
        //        sum += val;
        //    }
        //    return sum;
        //}
    }
}