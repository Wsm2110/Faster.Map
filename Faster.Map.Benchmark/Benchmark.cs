using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using System;
using System.Collections.Generic;

namespace Faster.Map.Benchmark
{
    [HardwareCounters(HardwareCounter.BranchInstructions, HardwareCounter.CacheMisses, HardwareCounter.BranchMispredictions, HardwareCounter.LlcMisses)]
    public class Benchmark
    {
        #region Fields

        Map<uint, uint> _map = new Map<uint, uint>();
        NumericalMap<uint, uint> _numericalMap = new NumericalMap<uint, uint>();
        Dictionary<uint, uint> _dict = new Dictionary<uint, uint>();

        #endregion

        [Params(1000000)]
        public int N { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            var r = new Random();
            for (int i = 0; i < 1000000; i++)
            {
                var result = r.Next(100, 1000000);

                if (!_dict.ContainsKey((uint)result))
                {
                    _dict.Add((uint)result, (uint)result);
                }

                _map.Emplace((uint)result, (uint)result);
                _numericalMap.Emplace((uint)result, (uint)result);
            }

            _map.Emplace(33, 33);
            _numericalMap.Emplace(33, 33);
            _dict.Add(33, 33);
        }

        [Benchmark]
        public void NumericalMap()
        {
            uint result = 0;
            for (int i = 0; i < N; i++)
            {
                _numericalMap.Get(33, out result);
            }

            if (result != 33)
            {
                throw new InvalidOperationException("benchmark failed");
            }
        }

        [Benchmark]
        public void Map()
        {
            uint result = 0;
            for (int i = 0; i < N; i++)
            {
                _numericalMap.Get(33, out result);
            }

            if (result != 33)
            {
                throw new InvalidOperationException("benchmark failed");
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

            if (result != 33)
            {
                throw new InvalidOperationException("benchmark failed");
            }
        }
    }
}
