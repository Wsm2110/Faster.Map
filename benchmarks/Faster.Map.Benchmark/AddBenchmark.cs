using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Collections.Extensions;

namespace Faster.Map.Benchmark
{
    //[SimpleJob(RunStrategy.Monitoring, 1, 10, 50)]
    public class AddBenchmark
    {
        #region Fields
  
        FastMap<uint, uint> _fastMap = new FastMap<uint, uint>(16, 0.5);
        private DenseMap<uint, uint> _denseMap = new DenseMap<uint, uint>(16);
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

           // Shuffle(new Random(), keys);
        }

        #region Benchmarks

        [Benchmark]
        public void DictionarySlim()
        {
            foreach (var key in keys)
            {
                _slim.GetOrAddValueRef(key);
            }
        }

        [Benchmark]
        public void Dictionary()
        {
            foreach (var key in keys)
            {
                dic[key] = key;
            }
        }

        [Benchmark]
        public void FastMap()
        {
            foreach (var key in keys)
            {
                _fastMap.Emplace(key, key);
            }
        }

        [Benchmark]
        public void DenseMap()
        {
            foreach (var key in keys)
            {
                _denseMap.Emplace(key, key);
            }
        }

        #endregion


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


    }
}
