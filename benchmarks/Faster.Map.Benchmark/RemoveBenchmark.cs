using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using Faster.Map.Benchmark.Utilities;
using Faster.Map.Core;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, launchCount: 1, iterationCount: 3, warmupCount: 2)]

    public class RemoveBenchmark
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
            _blitz = new BlitzMap<uint, uint>((int)length);

            _dictionary = new Dictionary<uint, uint>(dicLength);
            _robinHoodMap = new RobinhoodMap<uint, uint>(length, 0.9);
      
        }

        [IterationSetup]
        public void IterationSetupX(BenchmarkCase benchmarkCase)
        {
            // round of length to power of 2 prevent resizing
            uint length = BitOperations.RoundUpToPowerOf2(Length);
            int dicLength = HashHelpers.GetPrime((int)Length);

            string benchmarkName = benchmarkCase.Descriptor.WorkloadMethod.Name; // Use Name instead of DisplayInfo


            _denseMap = new DenseMap<uint, uint>(length);
            _dictionary = new Dictionary<uint, uint>(dicLength);
            _robinHoodMap = new RobinhoodMap<uint, uint>(length * 2);
            _blitz = new BlitzMap<uint, uint>((int)length);

            // Use a switch statement instead of multiple if-else
            foreach (var key in keys)
            {
                switch (benchmarkName)
                {
                    case nameof(BlitzMap):
                        _blitz.Insert(key, key);
                        break;
                    case nameof(DenseMap):
                        _denseMap.InsertOrUpdate(key, key);
                        break;
                    case nameof(RobinhoodMap):
                        _robinHoodMap.Emplace(key, key);
                        break;
                    case nameof(Dictionary):
                        _dictionary.Add(key, key);
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected benchmark: {benchmarkName}");
                }
            }
        }

        #region Benchmarks

        [Benchmark]
        public void BlitzMap()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                _blitz.Remove(key);
            }
        }

        [Benchmark]
        public void DenseMap()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                _denseMap.Remove(key);
            }
        }

        [Benchmark]
        public void RobinhoodMap()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                _robinHoodMap.Remove(key);
            }
        }

        [Benchmark]
        public void Dictionary()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                _dictionary.Remove(key, out var result);
            }
        }
        #endregion
    }
}