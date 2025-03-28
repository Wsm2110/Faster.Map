﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Faster.Map.Benchmark.Utilities;
using Faster.Map.Hasher;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, launchCount: 1, iterationCount: 5, warmupCount: 3)]

    public class UpdateBenchmark
    {
        #region Fields

        private DenseMap<uint, uint> _denseMap;
        private BlitzMap<uint, uint> _blitz;
        private Dictionary<uint, uint> _dictionary;
        private RobinhoodMap<uint, uint> _robinHoodMap;

        private uint[] keys;

        #endregion

        #region Properties

        [Params(0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8)]
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

            _denseMap = new DenseMap<uint, uint>(length);
            _blitz = new BlitzMap<uint, uint>((int)length, 0.9);

            _dictionary = new Dictionary<uint, uint>(dicLength);
            _robinHoodMap = new RobinhoodMap<uint, uint>(length, 0.9);

            foreach (var key in keys)
            {
                _dictionary.Add(key, key);
                _denseMap.Emplace(key, key);
                _blitz.Insert(key, key);
                _robinHoodMap.Emplace(key, key);
            }
        }

        #region Benchmarks

        [Benchmark]
        public void BlitzMap()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                _blitz.Get(key, out var _);
            }
        }

        [Benchmark]
        public void DenseMap()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];                
                _denseMap.Update(key, 222);
            }
        }

        [Benchmark]
        public void RobinhoodMap()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                _robinHoodMap.Update(key, 222);
            }
        } 

        [Benchmark]
        public void Dictionary()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                _dictionary[key] = 222;
            }
        }

        #endregion
    }
}