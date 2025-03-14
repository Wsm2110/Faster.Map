using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Faster.Map.Benchmark.Utilities;
using Faster.Map.Hash;
using Faster.Map.Hasher;
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

    public class StringCustomHasherBenchmark
    {
        #region Fields

        ////fixed size, dont want to measure resize()
        private Dictionary<string, string> _dictionary;
        private BlitzMap<string, string> _blitzMap;
        private BlitzMap<string, string, XxHash3StringHasher> _blitzMap1;
        private BlitzMap<string, string, FastHasherString> _blitzMap2;
        private BlitzMap<string, string, WyHasher> _blitzMap3;

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
            var rnd = new FastRandom(3);
            var uni = new HashSet<string>((int)Length);
            while (uni.Count < (uint)(Length * LoadFactor))
            {
                uni.Add(rnd.Next().ToString());
            }

            keys = uni.ToArray();

            // round of length to power of 2 prevent resizing
            uint length = BitOperations.RoundUpToPowerOf2(Length);
            int dicLength = HashHelpers.GetPrime((int)Length);

            _blitzMap = new BlitzMap<string, string>((int)length, 0.8);
            _blitzMap1 = new BlitzMap<string, string, XxHash3StringHasher>((int)length, 0.8);
            _blitzMap2 = new BlitzMap<string, string, FastHasherString>((int)length, 0.8);
            _blitzMap3 = new BlitzMap<string, string, WyHasher>((int)length, 0.8);


            _dictionary = new Dictionary<string, string>(dicLength);

            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                _dictionary.Add(key, key);        
                _blitzMap.Insert(key, key);
                _blitzMap1.Insert(key, key);
                _blitzMap2.Insert(key, key);
                _blitzMap3.Insert(key, key);
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
        public void BlitzMapXX3()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                _blitzMap1.Get(key, out var _);
            }
        }

        [Benchmark]
        public void BlitzMapFastHash()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                _blitzMap2.Get(key, out var _);
            }
        }

        [Benchmark]
        public void BlitzMapWyHash()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                _blitzMap3.Get(key, out var _);
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
    }
}
