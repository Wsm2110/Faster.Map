using BenchmarkDotNet.Attributes;
using Faster.Map.DenseMap;
using Faster.Map.QuadMap;
using Faster.Map.RobinHoodMap;
using Microsoft.Collections.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    public class StringBenchmark
    {
        #region Fields

        private DenseMap<string, string> _denseMap = new DenseMap<string, string>();
        private Dictionary<string, string> dic = new Dictionary<string, string>();
        private RobinhoodMap<string, string> _robinhoodMap = new RobinhoodMap<string, string>();
        private QuadMap<string, string> _quadMap = new QuadMap<string, string>();

        private string[] keys;

        #endregion

        #region Properties

        [ParamsAttribute(/*1, 10, 100, 1000, 10000, 100000*/ 1000000)]
        public int Length { get; set; }

        #endregion

        /// <summary>
        /// Generate a million Keys and shuffle them afterwards
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            var output = File.ReadAllText("Numbers.txt");
            var splittedOutput = output.Split(',');

            keys = new string[Length];

            for (var index = 0; index < Length; index++)
            {
                keys[index] = splittedOutput[index];
            }

            foreach (var key in keys)
            {
                 _denseMap.Emplace(key, key);
                _robinhoodMap.Emplace(key, key);
                dic.Add(key, key);
            
                _quadMap.Emplace(key, key);
            }
        }

        //[Benchmark]
        //public void DenseMap()
        //{
        //    foreach (var key in keys)
        //    {
        //        _denseMap.Get(key, out var result);
        //    }
        //}

        [Benchmark]
        public void RobinhoodMap()
        {
            foreach (var key in keys)
            {
                _robinhoodMap.Get(key, out var result);
            }
        }

        //[Benchmark]
        //public void QuadMap()
        //{
        //    foreach (var key in keys)
        //    {
        //        _quadMap.Get(key, out var result);
        //    }
        //}

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
