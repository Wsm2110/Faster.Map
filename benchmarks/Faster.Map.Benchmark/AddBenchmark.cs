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
    public class AddBenchmark
    {
        #region Fields

        //fixed size, dont want to measure resize()
        FastMap<uint, uint> _fastMap = new FastMap<uint, uint>(); // remove resizing from benchmark hence the 2000000 size
        DenseMap<uint, uint> _dense = new DenseMap<uint, uint>();
        DenseMapSIMD<uint, uint> _denseMap = new DenseMapSIMD<uint, uint>();
        private Dictionary<uint, uint> dic = new Dictionary<uint, uint>();
        private DictionarySlim<uint, uint> _slim = new DictionarySlim<uint, uint>();
        private uint[] keys;

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

            keys = new uint[splittedOutput.Length];

            for (var index = 0; index < splittedOutput.Length; index++)
            {
                keys[index] = uint.Parse(splittedOutput[index]);
            }

            keys = keys.Take(900000).ToArray();
            //  Shuffle(new Random(), keys);
        }

        [IterationCleanup]
        public void ResetMaps()
        {
            _denseMap.Clear();
            _dense.Clear();
            dic.Clear();
            _slim.Clear();
            _fastMap.Clear();
        }

        #region Benchmarks

        //[Benchmark]
        //public void DenseMapSIMD()
        //{
        //    foreach (uint key in keys)
        //    {
        //        _denseMapSIMD.Emplace(key, key);
        //    }
        //}

        [Benchmark]
        public void DenseMap()
        {
            foreach (var key in keys)
            {
                var result = _dense.Emplace(key, key);

                if (result == false)
                {
                    throw new InvalidProgramException("fail");
                }
            }
        }

        //[Benchmark]
        //public void FastMap()
        //{
        //    foreach (var key in keys)
        //    {
        //        _fastMap.Emplace(key, key);
        //    }
        //}

        //[Benchmark]
        //public void Dictionary()
        //{
        //    foreach (var key in keys)
        //    {
        //        dic.Add(key, key);
        //    }
        //}

        //[Benchmark]
        //public void DictionarySlim()
        //{
        //    foreach (var key in keys)
        //    {
        //        _slim.GetOrAddValueRef(key);
        //    }
        //}

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
