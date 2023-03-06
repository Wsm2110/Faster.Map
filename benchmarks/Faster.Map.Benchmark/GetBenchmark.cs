using System;
using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Collections.Extensions;

namespace Faster.Map.Benchmark
{

    [MarkdownExporterAttribute.GitHub]
    // [SimpleJob(RunStrategy.Monitoring, 1, 10, 50)]
    public class GetBenchmark
    {
        #region Fields

        FastMap<uint, uint> _fastMap = new FastMap<uint, uint>();
        DenseMap<uint, uint> _dense = new DenseMap<uint, uint>();

        private DenseMapSIMD<uint, uint> _denseMapSIMD = new DenseMapSIMD<uint, uint>();

        private Dictionary<uint, uint> dic = new Dictionary<uint, uint>();
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

            foreach (var key in keys.Take(900000))
            {
                dic.Add(key, key);
                _denseMapSIMD.Emplace(key, key);
                _dense.Emplace(key, key);
                _fastMap.Emplace(key, key);
                _slim.GetOrAddValueRef(key);
            }

            //    Shuffle(new Random(), keys);
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

        [Benchmark]
        public void DenseMapSIMD()
        {
            foreach (var key in keys)
            {
                _denseMapSIMD.Get(key, out var result);
            }
        }

        //[Benchmark]
        //public void DenseMap()
        //{
        //    foreach (var key in keys)
        //    {
        //        _dense.Get(key, out var result);
        //    }
        //  }

        //[Benchmark]
        //public void FastMap()
        //{
        //    foreach (var key in keys)
        //    {
        //        _fastMap.Get(key, out var result);
        //    }
        //}

        //[Benchmark]
        //public void SlimDictionary()
        //{
        //    foreach (var key in keys)
        //    {
        //        _slim.GetOrAddValueRef(key);
        //    }
        //}

        //[Benchmark]
        //public void Dictionary()
        //{
        //    foreach (var key in keys)
        //    {
        //        dic.TryGetValue(key, out var result);
        //    }
        //}

    }
}