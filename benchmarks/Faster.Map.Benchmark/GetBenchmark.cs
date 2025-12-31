using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using Faster.Map.Benchmark.Utilities;
using Faster.Map.Core;
using Faster.Map.Hashing;
using Faster.Map.Hashing.Algorithm;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]  
    [SimpleJob(RunStrategy.Monitoring, launchCount: 1, iterationCount: 20, warmupCount: 5)]
    //[HardwareCounters(
    //HardwareCounter.BranchMispredictions,
    //HardwareCounter.BranchInstructions,
    //HardwareCounter.CacheMisses,
    //HardwareCounter.TotalCycles)]

    public class GetBenchmark
    {
        #region Fields

        private DenseMap<uint, uint, FastHasherUint> _denseMap;
        private BlitzMap<uint, uint, FastHasherUint> _blitz;
        private Dictionary<uint, uint> _dictionary;
        private RobinhoodMap<uint, uint, FastHasherUint> _robinHoodMap;

        private uint[] keys;

        #endregion

        #region Properties

        [Params(/*0.1, 0.2, 0.3, 0.4, 0.5,*/ 0.5/*, 0.7, 0.8*/)]
        public static double LoadFactor { get; set; }

        [Params(134_217_728)]
        public static uint Length { get; set; }

        #endregion

        /// <summary>
        /// Generate a million Keys and shuffle them afterwards
        /// </summary>
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

            // round of length to power of 2 prevent resizing
            uint length = BitOperations.RoundUpToPowerOf2(Length);
            int dicLength = HashHelpers.GetPrime((int)Length);

            _denseMap = new DenseMap<uint, uint, FastHasherUint>(length);
            _blitz = new BlitzMap<uint, uint, FastHasherUint>((int)length, LoadFactor);

            _dictionary = new Dictionary<uint, uint>(dicLength);
            _robinHoodMap = new RobinhoodMap<uint, uint, FastHasherUint>(length, 0.9);

            foreach (var key in keys)
            {
              //  _dictionary.Add(key, key);
              //  _denseMap.InsertOrUpdate(key, key);
                _blitz.Insert(key, key);           
              //  _robinHoodMap.Emplace(key, key);
            }
        }

        [Benchmark]
        public void BlitzMap()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                _blitz.Get(key, out var _);
            }
        }

        //[Benchmark]
        //public void DenseMap()
        //{
        //    for (int i = 0; i < keys.Length; i++)
        //    {
        //        var key = keys[i];
        //        _denseMap.Get(key, out var _);
        //    }
        //}

        //[Benchmark]
        //public void Dictionary()
        //{
        //    for (int i = 0; i < keys.Length; i++)
        //    {
        //        var key = keys[i];
        //        _dictionary.TryGetValue(key, out var _);
        //    }
        //}

        //[Benchmark]
        //public void RobinhoodMap()
        //{
        //    for (int i = 0; i < keys.Length; i++)
        //    {
        //        var key = keys[i];
        //        _robinHoodMap.Get(key, out var _);
        //    }
        //}
    }
}