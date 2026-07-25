using Faster.Map.Concurrent;
using Faster.Map.Core;
using Xunit;

namespace Faster.Map.BlitzMap.Tests;

public class ClearTests
{
    private readonly BlitzMap<int, string> _map;

    public ClearTests()
    {
        _map = new BlitzMap<int, string>(16, 0.75);
    }

    [Fact]
    public void Clear_ShouldEmptyMap_WhenMapHasElements()
    {
        _map.Insert(1, "value1");
        _map.Insert(2, "value2");
        _map.Insert(3, "value3");

        _map.Clear();

        Assert.Equal(0, _map.Count);
        Assert.False(_map.Get(1, out _));
        Assert.False(_map.Get(2, out _));
        Assert.False(_map.Get(3, out _));
    }

    [Fact]
    public void Clear_ShouldNotThrow_WhenMapIsAlreadyEmpty()
    {
        _map.Clear();
        _map.Clear(); // Calling clear again on an already empty map

        Assert.Equal(0, _map.Count);
        Assert.False(_map.Get(1, out _));
    }

    [Fact]
    public void Clear_ShouldAllowReinsertion_AfterClearing()
    {
        _map.Insert(1, "value1");
        _map.Clear();

        bool inserted = _map.Insert(1, "newValue1");

        Assert.True(inserted);
        Assert.True(_map.Get(1, out var value));
        Assert.Equal("newValue1", value);
    }

    [Fact]
    public void Clear_ShouldResetMapCapacity_WhenCalled()
    {
        var smallMap = new BlitzMap<int, string>(4, 0.75);
        for (int i = 0; i < 4; i++)
        {
            smallMap.Insert(i, $"value{i}");
        }

        smallMap.Clear();

        Assert.Equal(0, smallMap.Count);
        for (int i = 0; i < 4; i++)
        {
            Assert.False(smallMap.Get(i, out _));
        }
    }

    [Fact]
    public void Clear_ShouldHandleHighVolumeOfElements()
    {
        for (int i = 0; i < 100; i++)
        {
            _map.Insert(i, $"value{i}");
        }

        _map.Clear();

        Assert.Equal(0, _map.Count);
        for (int i = 0; i < 100; i++)
        {
            Assert.False(_map.Get(i, out _));
        }
    }

    [Fact]
    public void Clear_ShouldMaintainIntegrity_WhenCalledMultipleTimes()
    {
        _map.Insert(1, "value1");
        _map.Clear();
        _map.Clear();

        Assert.Equal(0, _map.Count);
        Assert.False(_map.Get(1, out _));
    }

    [Fact]
    public void Clear_ShouldAllowInsertOrUpdate_AfterClearing()
    {
        _map.InsertOrUpdate(1, "value1");
        _map.Clear();

         bool result = _map.InsertOrUpdate(1, "newValue1");

        //  Assert.True(result);
        Assert.True(_map.Get(1, out var value));
        Assert.Equal("newValue1", value);
    }

    [Fact]
    public void Clear_OnEmpty_IsIdempotent()
    {
        var map = new BlitzMap<int, int>();

        map.Clear();
        map.Clear();

        Assert.Equal(0, map.Count);
        Assert.False(map.Contains(1));
        Assert.False(map.Get(1, out _));
    }

    [Fact]
    public void Clear_RemovesAllKeys()
    {
        var map = new BlitzMap<int, int>();
        var dict = new Dictionary<int, int>();

        for (int i = 0; i < 2000; i++)
        {
            int key = i * 3 + 7;
            int value = i ^ 0x5a5a5a5a;
            map.InsertOrUpdate(key, value);
            dict[key] = value;
        }

        map.Clear();

        Assert.Equal(0, map.Count);

        foreach (var kv in dict)
        {
            Assert.False(map.Contains(kv.Key));
            Assert.False(map.Get(kv.Key, out _));
            Assert.False(map.Remove(kv.Key));
        }
    }

    [Fact]
    public void Clear_AllowsReuse_WithCorrectResults()
    {
        var map = new BlitzMap<int, int>();

        for (int i = 0; i < 500; i++)
            map.InsertOrUpdate(i, i + 10);

        map.Clear();

        for (int i = 0; i < 500; i++)
        {
            Assert.False(map.Contains(i));
            Assert.False(map.Get(i, out _));
        }

        for (int i = 0; i < 500; i++)
            map.InsertOrUpdate(i, i + 99);

        for (int i = 0; i < 500; i++)
        {
            Assert.True(map.Contains(i));
            Assert.True(map.Get(i, out var v));
            Assert.Equal(i + 99, v);
        }

        Assert.Equal(500, map.Count);
    }

    [Fact]
    public void Clear_AfterMixedOps_DoesNotCorruptFutureOps()
    {
        var rnd = new Random(12345);
        var map = new BlitzMap<int, int>();
        var dict = new Dictionary<int, int>();

        // Do a bunch of mixed ops.
        for (int i = 0; i < 10_000; i++)
        {
            int key = rnd.Next(0, 2000);
            int value = rnd.Next();

            switch (rnd.Next(3))
            {
                case 0:
                    map.InsertOrUpdate(key, value);
                    dict[key] = value;
                    break;

                case 1:
                    Assert.Equal(dict.Remove(key), map.Remove(key));
                    break;

                case 2:
                    Assert.Equal(dict.TryGetValue(key, out var dv), map.Get(key, out var mv));
                    if (dict.TryGetValue(key, out dv)) Assert.Equal(dv, mv);
                    break;
            }
        }

        map.Clear();
        dict.Clear();

        // Immediately continue with more mixed ops to catch stale pointers.
        for (int i = 0; i < 10_000; i++)
        {
            int key = rnd.Next(0, 2000);
            int value = rnd.Next();

            switch (rnd.Next(3))
            {
                case 0:
                    map.InsertOrUpdate(key, value);
                    dict[key] = value;
                    break;

                case 1:
                    Assert.Equal(dict.Remove(key), map.Remove(key));
                    break;

                case 2:
                    Assert.Equal(dict.TryGetValue(key, out var dv), map.Get(key, out var mv));
                    if (dict.TryGetValue(key, out dv)) Assert.Equal(dv, mv);
                    break;
            }
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(16)]
    public void Clear_RepeatedCycles_StaysCorrect(int cycles)
    {
        var map = new BlitzMap<int, int>();
        var dict = new Dictionary<int, int>();

        for (int c = 0; c < cycles; c++)
        {
            for (int i = 0; i < 1000; i++)
            {
                int key = (c * 1000) + i;
                int value = key ^ 0x1234567;
                map.InsertOrUpdate(key, value);
                dict[key] = value;
            }

            foreach (var kv in dict)
            {
                Assert.True(map.Get(kv.Key, out var v));
                Assert.Equal(kv.Value, v);
            }

            map.Clear();
            dict.Clear();

            Assert.Equal(0, map.Count);
            Assert.False(map.Contains(c));
            Assert.False(map.Get(c, out _));
        }
    }
}