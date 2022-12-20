using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Collections.Extensions;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    //[SimpleJob(RunStrategy.Monitoring, 1, 10, 50)]
    public class RemoveBenchmark
    {
        #region Fields

        FastMap<uint, uint> _fastMap = new FastMap<uint, uint>(16, 0.5);
        private DenseMap<uint, uint> _denseMap = new DenseMap<uint, uint>(16);
        private DenseMap<uint, uint> _dense = new DenseMap<uint, uint>(16, 0.5);
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

            foreach (var key in keys)
            {
                dic.Add(key, key);
                _denseMap.Emplace(key, key);       
                _fastMap.Emplace(key, key);
                _dense.Emplace(key, key);
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
        public void SlimDictionary()
        {
            foreach (var key in keys)
            {
                _slim.Remove(key);
            }
        }

        [Benchmark]
        public void Dictionary()
        {
            foreach (var key in keys)
            {
                dic.Remove(key, out var result);
            }
        }

        [Benchmark]
        public void FastMap()
        {
            foreach (var key in keys)
            {
                _fastMap.Remove(key);
            }
        }

        [Benchmark]
        public void Map()
        {
            foreach (var key in keys)
            {
                _denseMap.Remove(key);
            }
        }

 
        #endregion



    }
}
