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
    [MarkdownExporterAttribute.GitHub]
    public class AddBenchmark
    {
        #region Fields

        //fixed size, dont want to measure resize()
        FastMap<uint, uint> _fastMap = new FastMap<uint, uint>(2000000); // remove resizing from benchmark hence the 2000000 size
        DenseMap<uint, uint> _dense = new DenseMap<uint, uint>(2000000);
        DenseMapSIMD<uint, uint> _denseMap = new DenseMapSIMD<uint, uint>(1000000, 0.9);
        private Dictionary<uint, uint> dic = new Dictionary<uint, uint>(2000000);
        private DictionarySlim<uint, uint> _slim = new DictionarySlim<uint, uint>(1000000);
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

            keys = keys.Take(899999).ToArray();      
        }

        [IterationCleanup]
        public void FinalizeBenchmark()
        {
            _denseMap = new DenseMapSIMD<uint, uint>(1000000, 0.90); // wont resize
            _dense = new DenseMap<uint, uint>(2000000); 
            dic = new Dictionary<uint, uint>(20000000);
            _slim = new DictionarySlim<uint, uint>(1000000);
            _fastMap = new FastMap<uint, uint>(1000000);

            Shuffle(new Random(), keys);
        }

        #region Benchmarks

        [Benchmark]
        public void DenseMapSIMD()
        {
            foreach (var key in keys)
            {
                _denseMap.Emplace(key, key);
            }
        }

        [Benchmark]
        public void DenseMap()
        {
            foreach (var key in keys)
            {
                _dense.Emplace(key, key);
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
        public void Dictionary()
        {
            foreach (var key in keys)
            {
                dic[key] = key;
            }
        }

        [Benchmark]
        public void DictionarySlim()
        {
            foreach (var key in keys)
            {
                _slim.GetOrAddValueRef(key);
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
