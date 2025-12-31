using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Faster.Map.Contracts;
using Faster.Map.Core;
using Faster.Map.Hashing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Faster.Map.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    [DisassemblyDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, launchCount: 1, iterationCount: 8, warmupCount: 3)]
    public class CorrelatedKeyBenchmark
    {
        #region Fields

        private DenseMap<uint, uint, FastHasherUint> _denseMapAES;
        private DenseMap<uint, uint, AvalancheHasherUint> _denseMapAvalanche;
        private BlitzMap<uint, uint, FastHasherUint> _blitzMapAes;
        private BlitzMap<uint, uint, AvalancheHasherUint> _blitzMapAvalance;
        private uint[] keys;

        #endregion

        #region Params

        [Params("Random", "Correlated")]
        public string KeyPattern { get; set; }

        [Params(0.6, 0.75, 0.85, 0.9, 0.95)]
        public double LoadFactor { get; set; }

        [Params(16_777_216)]
        public uint Length { get; set; }

        #endregion

        #region Setup

        [GlobalSetup]
        public void Setup()
        {
            int count = (int)(Length * LoadFactor);
            keys = new uint[count];

            if (KeyPattern == "Random")
            {
                var rnd = new Random(123);
                var set = new HashSet<uint>(count);
                while (set.Count < count)
                    set.Add((uint)rnd.Next());

                keys = set.ToArray();
            }
            else
            {
                // MMF / pointer-style correlated keys
                uint baseAddr = 0x4000_0000;
                for (int i = 0; i < count; i++)
                    keys[i] = baseAddr + ((uint)i << 4); // only 4 bits change
            }

            uint capacity = BitOperations.RoundUpToPowerOf2(Length);

            _denseMapAES = new DenseMap<uint, uint, FastHasherUint>(capacity);
            _denseMapAvalanche = new DenseMap<uint, uint, AvalancheHasherUint>(capacity);
            _blitzMapAes = new BlitzMap<uint, uint, FastHasherUint>((int)capacity);
            _blitzMapAvalance = new BlitzMap<uint, uint, AvalancheHasherUint>((int)capacity);

            foreach (var key in keys)
            {
                _denseMapAES.InsertOrUpdate(key, key);
                _denseMapAvalanche.InsertOrUpdate(key, key);

                _blitzMapAes.InsertOrUpdate(key, key);
                _blitzMapAvalance.InsertOrUpdate(key, key);
            }
        }

        #endregion

        #region Benchmarks

        [Benchmark(Baseline = true)]
        public void DenseMap_AES()
        {
            for (int i = 0; i < keys.Length; i++)
                _denseMapAES.Get(keys[i], out _);
        }

        [Benchmark]
        public void DenseMap_Avalanche()
        {
            for (int i = 0; i < keys.Length; i++)
                _denseMapAvalanche.Get(keys[i], out _);
        }

        [Benchmark]
        public void BlitzMapAes()
        {
            for (int i = 0; i < keys.Length; i++)
                _blitzMapAes.Get(keys[i], out _);
        }

        [Benchmark]
        public void BlitzMapAvalanche()
        {
            for (int i = 0; i < keys.Length; i++)
                _blitzMapAvalance.Get(keys[i], out _);
        }

        #endregion
    }

    #region AvalancheHasher

    public struct AvalancheHasherUint : IHasher<uint>
    {

        public bool Equals(uint x, uint y)
        {
            return x == y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(uint key)
        {
            uint h = key;
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;
            return h;
        }
    }

    #endregion
}
