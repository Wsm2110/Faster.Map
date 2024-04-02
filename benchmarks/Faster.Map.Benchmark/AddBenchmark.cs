using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using Faster.Map.DenseMap;
using Faster.Map.RobinhoodMap;
using Faster.Map.QuadMap;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [DisassemblyDiagnoser]
    [MemoryDiagnoser]
    public class AddBenchmark
    {
        #region Fields

        //fixed size, dont want to measure resize()
        private DenseMap<uint, uint> _dense = new DenseMap<uint, uint>(1000000, 0.5);
        private Dictionary<uint, uint> dic = new Dictionary<uint, uint>(1000000);
        private RobinhoodMap<uint, uint> _robinhoodMap = new RobinhoodMap<uint, uint>(1000000);
        private QuadMap<uint, uint> _quadmap = new QuadMap<uint, uint>(1000000);

        private uint[] keys;

        #endregion

        #region Properties

        [ParamsAttribute(1, 10, 100, 1000, 10000, 100000, 1000000)]
        public int Length { get; set; }

        #endregion

        /// <summary>
        /// Generate a million Keys and shuffle them afterwards
        /// </summary>
        [GlobalSetup]
        public void Add()
        {      
            var output = File.ReadAllText("Numbers.txt");
            var splittedOutput = output.Split(',');

            keys = new uint[Length];

            for (var index = 0; index < Length; index++)
            {
                keys[index] = uint.Parse(splittedOutput[index]);
            }        
        }

        [IterationCleanup]
        public void ResetMaps()
        {
            _dense.Clear();
            dic.Clear();
            _robinhoodMap.Clear();
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
        public void RobinhoodMap()
        {
            foreach (var key in keys)
            {
                _robinhoodMap.Emplace(key, key);
            }
        }

        [Benchmark]
        public void QuadMap()
        {
            foreach (var key in keys)
            {
                _quadmap.Emplace(key, key);
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
