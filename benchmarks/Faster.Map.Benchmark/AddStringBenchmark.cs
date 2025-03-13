//using System;
//using System.Collections.Generic;
//using System.IO;
//using BenchmarkDotNet.Attributes;
//using System.Linq;
//using System.Numerics;

//using System.Collections;
//using Faster.Map.Hasher;
//using BenchmarkDotNet.Engines;

//namespace Faster.Map.Benchmark;

//[MarkdownExporterAttribute.GitHub]
//[MemoryDiagnoser]
//[SimpleJob(RunStrategy.Monitoring, launchCount: 1, iterationCount: 5, warmupCount: 5)]

//public class AddStringBenchmark
//{
//    #region Fields

//    //fixed size, dont want to measure resize()
//    private DenseMap<string, string> _dense;
//    private Dictionary<string, string> dic;
//    private RobinhoodMap<string, string> _robinhoodMap;
//    private DenseMap<string, string> _denseMapxxHash;
//    private DenseMap<string, string> _denseMapGxHash;
//    private DenseMap<string, string> _denseMapFastHash;
//    private BlitzMap<string, string, DefaultHasher> _blitzMap;

//    private string[] keys;

//    #endregion

//    #region Properties
 
//    [Params(0.5, 0.6, 0.7, 0.8)]
//    public static double LoadFactor { get; set; }

//    [Params(134_217_728)]
//    public static uint Length { get; set; }

//    #endregion

//    /// <summary>
//    /// Generate a million Keys and shuffle them afterwards
//    /// </summary>
//    [GlobalSetup]
//    public void Add()
//    {
//        var rnd = new Random(3);

//        var uni = new HashSet<string>();

//        while (uni.Count < (uint)(Length * LoadFactor))
//        {
//            bool v = uni.Add(rnd.NextInt64().ToString());
//        }

//        keys = uni.ToArray();
//    }

//    [IterationSetup]
//    public void Setup()
//    {
//        // round of length to power of 2 prevent resizing
//        uint length = BitOperations.RoundUpToPowerOf2(Length);
//        int dicLength = HashHelpers.GetPrime((int)Length);

//        _dense = new DenseMap<string, string>(length, 0.875, new XxHash3StringHasher());

//        _blitzMap = new BlitzMap<string, string, DefaultHasher>((int)length, 0.8, new DefaultHasher());

//        dic = new Dictionary<string, string>(dicLength);
//        //   _robinhoodMap = new RobinhoodMap<string, string>(length * 2);
//    }

//    [Benchmark]
//    public void BlitzMap()
//    {
//        for (int i = 0; i < keys.Length; i++)
//        {
//            var key = keys[i];
//            _blitzMap.Insert(key, key);
//        }
//    }

//    [Benchmark]
//    public void DenseMap()
//    {
//        for (int i = 0; i < keys.Length; i++)
//        {
//            var key = keys[i];
//            _dense.Emplace(key, key);
//        }
//    }


//    [Benchmark]
//    public void Dictionary()
//    {
//        for (int i = 0; i < keys.Length; i++)
//        {
//            var key = keys[i];
//            dic.Add(key, key);
//        }
//    }
//}