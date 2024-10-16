﻿using BenchmarkDotNet.Attributes;
using Faster.Map.Core;
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
    public class StringWrapperBenchmark
    {
        #region Fields

        private DenseMap<StringWrapper, StringWrapper> _denseMap;
        private Dictionary<StringWrapper, StringWrapper> _dictionary;
        private RobinhoodMap<StringWrapper, StringWrapper> _robinhoodMap;

        private string[] keys;

        #endregion

        #region Properties

        [Params(1000, 10000, 100000, 400000, 900000, 1000000)]
        public uint Length { get; set; }

        #endregion

        /// <summary>
        /// Generate a million Keys and shuffle them afterwards
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            var output = File.ReadAllText("Numbers.txt");
            var splittedOutput = output.Split(',');

            keys = new string[Length];

            for (var index = 0; index < Length; index++)
            {
                keys[index] = splittedOutput[index];
            }

            // round of length to power of 2 prevent resizing
            uint length = BitOperations.RoundUpToPowerOf2(Length) * 2;
            int dicLength = HashHelpers.GetPrime((int)Length);

            _denseMap = new DenseMap<StringWrapper, StringWrapper>(length);
            _dictionary = new Dictionary<StringWrapper, StringWrapper>(dicLength);
            _robinhoodMap = new RobinhoodMap<StringWrapper, StringWrapper>(length);

            foreach (var key in keys)
            {
                _dictionary.Add(key, key);
                _denseMap.Emplace(key, key);
                _robinhoodMap.Emplace(key, key);
            }
        }

        [Benchmark]
        public void DenseMap()
        {
            foreach (var key in keys)
            {
                _denseMap.Get(key, out var result);
            }
        }

        [Benchmark]
        public void RobinhoodMap()
        {
            foreach (var key in keys)
            {
                _robinhoodMap.Get(key, out var result);
            }
        }

        [Benchmark]
        public void Dictionary()
        {
            foreach (var key in keys)
            {
                _dictionary.TryGetValue(key, out var result);
            }
        }

    }
}
