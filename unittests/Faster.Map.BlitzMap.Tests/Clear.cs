using Faster.Map.Concurrent;
using Xunit;

namespace Faster.Map.BlitzMap.Tests;

public class Clear
{
    private readonly BlitzMap<int, string> _map;

    public Clear()
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

        Assert.True(result);
        Assert.True(_map.Get(1, out var value));
        Assert.Equal("newValue1", value);
    }
}

