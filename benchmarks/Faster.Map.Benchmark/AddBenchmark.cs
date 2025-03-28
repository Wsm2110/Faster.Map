﻿using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using System.Linq;
using System.Numerics;
using System.Collections;
using BenchmarkDotNet.Engines;
using Faster.Map.Benchmark.Utilities;
using Faster.Map.Hasher;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [SimpleJob(RunStrategy.Monitoring, launchCount: 1, iterationCount: 5, warmupCount: 3)]

    public class AddBenchmark
    {
        #region Fields

        ////fixed size, dont want to measure resize()
        private DenseMap<uint, uint> _dense;
        private Dictionary<uint, uint> dic;
        private RobinhoodMap<uint, uint> _robinhoodMap;
        private BlitzMap<uint, uint> blitzMap;

        private uint[] keys;

        #endregion

        #region Properties

        [Params(0.1, 0.2, 0.3, 0.4, 0.5, 0.6 ,0.7, 0.8)]
        public static double LoadFactor { get; set; }

        [Params(134_217_728)]
        public static uint Length { get; set; }

        #endregion

        /// <summary>
        /// Generate a million Keys and shuffle them afterwards
        /// </summary>
        [GlobalSetup]
        public void Add()
        {
            var rnd = new FastRandom(3);
            var uni = new HashSet<uint>((int)Length);
            while (uni.Count < (uint)(Length * LoadFactor))
            {
                uni.Add((uint)rnd.Next());
            }

            keys = uni.ToArray();
        }

        [IterationSetup]
        public void Setup()
        {
            // round of length to power of 2 prevent resizing
            uint length = BitOperations.RoundUpToPowerOf2(Length);
            int dicLength = HashHelpers.GetPrime((int)(Length));

            blitzMap = new BlitzMap<uint, uint>((int)length, 0.81);
            _dense = new DenseMap<uint, uint>(length);
            dic = new Dictionary<uint, uint>(dicLength);
            _robinhoodMap = new RobinhoodMap<uint, uint>(length, 0.81);
        }

        #region Benchmarks

        [Benchmark]
        public void BlitzMap()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                blitzMap.Insert(key, key);
            }
        }

        [Benchmark]
        public void DenseMap()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                _dense.Emplace(key, key);
            }
        }


        [Benchmark]
        public void RobinhoodMap()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                _robinhoodMap.Emplace(key, key);
            }
        }

        [Benchmark]
        public void Dictionary()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                dic.Add(key, key);
            }
        }

        #endregion
    }
}