using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Faster.Map.Experimental;
using Microsoft.Collections.Extensions;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    public class UpdateBenchmark
    {
        #region Fields

        FastMap<uint, uint> _fastMap = new FastMap<uint, uint>(16, 0.5);
        private DenseMapSIMD<uint, uint> _denseMapSIMD = new DenseMapSIMD<uint, uint>();
        private DenseMap<uint, uint> _denseMap = new DenseMap<uint, uint>();

        private Dictionary<uint, uint> dic = new Dictionary<uint, uint>();
        private DictionarySlim<uint, uint> _slim = new DictionarySlim<uint, uint>();


        private uint[] keys;
        private int _length = 900000;
        #endregion

        /// <summary>
        /// Generate a million Keys and shuffle them afterwards
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            var output = File.ReadAllText("Numbers.txt");
            var splittedOutput = output.Split(',');

            keys = new uint[_length];

            for (var index = 0; index < _length; index++)
            {
                keys[index] = uint.Parse(splittedOutput[index]);
            }

            foreach (var key in keys)
            {
                dic.Add(key, key);
                _denseMapSIMD.Emplace(key, key);
                _denseMap.Emplace(key, key);
                _fastMap.Emplace(key, key);
                _slim.GetOrAddValueRef(key);
            }

            // Shuffle(new Random(), keys);
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
        public void Dictionary()
        {
            foreach (var key in keys)
            {
                dic[key] = 222;
            }
        }

        [Benchmark]
        public void DenseMapSIMD()
        {
            foreach (var key in keys)
            {
                _denseMapSIMD.Update(key, 222);
            }
        }

        [Benchmark]
        public void FastMap()
        {
            foreach (var key in keys)
            {
                _fastMap.Update(key, 222);
            }
        }

        [Benchmark]
        public void DenseMap()
        {
            for (uint i = 0; i < _length; i++)
            {
                _denseMap.Update(keys[i], 222);
            }
        }

        #endregion
    }
}