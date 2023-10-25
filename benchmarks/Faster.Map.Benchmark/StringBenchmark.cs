using BenchmarkDotNet.Attributes;
using Faster.Map.Experimental;
using Microsoft.Collections.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Faster.Map.Benchmark
{
    public class StringBenchmark
    {
        #region Fields

        private QuadMap<string, string> _dense = new QuadMap<string, string>();
        private DenseMapSIMD<string, string> _denseMapSIMD = new DenseMapSIMD<string, string>();

        private Dictionary<string, string> dic = new Dictionary<string, string>();
        private DictionarySlim<string, string> _slim = new DictionarySlim<string, string>();

        private string[] keys;
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

            keys = new string[_length];

            for (var index = 0; index < _length; index++)
            {
                keys[index] = splittedOutput[index];
            }

            foreach (var key in keys)
            {
                dic.Add(key, key);
                _denseMapSIMD.Emplace(key, key);
                _dense.Emplace(key, key);
                _slim.GetOrAddValueRef(key);
            }

            //    Shuffle(new Random(), keys);
        }




        [Benchmark]
        public void DenseMapSIMD()
        {
            foreach (var key in keys)
            {
                _denseMapSIMD.Get(key, out var result);
            }
        }

        [Benchmark]
        public void DenseMap()
        {
            foreach (var key in keys)
            {
                _dense.Get(key, out var result);
            }

        }

        [Benchmark]
        public void Dictionary()
        {
            foreach (var key in keys)
            {
                dic.TryGetValue(key, out var result);
            }
        }

        [Benchmark]
        public void SlimDictionary()
        {
            foreach (var key in keys)
            {
                _slim.TryGetValue(key, out var result);
            }
        }

    }
}
