using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using Faster.Map.DenseMap;
using Faster.Map.RobinHoodMap;
using Faster.Map.QuadMap;
using System.Collections.Concurrent;
using System.Linq;
using System.Numerics;

using System.Collections;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    public class AddBenchmark
    {
        #region Fields

        //fixed size, dont want to measure resize()
        private DenseMap<uint, uint> _dense = new DenseMap<uint, uint>(1000000);
        private Dictionary<uint, uint> dic = new Dictionary<uint, uint>(10000000);
        private RobinhoodMap<uint, uint> _robinhoodMap = new RobinhoodMap<uint, uint>(2000000);
        private QuadMap<uint, uint> _quadmap = new QuadMap<uint, uint>(2000000);

        private uint[] keys;

        #endregion

        #region Properties

        [Params(100000, 500000, 900000, 1000000)]
        public uint Length { get; set; }

        #endregion

        /// <summary>
        /// Generate a million Keys and shuffle them afterwards
        /// </summary>
        [GlobalSetup]
        public void Add()
        {
            var output = File.ReadAllText("Numbers.txt");
            var splittedOutput = output.Split(',');

            keys = new uint[1000000];

            for (var index = 0; index < Length; index++)
            {
                keys[index] = uint.Parse(splittedOutput[index]);
            }
        }

        [IterationSetup]
        public void Setup()
        {
            // round of length to power of 2 prevent resizing
            uint length = BitOperations.RoundUpToPowerOf2(Length) * 2;
            int dicLength = HashHelpers.GetPrime((int)Length);

            _dense = new DenseMap<uint, uint>(length);
            dic = new Dictionary<uint, uint>(dicLength);
            _quadmap = new QuadMap<uint, uint>(length);
            _robinhoodMap = new RobinhoodMap<uint, uint>(length);
        }

        #region Benchmarks

        [Benchmark]
        public void DenseMap()
        {
            foreach (var key in keys.Take((int)Length))
            {
                _dense.Emplace(key, key);
            }
        }

        [Benchmark]
        public void RobinhoodMap()
        {
            foreach (var key in keys.Take((int)Length))
            {
                _robinhoodMap.Emplace(key, key);
            }
        }

        [Benchmark]
        public void QuadMap()
        {
            foreach (var key in keys.Take((int)Length))
            {

                _quadmap.Emplace(key, key);
            }
        }

        [Benchmark]
        public void Dictionary()
        {
            foreach (var key in keys.Take((int)Length))
            {
                dic.Add(key, key);
            }
        }

        #endregion
    }
}