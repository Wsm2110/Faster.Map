using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Faster.Map.Benchmark.Utilities;
using Faster.Map.Core;
using Faster.Map.Hasher;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
namespace Faster.Map.Benchmark;

//[MarkdownExporterAttribute.GitHub]
//[DisassemblyDiagnoser]
//[MemoryDiagnoser]
//[SimpleJob(RunStrategy.Monitoring, launchCount: 1, iterationCount: 10, warmupCount: 3)]

public class EnumerableBenchmark
{
    #region Fields

    private DenseMap<uint, uint> _denseMap;
    private BlitzMap<uint, uint> _blitz;
    private Dictionary<uint, uint> _dictionary;
    private RobinhoodMap<uint, uint> _robinHoodMap;

    private uint[] keys;

    #endregion

    #region Properties

    [Params(0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8)]
    public double LoadFactor { get; set; }

    [Params(134_217_728)]
    public uint Length { get; set; }

    #endregion

    /// <summary>
    /// Generate a million Keys and shuffle them afterwards
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var rnd = new FastRandom(6);
        var uni = new HashSet<uint>((int)Length);
        while (uni.Count < (uint)(Length * LoadFactor))
        {
            uni.Add((uint)rnd.Next());
        }

        keys = uni.ToArray();

        // round of length to power of 2 prevent resizing
        uint length = BitOperations.RoundUpToPowerOf2(Length);
        int dicLength = HashHelpers.GetPrime((int)Length);

        _denseMap = new DenseMap<uint, uint>(length);
        _blitz = new BlitzMap<uint, uint>((int)length, LoadFactor);

        _dictionary = new Dictionary<uint, uint>(dicLength);
        _robinHoodMap = new RobinhoodMap<uint, uint>(length, 0.9);

        foreach (var key in keys)
        {
            _dictionary.Add(key, key);
            _denseMap.Emplace(key, key);
            _blitz.Insert(key, key);
            _robinHoodMap.Emplace(key, key);
        }
    }

    [Benchmark]
    public void BlitzMap()
    {
        foreach (var entry in _blitz) ;

    }

    [Benchmark]
    public void Dictionary()
    {
        foreach (var entry in _dictionary) ;
    }

    [Benchmark]
    public void DenseMap()
    {
        foreach (var entry in _denseMap.Entries) ;
    }



    [Benchmark]
    public void RobinhoodMap()
    {
        foreach (var entry in _robinHoodMap.Entries) ;
    }
}