using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.Collections.Extensions;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [DisassemblyDiagnoser]
    [MemoryDiagnoser]
    public class AddBenchmark
    {
        #region Fields

        //fixed size, dont want to measure resize()
        DenseMap<uint, uint> _dense = new DenseMap<uint, uint>(1000000);
        private Dictionary<uint, uint> dic = new Dictionary<uint, uint>(1000000);
        private DictionarySlim<uint, uint> _slim = new DictionarySlim<uint, uint>(1000000);
        private uint[] keys;
        private uint _length = 900000;

        #endregion

        /// <summary>
        /// Generate a million Keys and shuffle them afterwards
        /// </summary>
        [GlobalSetup]
        public void Add()
        {
            // System.Diagnostics.Debugger.Launch();

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
            _dense.Clear();
            dic.Clear();
            _slim.Clear();         
        }

        #region Benchmarks
            

        [Benchmark]
        public void DenseMap()
        {
            foreach (var key in keys)
            {
                _dense.Emplace(key, key);
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
