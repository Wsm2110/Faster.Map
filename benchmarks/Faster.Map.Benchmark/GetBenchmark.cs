using System;
using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Collections.Extensions;
using System.Diagnostics;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters;

namespace Faster.Map.Benchmark
{

    [MarkdownExporterAttribute.GitHub]
    [DisassemblyDiagnoser]
    [MemoryDiagnoser]  
    //[CsvMeasurementsExporter]
    //[RPlotExporter]
    //// [SimpleJob(RunStrategy.Monitoring, 1, 10, 50)]
    public class GetBenchmark
    {
        #region Fields

        DenseMap<uint, uint> _denseMap = new DenseMap<uint, uint>();
        private Dictionary<uint, uint> dictionary = new Dictionary<uint, uint>();
        private DictionarySlim<uint, uint> _slim = new DictionarySlim<uint, uint>();

        private uint[] keys;
        private uint _length = 900000;

        #endregion

        /// <summary>
        /// Generate a million Keys and shuffle them afterwards
        /// </summary>
        [GlobalSetup]
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
                dictionary.Add(key, key);
                _denseMap.Emplace(key, key);
                _slim.GetOrAddValueRef(key);
            }
        }

        [ParamsAttribute(900000)]
        public int Length { get; set; }

        [Benchmark]
        public void DenseMap()
        {
            foreach (var key in keys)
            {
                _denseMap.Get(key, out var result);
            }
        }

        //[Benchmark]
        //public void SlimDictionary()
        //{
        //    foreach (var key in keys)
        //    {
        //        _slim.GetOrAddValueRef(key);
        //    }
        //}

        //[Benchmark]
        //public void Dictionary()
        //{
        //    foreach (var key in keys)
        //    {
        //        dictionary.TryGetValue(key, out var result);
        //    }
        //}

    }
}