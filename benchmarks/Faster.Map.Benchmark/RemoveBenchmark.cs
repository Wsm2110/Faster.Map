using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Faster.Map.DenseMap;
using Faster.Map.QuadMap;
using Faster.Map.RobinHoodMap;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    //[SimpleJob(RunStrategy.Monitoring, 1, 10, 50)]
    public class RemoveBenchmark
    {
        #region Fields

        private DenseMap<uint, uint> _denseMap = new DenseMap<uint, uint>();
        private Dictionary<uint, uint> dic = new Dictionary<uint, uint>();
        private RobinhoodMap<uint, uint> _robinhoodMap = new RobinhoodMap<uint, uint>();
        private QuadMap<uint, uint> _quadMap = new QuadMap<uint, uint>();

        private uint[] keys;

        #endregion

        #region Properties

        #region Properties

        [Params( 1000000)]
        public int Length { get; set; }

        #endregion

        #endregion

        /// <summary>
        /// Generate a million Keys and shuffle them afterwards
        /// </summary>
        [IterationSetup]
        public void Setup()
        {
            var output = File.ReadAllText("Numbers.txt");
            var splittedOutput = output.Split(',');

            keys = new uint[Length];

            for (var index = 0; index < Length; index++)
            {
                keys[index] = uint.Parse(splittedOutput[index]);
            }

            foreach (var key in keys)
            {
                dic.Add(key, key);
                _denseMap.Emplace(key, key);
                _robinhoodMap.Emplace(key, key);
                _quadMap.Emplace(key, key);
            }

            // Shuffle(new Random(), keys);
        }

        [IterationCleanup]
        public void clear()
        {
            dic.Clear();
            _denseMap.Clear();
            _robinhoodMap.Clear();
            _quadMap.Clear();
        }

        #region Benchmarks


        [Benchmark]
        public void DenseMap()
        {
            foreach (var key in keys)
            {
                _denseMap.Remove(key);
            }
        }

        [Benchmark]
        public void RobinhoodMap()
        {
            foreach (var key in keys)
            {
                _robinhoodMap.Remove(key);
            }
        }

        [Benchmark]
        public void QuadMap()
        {
            foreach (var key in keys)
            {
                _quadMap.Remove(key);
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



        #endregion

    }
}
