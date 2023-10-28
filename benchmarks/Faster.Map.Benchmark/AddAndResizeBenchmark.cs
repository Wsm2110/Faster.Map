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
    [MarkdownExporterAttribute.GitHub]
    public class AddAndResizeBenchmark
    {
        #region Fields

        DenseMap<uint, uint> _denseMap = new DenseMap<uint, uint>();
        private Dictionary<uint, uint> dic = new Dictionary<uint, uint>();
        private DictionarySlim<uint, uint> _slim = new DictionarySlim<uint, uint>();
        private uint[] keys;
        private uint _length = 900000;

        #endregion

        /// <summary>
        /// Generate a million Keys and shuffle them afterwards
        /// </summary>
        [GlobalSetup]
        public void Add()
        {
            var output = File.ReadAllText("Numbers.txt");
            var splittedOutput = output.Split(',');

            keys = new uint[_length];

            for (var index = 0; index < _length; index++)
            {
                keys[index] = uint.Parse(splittedOutput[index]);
            }

            //  Shuffle(new Random(), keys);
        }

        [IterationCleanup]
        public void ResetMaps()
        {
            _denseMap = new DenseMap<uint, uint>();
            dic = new Dictionary<uint, uint>();
            _slim = new DictionarySlim<uint, uint>();
        }

        #region Benchmarks


        [Benchmark]
        public void DenseMap()
        {
            foreach (var key in keys)
            {
                _denseMap.Emplace(key, key);
            }
        }   

        [Benchmark]
        public void Dictionary()
        {
            foreach (var key in keys)
            {
                dic.Add(key, key);
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