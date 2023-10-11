using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.Collections.Extensions;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    //[SimpleJob(RunStrategy.Monitoring, 1, 10, 50)]
    public class RemoveBenchmark
    {
        #region Fields

        FastMap<uint, uint> _fastMap = new FastMap<uint, uint>();
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
        [IterationSetup]
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
                _denseMapSIMD.Add(key, key);
                _fastMap.Emplace(key, key);
                _denseMap.Emplace(key, key);
                _slim.GetOrAddValueRef(key);
            }

            // Shuffle(new Random(), keys);
        }

        [IterationCleanup]
        public void clear()
        {
            dic.Clear();
            _denseMapSIMD.Clear();
            _denseMap.Clear();
            _fastMap.Clear();
            _slim.Clear();
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

        //[Benchmark]
        //public void SlimDictionary()
        //{
        //    foreach (var key in keys)
        //    {
        //        _slim.Remove(key);
        //    }
        //}

        //[Benchmark]
        //public void Dictionary()
        //{
        //    foreach (var key in keys)
        //    {
        //        dic.Remove(key, out var result);
        //    }
        //}

        //[Benchmark]
        //public void FastMap()
        //{
        //    foreach (var key in keys)
        //    {
        //        _fastMap.Remove(key);
        //    }
        //}

        [Benchmark]
        public void DenseMapSIMD()
        {
            foreach (var key in keys)
            {
                _denseMapSIMD.Remove(key);
            }
        }

        //[Benchmark]
        //public void DenseMap()
        //{
        //    foreach (var key in keys)
        //    {
        //        _denseMap.Remove(key);
        //    }
        //}

        #endregion

    }
}
