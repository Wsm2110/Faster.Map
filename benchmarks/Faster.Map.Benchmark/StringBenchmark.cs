using BenchmarkDotNet.Attributes;
using Microsoft.Collections.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Map.Benchmark
{ 

    public class StringBenchmark
    {

        #region Fields


        DenseMap<string, string> _dense = new DenseMap<string, string>();

        private DenseMapSIMD<StringWrapper, string> _denseMap = new DenseMapSIMD<StringWrapper, string>();

        private Dictionary<string, string> dic = new Dictionary<string, string>();
        private DictionarySlim<string, string> _slim = new DictionarySlim<string, string>();

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
                dic.Add(key.Value, key.Value);
                _denseMap.Emplace(key, key.Value);
                _dense.Emplace(key.Value, key.Value);
                //  _fastMap.Emplace(key.ToString(), key.ToString());
                _slim.GetOrAddValueRef(key.Value);
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
                _dense.Get(key.ToString(), out var result);
            }

        }

        [Benchmark]
        public void Dictionary()
        {
            foreach (var key in keys)
            {
                dic.TryGetValue(key.ToString(), out var result);
            }
        }

    }
}
