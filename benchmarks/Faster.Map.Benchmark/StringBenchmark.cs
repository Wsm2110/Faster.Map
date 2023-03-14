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

        private DenseMap<StringWrapper, string> _dense = new DenseMap<StringWrapper, string>();
        private DenseMapSIMD<StringWrapper, string> _denseMap = new DenseMapSIMD<StringWrapper, string>();

        private Dictionary<StringWrapper, string> dic = new Dictionary<StringWrapper, string>();
        private DictionarySlim<StringWrapper, string> _slim = new DictionarySlim<StringWrapper, string>();
       
        private StringWrapper[] keys;

        #endregion

        /// <summary>
        /// Generate a million Keys and shuffle them afterwards
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            var output = File.ReadAllText("Numbers.txt");
            var splittedOutput = output.Split(',');

            keys = new StringWrapper[splittedOutput.Length];

            for (var index = 0; index < splittedOutput.Length; index++)
            {
                keys[index] = new StringWrapper(splittedOutput[index]);
            }

            foreach (var key in keys.Take(900000))
            {
                dic.Add(key, key.Value);
                _denseMap.Emplace(key, key.Value);
                _dense.Emplace(key, key.Value);             
                _slim.GetOrAddValueRef(key);            }

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

    }
}
