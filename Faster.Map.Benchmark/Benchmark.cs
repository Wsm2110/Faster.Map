using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using System.Collections.Generic;

namespace Faster.Map.Benchmark
{
    [HardwareCounters(HardwareCounter.BranchInstructions,
        HardwareCounter.CacheMisses,
        HardwareCounter.BranchMispredictions,
        HardwareCounter.LlcMisses)]
   
    public class Benchmark
    {
        #region Fields

        Map<uint, uint> _map = new Map<uint, uint>();
        GenericMap<uint, uint> _genericMap = new GenericMap<uint, uint>(16, 0.5);
        ExampleMap<uint> _fixedKeyMap = new ExampleMap<uint>(16, 0.5);
        Dictionary<uint, uint> _dict = new Dictionary<uint, uint>();

        #endregion

        [Params(1000000)]
        public int N { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            //var r = new Random();
            //for (int i = 0; i < 1000000; i++)
            //{
            //    var result = r.Next(100, 1000000);

            //    if (!_dict.ContainsKey((uint)result))
            //    {
            //        _dict.Add((uint)result, (uint)result);
            //    }

            //    _map.Emplace((uint)result, (uint)result);
            //    _numericalMap.Emplace((uint)result, (uint)result);
            //}

            _map.Emplace(33, 33);          
            _dict.Add(33, 33);
            _genericMap.Emplace(33, 33);
            _fixedKeyMap.Emplace(33, 33);
        }

        [Benchmark]
        public void FixedKeyMap()
        {
            uint result = 0;
            for (int i = 0; i < N; i++)
            {
                _fixedKeyMap.Get(33, out result);
            }
        }


        [Benchmark]
        public void Map()
        {
            uint result = 0;
            for (int i = 0; i < N; i++)
            {
                _map.Get(33, out result);
            }
        }


        [Benchmark]
        public void GenericMap()
        {
            uint result = 0;
            for (int i = 0; i < N; i++)
            {
                _genericMap.Get(33, out result);
            }
        }

        [Benchmark]
        public void Dictionary()
        {
            uint result = 0;
            for (int i = 0; i < N; i++)
            {
                _dict.TryGetValue(33, out result);
            }
        }
    }
}
