using BenchmarkDotNet.Attributes;
using Faster.Map.DenseMap;
using Faster.Map.QuadMap;
using Faster.Map.RobinHoodMap;
using System;
using System.Collections.Generic;
using System.IO;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    public class AddAndResizeBenchmark
    {
        #region Fields

        DenseMap<uint, uint> _denseMap = new DenseMap<uint, uint>();
        private Dictionary<uint, uint> dic = new Dictionary<uint, uint>();
        private RobinhoodMap<uint, uint> _robinhoodMap = new RobinhoodMap<uint, uint>();
        private QuadMap<uint, uint> _quadMap = new QuadMap<uint, uint>();

        private uint[] keys;

        #endregion

        #region Properties

        [ParamsAttribute(1000000)]
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

            //  Shuffle(new Random(), keys);
        }

        [IterationCleanup]
        public void ResetMaps()
        {
            _denseMap = new DenseMap<uint, uint>();
            dic = new Dictionary<uint, uint>();
            _robinhoodMap = new RobinhoodMap<uint, uint>();
            _quadMap = new QuadMap<uint, uint>();
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
                _quadMap.Emplace(key, key);
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