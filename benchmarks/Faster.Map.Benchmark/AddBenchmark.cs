using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
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
        private DenseMap<uint, uint> _dense;
        private Dictionary<uint, uint> dic;
        private RobinhoodMap<uint, uint> _robinhoodMap;
    
        private uint[] keys;

        #endregion

        #region Properties

        [Params(10000, 100000, 400000, 800000, 900000)]
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
            uint length = BitOperations.RoundUpToPowerOf2(Length);
            int dicLength = HashHelpers.GetPrime((int)Length);

            _dense = new DenseMap<uint, uint>(length);
            dic = new Dictionary<uint, uint>(dicLength);          
            _robinhoodMap = new RobinhoodMap<uint, uint>(length);
        }

        #region Benchmarks

        [Benchmark]
        public void DenseMap()
        {
            for (int i = 0; i < Length; i++)
            {
                var key = keys[i];
                _dense.Emplace(key, key);
            } 
        }

        [Benchmark]
        public void RobinhoodMap()
        {
            for (int i = 0; i < Length; i++)
            {
                var key = keys[i];
                _robinhoodMap.Emplace(key, key);
            }
        }

        [Benchmark]
        public void Dictionary()
        {
            for (int i = 0; i < Length; i++)
            {
                var key = keys[i];
                dic.Add(key, key);
            }
        }

        #endregion
    }
}