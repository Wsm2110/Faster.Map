using Faster.Map.Core;
using System;
using System.Collections.Generic;
using Xunit;

namespace Faster.Map.RobinhoodMap.Tests;

public class RobinhoodMap_Emplace_Tests
{
    [Fact]
    public void InsertSingleElement()
    {
        var map = new RobinhoodMap<int, string>();

        Assert.True(map.Emplace(1, "one"));
        Assert.Equal(1, map.Count);
    }

    [Fact]
    public void InsertDuplicateReturnsFalse()
    {
        var map = new RobinhoodMap<int, string>();

        Assert.True(map.Emplace(1, "one"));
        Assert.False(map.Emplace(1, "two"));
        Assert.Equal(1, map.Count);
    }

    [Fact]
    public void InsertMany_Unique()
    {
        var map = new RobinhoodMap<int, int>();

        for (int i = 0; i < 10000; i++)
            Assert.True(map.Emplace(i, i * 2));

        Assert.Equal(10000, map.Count);
    }

    [Fact]
    public void InsertMany_WithCollisions()
    {
        var map = new RobinhoodMap<int, int>();

        for (int i = 0; i < 5000; i++)
        {
            int k = i * 16; // forces heavy collisions in power-of-two table
            Assert.True(map.Emplace(k, i));
        }

        Assert.Equal(5000, map.Count);
    }

    [Fact]
    public void InsertTriggersResizeAndStillCorrect()
    {
        var map = new RobinhoodMap<int, int>(16);

        for (int i = 0; i < 5000; i++)
            map.Emplace(i, i);

        Assert.Equal(5000, map.Count);
    }

    [Fact]
    public void DenseIndexNeverCorrupts()
    {
        var map = new RobinhoodMap<int, int>();

        for (int i = 0; i < 3000; i++)
            map.Emplace(i, i);

        for (int i = 0; i < 3000; i++)
        {
            Assert.True(map.Emplace(i + 10000, i));
        }

        Assert.Equal(6000, map.Count);
    }

    [Fact]
    public void InsertAlternatingPattern()
    {
        var map = new RobinhoodMap<int, int>();

        for (int i = 0; i < 2000; i++)
        {
            Assert.True(map.Emplace(i, i));
            Assert.True(map.Emplace(i + 100000, i));
        }

        Assert.Equal(4000, map.Count);
    }

    [Fact]
    public void NoInfiniteLoopsUnderStress()
    {
        var map = new RobinhoodMap<int, int>();

        var rand = new Random(123);

        for (int i = 0; i < 20000; i++)
        {
            int k = rand.Next(0, 100000);
            map.Emplace(k, i);
        }

        Assert.True(map.Count > 1000);
    }

    [Fact]
    public void RandomInsertMatchesDictionary()
    {
        var map = new RobinhoodMap<int, int>();
        var dict = new Dictionary<int, int>();

        var rand = new Random(42);

        for (int i = 0; i < 20000; i++)
        {
            int k = rand.Next(0, 50000);

            bool a = map.Emplace(k, i);
            bool b = dict.TryAdd(k, i);

            Assert.Equal(b, a);
        }

        Assert.Equal(dict.Count, map.Count);
    }

    [Fact]
    public void HandlesWorstCaseCollisionCluster()
    {
        var map = new RobinhoodMap<int, int>(8);

        for (int i = 0; i < 4096; i++)
            map.Emplace(i << 4, i);

        Assert.Equal(4096, map.Count);
    }

    [Fact]
    public void Insert_ZeroAndMaxIntKeys()
    {
        var map = new RobinhoodMap<int, int>();

        Assert.True(map.Emplace(0, 0));
        Assert.True(map.Emplace(int.MaxValue, 1));
        Assert.True(map.Emplace(int.MinValue, 2));

        Assert.Equal(3, map.Count);
    }

    [Fact]
    public void Insert_MassiveResizeChain()
    {
        var map = new RobinhoodMap<int, int>(4);

        for (int i = 0; i < 100_000; i++)
            map.Emplace(i, i);

        Assert.Equal(100_000, map.Count);
    }
      

    [Fact]
    public void Insert_WorstCaseAlternatingCollision()
    {
        var map = new RobinhoodMap<int, int>(8);

        for (int i = 0; i < 10_000; i++)
            map.Emplace(i * 32, i);

        Assert.Equal(10_000, map.Count);
    }

    [Fact]
    public void Insert_RandomHighEntropyKeys()
    {
        var map = new RobinhoodMap<Guid, int>();

        for (int i = 0; i < 20_000; i++)
            map.Emplace(Guid.NewGuid(), i);

        Assert.Equal(20_000, map.Count);
    }

    [Fact]
    public void Insert_Then_ReinsertAfterResize()
    {
        var map = new RobinhoodMap<int, int>(8);

        for (int i = 0; i < 5000; i++)
            map.Emplace(i, i);

        for (int i = 5000; i < 10000; i++)
            Assert.True(map.Emplace(i, i));

        Assert.Equal(10000, map.Count);
    }

    [Fact]
    public void AssertInsertingBulk()
    {
        var rnd = new Random(3);
        var uni = new HashSet<uint>();
        while (uni.Count < (uint)(10_000_00))
        {
            uni.Add((uint)rnd.Next());
        }

        var keys = uni.ToArray();
        var _robinHoodMap = new RobinhoodMap<uint, uint>(16);

        foreach (var key in keys)
        {
            _robinHoodMap.Emplace(key, key);
        }

        Assert.Equal(keys.Length, _robinHoodMap.Count); 

    }

    sealed class BadHashKey
    {
        public int Value;
        public BadHashKey(int v) => Value = v;
        public override int GetHashCode() => 1; // worst-case hash
        public override bool Equals(object? obj) => obj is BadHashKey o && o.Value == Value;
    }
}