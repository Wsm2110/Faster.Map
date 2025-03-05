using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static Faster.Map.BlitzMap.Tests.Get;

namespace Faster.Map.BlitzMap.Tests;

public class Update
{
    private readonly BlitzMap<int, string> _map;

    public Update()
    {
        _map = new BlitzMap<int, string>(16, 0.75);
    }

    [Fact]
    public void Update_UpdatesValue_WhenKeyExists()
    {
        _map.Insert(1, "Initial");
        Assert.True(_map.Update(1, "Updated"));
        Assert.True(_map.Get(1, out var value));
        Assert.Equal("Updated", value);
    }

    [Fact]
    public void Update_ReturnsFalse_WhenKeyDoesNotExist()
    {
        Assert.False(_map.Update(99, "NonExistent"));
    }

    [Fact]
    public void Update_HandlesExtremeValues_Correctly()
    {
        _map.Insert(int.MinValue, "MinValue");
        _map.Insert(int.MaxValue, "MaxValue");
        Assert.True(_map.Update(int.MinValue, "UpdatedMin"));
        Assert.True(_map.Update(int.MaxValue, "UpdatedMax"));
        Assert.True(_map.Get(int.MinValue, out var minValue));
        Assert.Equal("UpdatedMin", minValue);
        Assert.True(_map.Get(int.MaxValue, out var maxValue));
        Assert.Equal("UpdatedMax", maxValue);
    }

    [Fact]
    public void Update_CorrectlyHandles_HashCollisions()
    {
        var key1 = new CollisionKey(1);
        var key2 = new CollisionKey(2);
        _map.Insert(key1.GetHashCode(), "First");
        _map.Insert(key2.GetHashCode(), "Second");
        Assert.True(_map.Update(key1.GetHashCode(), "UpdatedFirst"));
        Assert.True(_map.Get(key1.GetHashCode(), out var value));
        Assert.Equal("UpdatedFirst", value);
    }

    [Fact]
    public void Update_HandlesNullValues_Correctly()
    {
        _map.Insert(1, "Initial");
        Assert.True(_map.Update(1, null));
        Assert.True(_map.Get(1, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Update_HandlesEmptyValues_Correctly()
    {
        _map.Insert(1, "Initial");
        Assert.True(_map.Update(1, string.Empty));
        Assert.True(_map.Get(1, out var value));
        Assert.Equal(string.Empty, value);
    }

    [Fact]
    public void Update_HandlesSpecialCharacters_InValues()
    {
        _map.Insert(1, "Initial");
        Assert.True(_map.Update(1, "Spécîål Chåräçtęrś"));
        Assert.True(_map.Get(1, out var value));
        Assert.Equal("Spécîål Chåräçtęrś", value);
    }

    [Fact]
    public void Update_HandlesMaximumCapacity_WithoutErrors()
    {
        for (int i = 0; i < 16; i++)
        {
            _map.Insert(i, $"Value{i}");
        }
        Assert.True(_map.Update(15, "UpdatedMax"));
        Assert.True(_map.Get(15, out var value));
        Assert.Equal("UpdatedMax", value);
    }

    [Fact]
    public void Update_MaintainsDataConsistency_AfterResize()
    {
        var largeMap = new BlitzMap<int, string>(4, 0.75);
        for (int i = 0; i < 100; i++)
        {
            largeMap.Insert(i, $"Value{i}");
        }
        Assert.True(largeMap.Update(50, "UpdatedValue50"));
        Assert.True(largeMap.Get(50, out var value));
        Assert.Equal("UpdatedValue50", value);
    }

    [Fact]
    public void Update_ShouldUpdateValue_WhenKeyExists()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "initialValue");
        bool updated = map.Update(1, "newValue");

        Assert.True(updated);
        Assert.Equal("newValue", map[1]);
    }

    [Fact]
    public void Update_ShouldReturnFalse_WhenKeyDoesNotExist()
    {
        var map = new BlitzMap<int, string>();
        bool updated = map.Update(99, "newValue");

        Assert.False(updated);
    }

    [Fact]
    public void Update_ShouldHandleMinAndMaxIntKeys()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(int.MinValue, "minValue");
        map.Insert(int.MaxValue, "maxValue");

        bool minUpdated = map.Update(int.MinValue, "updatedMinValue");
        bool maxUpdated = map.Update(int.MaxValue, "updatedMaxValue");

        Assert.True(minUpdated);
        Assert.True(maxUpdated);
        Assert.Equal("updatedMinValue", map[int.MinValue]);
        Assert.Equal("updatedMaxValue", map[int.MaxValue]);
    }

    [Fact]
    public void Update_ShouldReturnFalse_OnEmptyMap()
    {
        var map = new BlitzMap<int, string>(16);
        bool updated = map.Update(1, "value");

        Assert.False(updated);
    }

    [Fact]
    public void Update_ShouldHandleNullValues()
    {
        var map = new BlitzMap<int, string?>(16);
        map.Insert(1, "initialValue");

        bool updated = map.Update(1, null);

        Assert.True(updated);
        Assert.Null(map[1]);
    }

    [Fact]
    public void Update_ShouldHandleZeroKey()
    {
        var map = new BlitzMap<int, string>(16);
        map.Insert(0, "initialValue");

        bool updated = map.Update(0, "updatedValue");

        Assert.True(updated);
        Assert.Equal("updatedValue", map[0]);
    }

    [Fact]
    public void Update_ShouldWorkNearLoadFactorLimit()
    {
        var map = new BlitzMap<int, string>(16, 0.9);

        for (int i = 0; i < 14; i++)
        {
            map.Insert(i, $"value{i}");
        }

        bool updated = map.Update(5, "updatedValue");

        Assert.True(updated);
        Assert.Equal("updatedValue", map[5]);
    }

    [Fact]
    public void Update_ShouldReuseTombstonedSlot_WhenKeyReinserted()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "initialValue");
        map.Remove(1);
        map.Insert(1, "newValue");

        bool updated = map.Update(1, "updatedAgain");

        Assert.True(updated);
        Assert.Equal("updatedAgain", map[1]);
    }

    [Fact]
    public void Update_ShouldHandleHighCollisionScenario()
    {
        var map = new BlitzMap<int, string>();
        int baseKey = 100;
        int[] collidingKeys = { baseKey, baseKey + 16, baseKey + 32 };

        foreach (var key in collidingKeys)
        {
            map.Insert(key, $"value{key}");
        }

        bool updated = map.Update(collidingKeys[1], "updatedValue");

        Assert.True(updated);
        Assert.Equal("updatedValue", map[collidingKeys[1]]);
    }

    [Fact]
    public void Update_ShouldNotAffectOtherEntries_InCollisionChain()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "value1");
        map.Insert(2, "value2");
        map.Insert(17, "value17"); // Assuming it causes a collision with key 1

        bool updated = map.Update(1, "newValue1");

        Assert.True(updated);
        Assert.Equal("newValue1", map[1]);
        Assert.Equal("value2", map[2]);
        Assert.Equal("value17", map[17]);
    }

    [Fact]
    public void Update_ShouldReturnFalse_WhenTryingToUpdateTombstonedEntry()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "value1");
        map.Remove(1);

        bool updated = map.Update(1, "newValue");

        Assert.False(updated);
    }

    [Fact]
    public void Update_ShouldHandleNegativeKeys()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(-1, "negativeValue");

        bool updated = map.Update(-1, "updatedNegativeValue");

        Assert.True(updated);
        Assert.Equal("updatedNegativeValue", map[-1]);
    }

    [Fact]
    public void Update_ShouldReturnTrue_WhenUpdatingKeyInCollisionGroup()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "value1");
        map.Insert(17, "value17"); // Assuming collision with key 1

        bool updated = map.Update(17, "updatedValue17");

        Assert.True(updated);
        Assert.Equal("updatedValue17", map[17]);
    }

    [Fact]
    public void Update_ShouldHandleMultipleUpdateOperations_OnSameKey()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "initialValue");

        map.Update(1, "updatedValue1");
        map.Update(1, "updatedValue2");

        Assert.Equal("updatedValue2", map[1]);
    }

    [Fact]
    public void Update_ShouldHandleResizeCorrectly()
    {
        var map = new BlitzMap<int, string>(4, 0.9);

        for (int i = 0; i < 5; i++)
        {
            map.Insert(i, $"value{i}");
        }

        bool updated = map.Update(3, "updatedValue");

        Assert.True(updated);
        Assert.Equal("updatedValue", map[3]);
        Assert.True(map.Size > 4);
    }

    [Fact]
    public void Update_ShouldReturnFalse_WhenCalledAfterClear()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "value1");

        map.Clear();

        bool updated = map.Update(1, "newValue");

        Assert.False(updated);
    }

    [Fact]
    public void Update_ShouldNotReinsertRemovedKey()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "value1");
        map.Remove(1);

        bool updated = map.Update(1, "newValue");

        Assert.False(updated);
        Assert.False(map.Contains(1));
    }

    [Fact]
    public void Update_ShouldHandleReinsertedKey_AfterBeingRemovedAndReinserted()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "initialValue");
        map.Remove(1);
        map.Insert(1, "newValue");

        bool updated = map.Update(1, "finalValue");

        Assert.True(updated);
        Assert.Equal("finalValue", map[1]);
    }

    [Fact]
    public void Update_ShouldNotAffectAdjacentKeys_WhenUpdatingKeyInHighLoadMap()
    {
        var map = new BlitzMap<int, string>(32);

        for (int i = 0; i < 28; i++)
        {
            map.Insert(i, $"value{i}");
        }

        map.Update(4, "updatedValue4");

        Assert.Equal("updatedValue4", map[4]);
        Assert.Equal("value5", map[5]);
    }

    [Fact]
    public void Update_ShouldReturnFalse_WhenKeyIsNotPresentInHighLoadMap()
    {
        var map = new BlitzMap<int, string>(32);

        for (int i = 0; i < 28; i++)
        {
            map.Insert(i, $"value{i}");
        }

        bool updated = map.Update(29, "newValue");

        Assert.False(updated);
    }

    [Fact]
    public void Update_ShouldReturnFalse_WhenAttemptingToUpdateKeyInClearedMap()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "initialValue");
        map.Clear();

        bool updated = map.Update(1, "newValue");

        Assert.False(updated);
    }

    [Fact]
    public void Update_ShouldWorkCorrectly_WhenUpdatingValueToEmptyString()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "initialValue");

        bool updated = map.Update(1, "");

        Assert.True(updated);
        Assert.Equal("", map[1]);
    }

    [Fact]
    public void Update_ShouldHandleHighVolumeOfUpdates()
    {
        var map = new BlitzMap<int, string>(16, 0.9);

        for (int i = 0; i < 64; i++)
        {
            map.Insert(i, $"initialValue{i}");
        }

        for (int i = 0; i < 64; i++)
        {
            bool updated = map.Update(i, $"updatedValue{i}");
            Assert.True(updated);
            Assert.Equal($"updatedValue{i}", map[i]);
        }
    }
}
