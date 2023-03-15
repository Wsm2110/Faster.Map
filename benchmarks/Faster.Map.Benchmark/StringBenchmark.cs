using BenchmarkDotNet.Attributes;
using Microsoft.Collections.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Faster.Map.Core;

namespace Faster.Map.Benchmark
{
    public class StringBenchmark
    {
        #region Fields

        private DenseMap<string, string> _dense = new DenseMap<string, string>();
        private DenseMapSIMD<string, string> _denseMap = new DenseMapSIMD<string, string>();

        private Dictionary<string, string> dic = new Dictionary<string, string>();
        private DictionarySlim<string, string> _slim = new DictionarySlim<string, string>();

        private string[] keys;

        #endregion

        /// <summary>
        /// Generate a million Keys and shuffle them afterwards
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            var output = File.ReadAllText("Numbers.txt");
            var splittedOutput = output.Split(',');

            keys = new string[splittedOutput.Length];

            for (var index = 0; index < splittedOutput.Length; index++)
            {
                keys[index] = splittedOutput[index];
            }

            foreach (var key in keys.Take(900000))
            {
                dic.Add(key, key);
                _denseMap.Emplace(key, key);
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
                _denseMap.Get(key, out var result);
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
