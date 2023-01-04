﻿using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.Collections.Extensions;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    // [SimpleJob(RunStrategy.Monitoring, 1, 10, 50)]
    public class UpdateBenchmark
    {
        #region Fields

        FastMap<uint, uint> _fastMap = new FastMap<uint, uint>(16, 0.5);
        private DenseMapSIMD<uint, uint> _denseMap = new DenseMapSIMD<uint, uint>(16);
        private DenseMap<uint, uint> _dense = new DenseMap<uint, uint>(16);

        private Dictionary<uint, uint> dic = new Dictionary<uint, uint>();
        private Dictionary<uint, uint> dic2 = new Dictionary<uint, uint>();
        private DictionarySlim<uint, uint> _slim = new DictionarySlim<uint, uint>();
    

        private uint[] keys;
        #endregion

        /// <summary>
        /// Generate a million Keys and shuffle them afterwards
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            var output = File.ReadAllText("Numbers.txt");
            var splittedOutput = output.Split(',');

            keys = new uint[splittedOutput.Length];

            for (var index = 0; index < splittedOutput.Length; index++)
            {
                keys[index] = uint.Parse(splittedOutput[index]);
            }

            foreach (var key in keys)
            {
                dic.Add(key, key);
                _denseMap.Emplace(key, key);     
                _fastMap.Emplace(key, key);
                _slim.GetOrAddValueRef(key);
            }

            Shuffle(new Random(), keys);
        }

        private static void Shuffle<T>(Random rng, T[] a)
        {
            int n = a.Length;
            while (n > 1)
            {
                int k = rng.Next(--n);
                T temp = a[n];
                a[n] = a[k];
                a[k] = temp;
            }

        }

        #region Benchmarks

        [Benchmark]
        public void UpdateSlim()
        {
            foreach (var key in keys)
            {
                _slim.GetOrAddValueRef(key) = 222;
            }
        }

        [Benchmark]
        public void UpdateDictionary()
        {
            foreach (var key in keys)
            {
                dic[key] = 222;
            }
        }

        [Benchmark]
        public void UpdateFastMap()
        {
            foreach (var key in keys)
            {
                _fastMap.Update(key, 222);
            }
        }

        [Benchmark]
        public void UpdateDenseMap()
        {
            foreach (var key in keys)
            {
                _dense.Update(key, 222);
            }
        }

  
        #endregion


    }
}