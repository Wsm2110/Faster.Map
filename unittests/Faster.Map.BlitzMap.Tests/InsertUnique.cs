using Faster.Map.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Faster.Map.BlitzMap.Tests.Get;
using Xunit;

namespace Faster.Map.BlitzMap.Tests;

public class InsertUnique
{
    private readonly BlitzMap<int, string> _map;

    public InsertUnique()
    {
        _map = new BlitzMap<int, string>(16, 0.75);
    }

    [Fact]
    public void InsertUnique_Inserts_WhenKeyDoesNotExist()
    {
        Assert.True(_map.InsertUnique(1, "UniqueValue"));
        Assert.True(_map.Get(1, out var value));
        Assert.Equal("UniqueValue", value);
    }

    [Fact]
    public void InsertUnique_HandlesExtremeValues_Correctly()
    {
        Assert.True(_map.InsertUnique(int.MinValue, "MinValue"));
        Assert.True(_map.InsertUnique(int.MaxValue, "MaxValue"));
        Assert.True(_map.Get(int.MinValue, out var minValue));
        Assert.Equal("MinValue", minValue);
        Assert.True(_map.Get(int.MaxValue, out var maxValue));
        Assert.Equal("MaxValue", maxValue);
    }

    [Fact]
    public void InsertUnique_ShouldInsert_WhenKeyDoesNotExist()
    {
        bool inserted = _map.InsertUnique(1, "newValue");

        Assert.True(inserted);
        Assert.True(_map.Get(1, out var value));
        Assert.Equal("newValue", value);
    }

    [Fact]
    public void InsertUnique_ShouldHandleMinAndMaxIntKeys()
    {
        bool minInserted = _map.InsertUnique(int.MinValue, "minValue");
        bool maxInserted = _map.InsertUnique(int.MaxValue, "maxValue");

        Assert.True(minInserted);
        Assert.True(maxInserted);
        Assert.True(_map.Get(int.MinValue, out var minValue));
        Assert.Equal("minValue", minValue);
        Assert.True(_map.Get(int.MaxValue, out var maxValue));
        Assert.Equal("maxValue", maxValue);
    }

    [Fact]
    public void InsertUnique_ShouldHandleCollisionScenarios()
    {
        _map.InsertUnique(1, "value1");
        _map.InsertUnique(17, "value17"); // Assuming collision with key 1

        Assert.True(_map.Get(1, out var value1));
        Assert.Equal("value1", value1);
        Assert.True(_map.Get(17, out var value17));
        Assert.Equal("value17", value17);
    }

    [Fact]
    public void InsertUnique_ShouldHandleNullValues()
    {
        var map = new BlitzMap<int, string?>();
        bool inserted = map.InsertUnique(1, null);

        Assert.True(inserted);
        Assert.True(map.Get(1, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void InsertUnique_ShouldHandleZeroAndNegativeKeys()
    {
        bool zeroInserted = _map.InsertUnique(0, "zeroValue");
        bool negativeInserted = _map.InsertUnique(-1, "negativeValue");

        Assert.True(zeroInserted);
        Assert.True(negativeInserted);
        Assert.True(_map.Get(0, out var zeroValue));
        Assert.Equal("zeroValue", zeroValue);
        Assert.True(_map.Get(-1, out var negativeValue));
        Assert.Equal("negativeValue", negativeValue);
    }

    [Fact]
    public void InsertUnique_ShouldHandleHighVolumeOfInserts()
    {
        for (int i = 0; i < 100; i++)
        {
            bool inserted = _map.InsertUnique(i, $"value{i}");
            Assert.True(inserted);
            Assert.True(_map.Get(i, out var value));
            Assert.Equal($"value{i}", value);
        }
    }
}