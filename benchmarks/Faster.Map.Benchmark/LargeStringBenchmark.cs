using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Faster.Map.Benchmark.Utilities;
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
    [SimpleJob(RunStrategy.Monitoring, launchCount: 1, iterationCount: 5, warmupCount: 3)]

    public class largeStringBenchmark
    {
        #region Fields

        ////fixed size, dont want to measure resize()
        private DenseMap<string, string> _dense;
        private Dictionary<string, string> _dictionary;
        private RobinhoodMap<string, string> _robinhoodMap;
        private BlitzMap<string, string> _blitzMap;

        private string[] keys;

        #endregion

        #region Properties

        [Params(0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8)]
        public double LoadFactor { get; set; }

        [Params(16_777_216)]
        public uint Length { get; set; }

        #endregion

        /// <summary>
        /// Generate a million Keys and shuffle them afterwards
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()-_=+";

            Random random = new Random();

            var rnd = new FastRandom(3);
            var uni = new HashSet<string>((int)Length);
            while (uni.Count < (uint)(Length * LoadFactor))
            {
                string s = new string(Enumerable.Repeat(chars, 100)
               .Select(s => s[random.Next(s.Length)]).ToArray());
                uni.Add(s + rnd.Next().ToString());
            }

            keys = uni.ToArray();

            // round of length to power of 2 prevent resizing
            uint length = BitOperations.RoundUpToPowerOf2(Length);
            int dicLength = HashHelpers.GetPrime((int)Length);

            _dense = new DenseMap<string, string>(length, 0.875);
            _blitzMap = new BlitzMap<string, string>((int)length, 0.8);
            _dictionary = new Dictionary<string, string>(dicLength);
            _robinhoodMap = new RobinhoodMap<string, string>(length * 2);

            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                _dictionary.Add(key, key);
                _dense.Emplace(key, key);
                _robinhoodMap.Emplace(key, key);
                _blitzMap.Insert(key, key);
            }
        }

        [Benchmark]
        public void BlitzMap()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                _blitzMap.Get(key, out var _);
            }
        }

        [Benchmark]
        public void DenseMap()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                _dense.Get(key, out var _);
            }
        }

        [Benchmark]
        public void Dictionary()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                _dictionary.TryGetValue(key, out var _);
            }
        }

        [Benchmark]
        public void RobinhoodMap()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                _robinhoodMap.Get(key, out var _);
            }
        }

    }
}
