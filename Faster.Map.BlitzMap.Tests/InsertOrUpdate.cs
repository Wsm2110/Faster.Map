using Faster.Map.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Faster.Map.BlitzMap.Tests;

public class InsertOrUpdate
{
    private readonly BlitzMap<int, string> _map;

    public InsertOrUpdate()
    {
        _map = new BlitzMap<int, string>(16, 0.75);
    }

    [Fact]
    public void InsertOrUpdate_ShouldInsert_WhenKeyDoesNotExist()
    {
        bool result = _map.InsertOrUpdate(1, "newValue");

        Assert.True(result);
        Assert.True(_map.Get(1, out var value));
        Assert.Equal("newValue", value);
    }

    [Fact]
    public void InsertOrUpdate_ShouldUpdate_WhenKeyExists()
    {
        _map.InsertOrUpdate(1, "initialValue");
        bool result = _map.InsertOrUpdate(1, "updatedValue");

        Assert.True(result);
        Assert.True(_map.Get(1, out var value));
        Assert.Equal("updatedValue", value);
    }

    [Fact]
    public void InsertOrUpdate_ShouldHandleMinAndMaxIntKeys()
    {
        _map.InsertOrUpdate(int.MinValue, "minValue");
        _map.InsertOrUpdate(int.MaxValue, "maxValue");

        Assert.True(_map.Get(int.MinValue, out var minValue));
        Assert.Equal("minValue", minValue);
        Assert.True(_map.Get(int.MaxValue, out var maxValue));
        Assert.Equal("maxValue", maxValue);

        _map.InsertOrUpdate(int.MinValue, "updatedMinValue");
        _map.InsertOrUpdate(int.MaxValue, "updatedMaxValue");

        Assert.True(_map.Get(int.MinValue, out var updatedMinValue));
        Assert.Equal("updatedMinValue", updatedMinValue);
        Assert.True(_map.Get(int.MaxValue, out var updatedMaxValue));
        Assert.Equal("updatedMaxValue", updatedMaxValue);
    }

    [Fact]
    public void InsertOrUpdate_ShouldHandleNullValues()
    {
        var map = new BlitzMap<int, string?>();
        map.InsertOrUpdate(1, null);

        Assert.True(map.Get(1, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void InsertOrUpdate_ShouldHandleCollisionScenarios()
    {
        _map.InsertOrUpdate(1, "value1");
        _map.InsertOrUpdate(17, "value17"); // Assuming collision with key 1

        Assert.True(_map.Get(1, out var value1));
        Assert.Equal("value1", value1);
        Assert.True(_map.Get(17, out var value17));
        Assert.Equal("value17", value17);

        _map.InsertOrUpdate(17, "updatedValue17");

        Assert.True(_map.Get(17, out var updatedValue17));
        Assert.Equal("updatedValue17", updatedValue17);
    }

    [Fact]
    public void InsertOrUpdate_ShouldHandleHighVolumeOfOperations()
    {
        for (int i = 0; i < 64; i++)
        {
            bool result = _map.InsertOrUpdate(i, $"initialValue{i}");
            Assert.True(result);
            Assert.True(_map.Get(i, out var value));
            Assert.Equal($"initialValue{i}", value);
        }

        for (int i = 0; i < 64; i++)
        {
            bool result = _map.InsertOrUpdate(i, $"updatedValue{i}");
            Assert.True(result);
            Assert.True(_map.Get(i, out var value));
            Assert.Equal($"updatedValue{i}", value);
        }
    }

    [Fact]
    public void InsertOrUpdate_ShouldWorkCorrectly_WhenMapIsAtFullCapacity()
    {
        var smallMap = new BlitzMap<int, string>(4, 0.75);

        for (int i = 0; i < 4; i++)
        {
            smallMap.InsertOrUpdate(i, $"value{i}");
        }

        Assert.True(smallMap.Get(3, out var value));
        Assert.Equal("value3", value);

        smallMap.InsertOrUpdate(3, "updatedValue3");

        Assert.True(smallMap.Get(3, out var updatedValue));
        Assert.Equal("updatedValue3", updatedValue);
    }

    [Fact]
    public void InsertOrUpdate_ShouldHandleEdgeCase_WithZeroAndNegativeKeys()
    {
        _map.InsertOrUpdate(0, "zeroValue");
        _map.InsertOrUpdate(-1, "negativeValue");

        Assert.True(_map.Get(0, out var zeroValue));
        Assert.Equal("zeroValue", zeroValue);
        Assert.True(_map.Get(-1, out var negativeValue));
        Assert.Equal("negativeValue", negativeValue);
    }

    [Fact]
    public void InsertOrUpdate_ShouldHandleSpecialCharacterValues()
    {
        _map.InsertOrUpdate(1, "Spécîål Chåräçtęrś");

        Assert.True(_map.Get(1, out var value));
        Assert.Equal("Spécîål Chåräçtęrś", value);
    }

    [Fact]
    public void InsertOrUpdate_ShouldMaintainIntegrity_AfterClear()
    {
        _map.InsertOrUpdate(1, "value1");
        _map.Clear();

        bool result = _map.InsertOrUpdate(1, "newValue");

        Assert.True(result);
        Assert.True(_map.Get(1, out var value));
        Assert.Equal("newValue", value);
    }

    [Fact]
    public void InsertOrUpdate_UpdatesValue_WhenKeyExists()
    {
        _map.InsertOrUpdate(1, "Initial");
        _map.InsertOrUpdate(1, "Updated");
        Assert.True(_map.Get(1, out var value));
        Assert.Equal("Updated", value);
    }

    [Fact]
    public void InsertOrUpdate_InsertsValue_WhenKeyDoesNotExist()
    {
        Assert.False(_map.Get(2, out var _));
        _map.InsertOrUpdate(2, "Inserted");
        Assert.True(_map.Get(2, out var value));
        Assert.Equal("Inserted", value);
    }

    [Fact]
    public void InsertOrUpdate_HandlesExtremeValues_Correctly()
    {
        _map.InsertOrUpdate(int.MinValue, "MinValue");
        _map.InsertOrUpdate(int.MaxValue, "MaxValue");
        Assert.True(_map.Get(int.MinValue, out var minValue));
        Assert.Equal("MinValue", minValue);
        Assert.True(_map.Get(int.MaxValue, out var maxValue));
        Assert.Equal("MaxValue", maxValue);
    }

    [Fact]
    public void InsertOrUpdate_CorrectlyHandles_ResizedMap()
    {
        var largeMap = new BlitzMap<int, string>(4, 0.75);
        for (int i = 0; i < 100; i++)
        {
            largeMap.InsertOrUpdate(i, $"Value{i}");
        }
        Assert.True(largeMap.Get(99, out var value));
        Assert.Equal("Value99", value);
    }

    [Fact]
    public void InsertOrUpdate_MaintainsData_WhenUpdatingAfterResize()
    {
        var largeMap = new BlitzMap<int, string>(4, 0.75);
        for (int i = 0; i < 100; i++)
        {
            largeMap.InsertOrUpdate(i, $"Value{i}");
        }
        largeMap.InsertOrUpdate(50, "UpdatedValue50");
        Assert.True(largeMap.Get(50, out var value));
        Assert.Equal("UpdatedValue50", value);
    }

    [Fact]
    public void InsertOrUpdate_HandlesNullValues_Correctly()
    {
        _map.InsertOrUpdate(3, null);
        Assert.True(_map.Get(3, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void InsertOrUpdate_HandlesMaximumCapacityWithoutErrors()
    {
        for (int i = 0; i < 16; i++)
        {
            _map.InsertOrUpdate(i, $"Value{i}");
        }
        _map.InsertOrUpdate(15, "UpdatedValue15");
        Assert.True(_map.Get(15, out var value));
        Assert.Equal("UpdatedValue15", value);
    }
}