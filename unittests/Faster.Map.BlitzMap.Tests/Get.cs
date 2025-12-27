
using Faster.Map.Core;
using Faster.Map.Hasher;
using Faster.Map.Hashing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Faster.Map.BlitzMap.Tests;

public class Get
{
    private readonly BlitzMap<int, string, DefaultHasher<int>> _map;

    public Get()
    {
        _map = new BlitzMap<int, string, DefaultHasher<int>>(16, 0.75);
    }

    [Fact]
    public void Get_ReturnsTrueAndCorrectValue_WhenKeyExists()
    {
        _map.Insert(1, "One");
        Assert.True(_map.Get(1, out var value));
        Assert.Equal("One", value);
    }

    [Fact]
    public void Get_ReturnsFalseAndDefaultValue_WhenKeyDoesNotExist()
    {
        Assert.False(_map.Get(99, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Get_ReturnsFalseAndDefaultValue_WhenMapIsEmpty()
    {
        Assert.False(_map.Get(1, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Get_ReturnsTrue_WhenHandlingHashCollisions()
    {
        var key1 = new CollisionKey(1);
        var key2 = new CollisionKey(2);
        _map.Insert(key1.GetHashCode(), "First");
        _map.Insert(key2.GetHashCode(), "Second");
        Assert.True(_map.Get(key2.GetHashCode(), out var value));
        Assert.Equal("First", value);
    }

    [Fact]
    public void Get_HandlesInactiveBuckets_Correctly()
    {
        _map.Insert(1, "One");
        _map.Remove(1);
        Assert.False(_map.Get(1, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Get_HandlesExtremeValues_Correctly()
    {
        _map.Insert(int.MinValue, "MinValue");
        _map.Insert(int.MaxValue, "MaxValue");
        Assert.True(_map.Get(int.MinValue, out var minValue));
        Assert.Equal("MinValue", minValue);
        Assert.True(_map.Get(int.MaxValue, out var maxValue));
        Assert.Equal("MaxValue", maxValue);
    }

    [Fact]
    public void Get_ReturnsFalse_WhenMapIsFullAndKeyNotFound()
    {
        for (int i = 0; i < 16; i++)
        {
            _map.Insert(i, $"Value{i}");
        }
        Assert.False(_map.Get(99, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Get_CorrectlyHandles_SequentialKeys()
    {
        for (int i = 0; i < 16; i++)
        {
            _map.Insert(i, $"Value{i}");
        }
        for (int i = 0; i < 16; i++)
        {
            Assert.True(_map.Get(i, out var value));
            Assert.Equal($"Value{i}", value);
        }
    }

    [Fact]
    public void Get_CorrectlyHandles_ResizedMap()
    {
        var largeMap = new BlitzMap<int, string, DefaultHasher<int>>(4, 0.75);
        for (int i = 0; i < 100; i++)
        {
            largeMap.Insert(i, $"Value{i}");
        }
        Assert.True(largeMap.Get(99, out var value));
        Assert.Equal("Value99", value);
    }

    [Fact]
    public void ShouldReturnCorrectKeyValue()
    {
        var length = 80_000_000;
        var rng1 = new Random(3);

        var keys = new int[length];

        for (int i = 0; i < length; i++)
        {
            var key = rng1.Next();
            keys[i] = key;
        }

        var rng2 = new Random(3);
        var map = new BlitzMap<int, int, DefaultHasher<int>>(length, 0.9);

        for (int i = 0; i < length; i++)
        {
            if (i == 8)
            {

            }

            map.Insert(keys[i], i + 2);
        }


        for (int i = 0; i < length; i++)
        {
            var result = map.Get(keys[i], out var value);
            if (result == false)
            {
                Assert.Fail();
            }
        }

        //   Assert.Equal(12ul, result);
    }

    [Fact]
    public void Get_ShouldReturnTrueAndCorrectValue_WhenKeyExists()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>();
        map.Insert(1, "value1");
        Assert.True(map.Get(1, out var value));
        Assert.Equal("value1", value);
    }

    [Fact]
    public void Get_ShouldReturnFalseAndDefaultValue_WhenKeyDoesNotExist()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>();
        Assert.False(map.Get(99, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Get_ShouldReturnFalseAndDefaultValue_WhenMapIsEmpty()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>();
        Assert.False(map.Get(0, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Get_ShouldReturnCorrectValue_WhenKeyIsZero()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>();
        map.Insert(0, "zeroValue");
        Assert.True(map.Get(0, out var value));
        Assert.Equal("zeroValue", value);
    }

    [Fact]
    public void Get_ShouldReturnTrueAndCorrectValue_ForNegativeKeys()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>();
        map.Insert(-1, "negativeValue");
        Assert.True(map.Get(-1, out var value));
        Assert.Equal("negativeValue", value);
    }

    [Fact]
    public void Get_ShouldReturnDefault_WhenKeyIsRemoved()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>();
        map.Insert(1, "value1");
        map.Remove(1);
        Assert.False(map.Get(1, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Get_ShouldReturnUpdatedValue_WhenKeyIsReinsertedAfterRemoval()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>();
        map.Insert(1, "value1");
        map.Remove(1);
        map.Insert(1, "newValue1");
        Assert.True(map.Get(1, out var value));
        Assert.Equal("newValue1", value);
    }

    [Fact]
    public void Get_ShouldReturnCorrectValue_ForMinAndMaxIntKeys()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>();
        map.Insert(int.MinValue, "minValue");
        map.Insert(int.MaxValue, "maxValue");
        Assert.True(map.Get(int.MinValue, out var minValue));
        Assert.Equal("minValue", minValue);
        Assert.True(map.Get(int.MaxValue, out var maxValue));
        Assert.Equal("maxValue", maxValue);
    }

    [Fact]
    public void Get_ShouldReturnDefault_ForNonExistentKeyInHighLoadMap()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>(16, 0.9);
        for (int i = 0; i < 14; i++)
        {
            map.Insert(i, $"value{i}");
        }
        Assert.False(map.Get(99, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Get_ShouldWorkCorrectly_AfterMapResize()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>(4, 0.9);
        for (int i = 0; i < 5; i++)
        {
            map.Insert(i, $"value{i}");
        }
        Assert.True(map.Get(3, out var value));
        Assert.Equal("value3", value);
        Assert.True(map.Size > 4);
    }

    [Fact]
    public void Get_ShouldReturnDefault_AfterClear()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>();
        map.Insert(1, "value1");
        map.Clear();
        Assert.False(map.Get(1, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Get_ShouldReturnCorrectValue_ForCollidingKeys()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>();
        int baseKey = 100;
        int[] collidingKeys = { baseKey, baseKey + 16, baseKey + 32 };
        foreach (var key in collidingKeys)
        {
            map.Insert(key, $"value{key}");
        }
        foreach (var key in collidingKeys)
        {
            Assert.True(map.Get(key, out var value));
            Assert.Equal($"value{key}", value);
        }
    }

    [Fact]
    public void Get_ShouldReturnCorrectValue_AfterHighVolumeInsertions()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>(16, 0.9);
        for (int i = 0; i < 1000; i++)
        {
            map.Insert(i, $"value{i}");
        }
        Assert.True(map.Get(999, out var value));
        Assert.Equal("value999", value);
    }

    [Fact]
    public void Get_ShouldReturnCorrectValue_WhenKeyIsReinsertedAfterRemovalInCollisionChain()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>();

        int baseKey = 50;
        int[] collidingKeys = { baseKey, baseKey + 16, baseKey + 32 };

        foreach (var key in collidingKeys)
        {
            map.Insert(key, $"value{key}");
        }

        map.Remove(collidingKeys[2]);

        map.Insert(collidingKeys[2], "newValue");

        Assert.True(map.Get(collidingKeys[2], out var value));
        Assert.Equal("newValue", value);
    }

    [Fact]
    public void Get_ShouldReturnTrue_AfterReinsertionInTombstonedSlot()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>();
        map.Insert(1, "value1");
        map.Remove(1);
        map.Insert(1, "newValue");

        Assert.True(map.Get(1, out var value));
        Assert.Equal("newValue", value);
    }

    [Fact]
    public void Get_ShouldReturnFalse_ForNonExistentKeyAfterHighVolumeInsertionsAndRemovals()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>();
        for (int i = 0; i < 1000; i++)
        {
            map.Insert(i, $"value{i}");
        }
        map.Remove(500);
        Assert.False(map.Get(500, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Get_ShouldReturnFalse_ForRemovedKeyInHighLoadMap()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>(16, 0.9);
        for (int i = 0; i < 14; i++)
        {
            map.Insert(i, $"value{i}");
        }
        map.Remove(10);
        Assert.False(map.Get(10, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Get_ShouldReturnCorrectValue_WhenBoundaryKeysArePresent()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>();
        map.Insert(int.MinValue, "minValue");
        map.Insert(int.MaxValue, "maxValue");

        Assert.True(map.Get(int.MinValue, out var minValue));
        Assert.Equal("minValue", minValue);
        Assert.True(map.Get(int.MaxValue, out var maxValue));
        Assert.Equal("maxValue", maxValue);
    }

    [Fact]
    public void Get_ShouldReturnDefault_WhenKeyNotInMap()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>();
        map.Insert(1, "value1");
        Assert.False(map.Get(999, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Get_ShouldReturnCorrectValue_ForZeroKey()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>();
        map.Insert(0, "zeroValue");

        Assert.True(map.Get(0, out var value));
        Assert.Equal("zeroValue", value);
    }

    [Fact]
    public void Get_ShouldReturnCorrectValue_ForNegativeKey()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>();
        map.Insert(-10, "negativeValue");

        Assert.True(map.Get(-10, out var value));
        Assert.Equal("negativeValue", value);
    }

    [Fact]
    public void Get_ShouldReturnCorrectValue_WhenReinsertingInDifferentGroupsAfterRemoval()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>(32);

        // Insert multiple keys to different groups
        int[] keys = { 3, 19, 35 }; // Assuming these hash to different groups
        foreach (var key in keys)
        {
            map.Insert(key, $"value{key}");
        }

        // Remove a key and reinsert
        map.Remove(19);
        map.Insert(19, "newValue19");

        Assert.True(map.Get(19, out var value));
        Assert.Equal("newValue19", value);
    }

    [Fact]
    public void Get_ShouldReturnTrueForReinsertedKey_WhenUsingHighLoadFactorAndResizing()
    {
        var map = new BlitzMap<int, string, DefaultHasher<int>>(4, 0.9);
        for (int i = 0; i < 10; i++)
        {
            map.Insert(i, $"value{i}");
        }
        map.Remove(8);
        map.Insert(8, "newEight");
        Assert.True(map.Get(8, out var value));
        Assert.Equal("newEight", value);
    }

    [Fact]
    public void Get_ShouldReturn_WhileMapIsEmpty()
    {
        var dense = new BlitzMap<int, int, DefaultHasher<int>>();
        for (int i = 1; i <= 16; ++i)
        {
            dense.Insert(i, 0);
            dense.Remove(i);
        }

        var result = dense.Get(0, out int value);

        Assert.False(result);
    }
}

public class CollisionKey
{
    private readonly int _value;

    public CollisionKey(int value)
    {
        _value = value;
    }

    public override int GetHashCode()
    {
        return 1; // Force a collision
    }

    public override bool Equals(object obj)
    {
        return obj is CollisionKey other && _value == other._value;
    }
}
