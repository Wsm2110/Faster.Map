using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections;
using System.Numerics;
using Faster.Map.Hasher;
using System.Linq;
using BenchmarkDotNet.Engines;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [DisassemblyDiagnoser]
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, launchCount: 1, iterationCount: 5, warmupCount: 5)]
    [GcServer(true)]
    [GcForce(true)]
    public class GetBenchmark
    {
        #region Fields

        private DenseMap<uint, uint> _denseMap_Default;
        private DenseMap<uint, uint> _denseMap_Xxhash3;
        private DenseMap<uint, uint> _denseMap_GxHash;
        private DenseMap<uint, uint> _denseMap_fastHash;
        private Dictionary<uint, uint> _dictionary;
        private RobinhoodMap<uint, uint> _robinHoodMap;

        private uint[] keys;

        #endregion

        #region Properties

        [Params(80_000_000)]
        public uint Length { get; set; }

        #endregion

        /// <summary>
        /// Generate a million Keys and shuffle them afterwards
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {          
            var rnd = new Random(3);
           
            var uni = new HashSet<uint>();

            while (uni.Count < Length)
            {
                uni.Add((uint)rnd.NextInt64());
            }

            keys = uni.ToArray();

            // round of length to power of 2 prevent resizing
            uint length = BitOperations.RoundUpToPowerOf2(Length);
            int dicLength = HashHelpers.GetPrime((int)Length);

            _denseMap_Default = new DenseMap<uint, uint>(length);
            _denseMap_Xxhash3 = new DenseMap<uint, uint>(length, 0.875, new XxHash3Hasher<uint>());
            _denseMap_fastHash = new DenseMap<uint, uint>(length, 0.875, new FastHasherUint());

            _dictionary = new Dictionary<uint, uint>(dicLength);
            _robinHoodMap = new RobinhoodMap<uint, uint>(length * 2);

            foreach (var key in keys)
            {
               // _dictionary.Add(key, key);
                _denseMap_Default.Emplace(key, key);
                //_denseMap_Xxhash3.Emplace(key, key);
                //_denseMap_fastHash.Emplace(key, key);
                //_denseMap_GxHash.Emplace(key, key);
                //_robinHoodMap.Emplace(key, key);
            }
        }

        [Benchmark]
        public void DenseMap()
        {
            for (int i = 0; i < Length; ++i)
            {
                var key = keys[i];
                _denseMap_Default.Get(key, out var _);
            }
        }


        //[Benchmark(Baseline = true)]
        //public void Dictionary()
        //{
        //    for (int i = 0; i < Length; ++i)
        //    {
        //        var key = keys[i];
        //        _dictionary.TryGetValue(key, out var _);
        //    }
        //}


        //[Benchmark]
        //public void DenseMap_XXhash3()
        //{
        //    for (int i = 0; i < Length; ++i)
        //    {
        //        var key = keys[i];
        //        _denseMap_Xxhash3.Get(key, out var _);
        //    }
        //}

        //[Benchmark]
        //public void DenseMap_FastHash()
        //{
        //    for (int i = 0; i < Length; ++i)
        //    {
        //        var key = keys[i];
        //        _denseMap_fastHash.Get(key, out var _);              
        //    }
        //}

        //[Benchmark]
        //public void DenseMap_GxHash()
        //{
        //    for (int i = 0; i < Length; ++i)
        //    {
        //        var key = keys[i];
        //        _denseMap_GxHash.Get(key, out var _);
        //    }
        //}

        //[Benchmark]
        //public void RobinhoodMap()
        //{
        //    for (int i = 0; i < Length; ++i)
        //    {
        //        var key = keys[i];
        //        _robinHoodMap.Get(key, out var _);
        //    }
        //}

 
    }
}