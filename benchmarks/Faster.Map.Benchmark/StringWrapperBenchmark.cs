using BenchmarkDotNet.Attributes;
using Faster.Map.Core;
using Faster.Map.DenseMap;
using Faster.Map.QuadMap;
using Faster.Map.RobinHoodMap;
using System.Collections.Generic;
using System.IO;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    public class StringWrapperBenchmark
    {
        #region Fields

        private DenseMap<StringWrapper, StringWrapper> _denseMap = new();
        private Dictionary<StringWrapper, StringWrapper> dic = new();
        private RobinhoodMap<StringWrapper, StringWrapper> _robinhoodMap = new();
        private QuadMap<StringWrapper, StringWrapper> _quadMap = new();

        private string[] keys;

        #endregion

        #region Properties

        [ParamsAttribute(1000000)]
        public int Length { get; set; }

        #endregion

        /// <summary>
        /// Generate a million Keys
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
               dic.Add(key, key);
               // _denseMap.Emplace(key, key);
                _robinhoodMap.Emplace(key, key);
                //_quadMap.Emplace(key, key);
            }
        }


        //[Benchmark]
        //public void DenseMap()
        //{
        //    foreach (var key in keys)
        //    {
        //        _denseMap.Get(key, out var _);
        //    }
        //}

        [Benchmark]
        public void RobinhoodMap()
        {
            foreach (var key in keys)
            {
                _robinhoodMap.Get(key, out var _);
            }
        }

        //[Benchmark]
        //public void QuadMap()
        //{
        //    foreach (var key in keys)
        //    {
        //        _quadMap.Get(key, out var _);
        //    }
        //}

        [Benchmark(Baseline = true)]
        public void Dictionary()
        {
            foreach (var key in keys)
            {
                dic.TryGetValue(key, out var _);
            }
        }

    }
}
