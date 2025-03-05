﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Faster.Map.BlitzMap.Tests;

public class ContainsTests
{
    [Fact]
    public void Contains_ShouldReturnTrue_WhenKeyExists()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "value1");

        Assert.True(map.Contains(1));
    }

    [Fact]
    public void Contains_ShouldReturnFalse_WhenKeyDoesNotExist()
    {
        var map = new BlitzMap<int, string>();

        Assert.False(map.Contains(99));
    }

    [Fact]
    public void Contains_ShouldReturnFalse_WhenMapIsEmpty()
    {
        var map = new BlitzMap<int, string>();

        Assert.False(map.Contains(0));
    }

    [Fact]
    public void Contains_ShouldHandleMinAndMaxIntKeys()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(int.MinValue, "minValue");
        map.Insert(int.MaxValue, "maxValue");

        Assert.True(map.Contains(int.MinValue));
        Assert.True(map.Contains(int.MaxValue));
    }

    [Fact]
    public void Contains_ShouldReturnTrue_WhenKeyIsZero()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(0, "zeroValue");

        Assert.True(map.Contains(0));
    }

    [Fact]
    public void Contains_ShouldReturnFalse_AfterKeyIsRemoved()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "value1");
        map.Remove(1);

        Assert.False(map.Contains(1));
    }

    [Fact]
    public void Contains_ShouldHandleNegativeKeys()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(-1, "negativeValue");

        Assert.True(map.Contains(-1));
    }

    [Fact]
    public void Contains_ShouldWorkAtHighLoadFactor()
    {
        var map = new BlitzMap<int, string>(16, 0.9);

        for (int i = 0; i < 14; i++)
        {
            map.Insert(i, $"value{i}");
        }

        Assert.True(map.Contains(5));
        Assert.False(map.Contains(99)); // Non-existent key
    }

    [Fact]
    public void Contains_ShouldReturnFalse_ForTombstonedEntry()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "value1");
        map.Remove(1);

        Assert.False(map.Contains(1));
    }

    [Fact]
    public void Contains_ShouldReturnTrue_ForCollidingKeys()
    {
        var map = new BlitzMap<int, string>();
        int baseKey = 100;
        int[] collidingKeys = { baseKey, baseKey + 16, baseKey + 32 };

        foreach (var key in collidingKeys)
        {
            map.Insert(key, $"value{key}");
        }

        Assert.True(map.Contains(collidingKeys[0]));
        Assert.True(map.Contains(collidingKeys[1]));
        Assert.True(map.Contains(collidingKeys[2]));
    }

    [Fact]
    public void Contains_ShouldReturnFalse_WhenKeyIsNotInCollisionGroup()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "value1");
        map.Insert(17, "value17"); // Assumes it collides with key 1

        Assert.False(map.Contains(99)); // Non-colliding, non-existent key
    }

    [Fact]
    public void Contains_ShouldWorkAfterResize()
    {
        var map = new BlitzMap<int, string>(4, 0.9);

        for (int i = 0; i < 5; i++)
        {
            map.Insert(i, $"value{i}");
        }

        Assert.True(map.Contains(3));
        Assert.True(map.Size > 4);
    }

    [Fact]
    public void Contains_ShouldReturnFalse_WhenCalledAfterClear()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "value1");

        map.Clear();

        Assert.False(map.Contains(1));
    }

    [Fact]
    public void Contains_ShouldReturnFalse_ForRemovedKeyInHighLoadMap()
    {
        var map = new BlitzMap<int, string>(32);

        for (int i = 0; i < 28; i++)
        {
            map.Insert(i, $"value{i}");
        }

        map.Remove(4);

        Assert.False(map.Contains(4));
    }

    [Fact]
    public void Contains_ShouldReturnTrue_AfterReinsertionOfPreviouslyRemovedKey()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "value1");
        map.Remove(1);
        map.Insert(1, "newValue");

        Assert.True(map.Contains(1));
    }

    [Fact]
    public void Contains_ShouldReturnTrue_AfterHighVolumeInsertions()
    {
        var map = new BlitzMap<int, string>(16, 0.9);

        for (int i = 0; i < 1000; i++)
        {
            map.Insert(i, $"value{i}");
        }

        Assert.True(map.Contains(999));
    }

    [Fact]
    public void Contains_ShouldReturnFalse_ForOutOfRangeKey()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "value1");

        Assert.False(map.Contains(int.MaxValue));
    }

    [Fact]
    public void Contains_ShouldHandleMixedInsertAndRemove()
    {
        var map = new BlitzMap<int, string>();
        for (int i = 0; i < 10; i++)
        {
            map.Insert(i, $"value{i}");
        }

        map.Remove(5);
        map.Remove(7);
        map.Insert(5, "newValue5");

        Assert.True(map.Contains(5));
        Assert.False(map.Contains(7));
    }

    [Fact]
    public void Contains_ShouldHandleUpdateAfterRemove()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "value1");
        map.Remove(1);
        map.Insert(1, "newValue");

        Assert.True(map.Contains(1));
        Assert.Equal("newValue", map[1]);
    }

    [Fact]
    public void Contains_ShouldReturnTrue_WhenKeyIsReinsertedAfterRemoval()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "value1");
        map.Remove(1);
        map.Insert(1, "newValue1");
        Assert.True(map.Contains(1));
    }

    [Fact]
    public void Contains_ShouldReturnTrue_WhenMinAndMaxIntKeysArePresent()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(int.MinValue, "minValue");
        map.Insert(int.MaxValue, "maxValue");
        Assert.True(map.Contains(int.MinValue));
        Assert.True(map.Contains(int.MaxValue));
    }

    [Fact]
    public void Contains_ShouldReturnFalse_ForRemovedTombstonedKey()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "value1");
        map.Remove(1);
        Assert.False(map.Contains(1));
    }

    [Fact]
    public void Contains_ShouldReturnTrue_ForKeysInHighLoadMap()
    {
        var map = new BlitzMap<int, string>(16, 0.9);
        for (int i = 0; i < 14; i++)
        {
            map.Insert(i, $"value{i}");
        }
        Assert.True(map.Contains(5));
        Assert.False(map.Contains(99));
    }

    [Fact]
    public void Contains_ShouldWorkCorrectly_AfterResize()
    {
        var map = new BlitzMap<int, string>(4, 0.9);
        for (int i = 0; i < 5; i++)
        {
            map.Insert(i, $"value{i}");
        }
        Assert.True(map.Contains(3));
        Assert.True(map.Size > 4);
    }

    [Fact]
    public void Contains_ShouldReturnFalse_AfterClear()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "value1");
        map.Clear();
        Assert.False(map.Contains(1));
    }

    [Fact]
    public void Contains_ShouldReturnFalse_ForKeyNotInCollisionGroup()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "value1");
        map.Insert(17, "value17");
        Assert.False(map.Contains(99));
    }

    [Fact]
    public void Contains_ShouldHandleMixedInsertRemove()
    {
        var map = new BlitzMap<int, string>();
        for (int i = 0; i < 10; i++)
        {
            map.Insert(i, $"value{i}");
        }
        map.Remove(5);
        map.Remove(7);
        map.Insert(5, "newValue5");
        Assert.True(map.Contains(5));
        Assert.False(map.Contains(7));
    }

    [Fact]
    public void Contains_ShouldReturnFalse_ForReinsertedAndUpdatedRemovedKey()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "value1");
        map.Remove(1);
        map.Insert(1, "newValue");
        Assert.True(map.Contains(1));
        Assert.Equal("newValue", map[1]);
    }

    [Fact]
    public void Contains_ShouldReturnTrue_ForBoundaryConditions()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(int.MinValue, "minValue");
        map.Insert(int.MaxValue, "maxValue");
        Assert.True(map.Contains(int.MinValue));
        Assert.True(map.Contains(int.MaxValue));
    }

    [Fact]
    public void Contains_ShouldReturnFalse_ForNonExistentLargeKey()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "value1");
        Assert.False(map.Contains(int.MaxValue));
    }

    [Fact]
    public void Contains_ShouldReturnFalse_AfterMultipleInsertRemoveCycles()
    {
        var map = new BlitzMap<int, string>();
        for (int i = 0; i < 10; i++)
        {
            map.Insert(i, $"value{i}");
        }
        for (int i = 0; i < 5; i++)
        {
            map.Remove(i);
        }
        Assert.All(Enumerable.Range(0, 5), i => Assert.False(map.Contains(i)));
    }

    [Fact]
    public void Contains_ShouldReturnTrue_WhenReinsertingKeyInTombstonedSlot()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "value1");
        map.Remove(1);
        map.Insert(1, "newValue");
        Assert.True(map.Contains(1));
        Assert.Equal("newValue", map[1]);
    }

    [Fact]
    public void Contains_ShouldReturnTrue_WhenKeyIsPresentAfterResize()
    {
        var map = new BlitzMap<int, string>(8);
        for (int i = 0; i < 12; i++)
        {
            map.Insert(i, $"value{i}");
        }
        Assert.True(map.Contains(11));
    }

    [Fact]
    public void Contains_ShouldReturnFalse_ForEmptyMapAfterResize()
    {
        var map = new BlitzMap<int, string>(8);
        for (int i = 0; i < 12; i++)
        {
            map.Insert(i, $"value{i}");
        }
        map.Clear();
        Assert.False(map.Contains(11));
    }

    [Fact]
    public void Contains_ShouldReturnFalse_ForNewlyRemovedHighVolumeKey()
    {
        var map = new BlitzMap<int, string>();
        for (int i = 0; i < 1000; i++)
        {
            map.Insert(i, $"value{i}");
        }
        map.Remove(500);
        Assert.False(map.Contains(500));
    }

    [Fact]
    public void Contains_ShouldHandleEdgeCaseWithNegativeAndPositiveBoundaryKeys()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(int.MinValue, "minValue");
        map.Insert(int.MaxValue, "maxValue");
        Assert.True(map.Contains(int.MinValue));
        Assert.True(map.Contains(int.MaxValue));
    }

    [Fact]
    public void Contains_ShouldReturnTrue_ForAllKeysAfterMixedInsertionsAndRemovals()
    {
        var map = new BlitzMap<int, string>();
        for (int i = -10; i <= 10; i++)
        {
            map.Insert(i, $"value{i}");
        }
        map.Remove(-5);
        map.Remove(5);
        map.Insert(-5, "newNegFive");
        Assert.True(map.Contains(-5));
        Assert.True(map.Contains(4));
        Assert.False(map.Contains(5));
    }

    [Fact]
    public void Contains_ShouldReturnTrue_WhenReAddingPreviouslyRemovedKeyAfterMapResize()
    {
        var map = new BlitzMap<int, string>(4, 0.9);
        for (int i = 0; i < 10; i++)
        {
            map.Insert(i, $"value{i}");
        }
        map.Remove(8);
        map.Insert(8, "newEight");
        Assert.True(map.Contains(8));
        Assert.Equal("newEight", map[8]);
    }
}
