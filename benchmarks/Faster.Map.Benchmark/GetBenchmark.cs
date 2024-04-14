using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using System.IO;
using Faster.Map.QuadMap;
using Faster.Map.DenseMap;
using Faster.Map.RobinhoodMap;
using System;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [DisassemblyDiagnoser]
    [MemoryDiagnoser]
    public class GetBenchmark
    {
        #region Fields

        private DenseMap<uint, uint> _denseMap = new DenseMap<uint, uint>();
        private Dictionary<uint, uint> _dictionary = new Dictionary<uint, uint>();
        private RobinhoodMap<uint, uint> _robinHoodMap = new();
        private QuadMap<uint, uint> _quadMap = new();

        private uint[] keys;

        #endregion

        #region Properties

        [Params(1, 10, 100, 1000, 10000, 100000, 1000000)]
        public int Length { get; set; }

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

            foreach (var key in keys)
            {
                _dictionary.Add(key, key);
                _denseMap.Emplace(key, key);
                _robinHoodMap.Emplace(key, key);
                _quadMap.Emplace(key, key);
            }
        }

        [Benchmark]
        public void DenseMap()
        {
            for (int i = 0; i < Length; ++i)
            {
                var key = keys[i];
                _denseMap.Get(key, out var _);
            }
        }

        [Benchmark]
        public void RobinhoodMap()
        {
            for (int i = 0; i < Length; ++i)
            {
                var key = keys[i];
                _robinHoodMap.Get(key, out var _);
            }
        }

        [Benchmark]
        public void QuadMap()
        {
            for (int i = 0; i < Length; ++i)
            {
                var key = keys[i];
                _quadMap.Get(key, out var _);
            }
        }

        [Benchmark(Baseline = true)]
        public void Dictionary()
        {
            for (int i = 0; i < Length; ++i)
            {
                var key = keys[i];
                _dictionary.TryGetValue(key, out var _);
            }
        }
    }
}