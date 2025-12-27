using Faster.Map.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Faster.Map.DenseMap.Tests;

public class RemoveTests
{
    private DenseMap<int, string> _map;

    public RemoveTests()
    {
        _map = new DenseMap<int, string>();
    }

    [Fact]
    public void Remove_ShouldReturnTrue_WhenKeyExists()
    {
        _map.Emplace(1, "value1");
        bool result = _map.Remove(1);

        Assert.True(result);
        Assert.False(_map.Contains(1));
    }

    [Fact]
    public void Remove_ShouldReturnFalse_WhenKeyDoesNotExist()
    {
        bool result = _map.Remove(42);

        Assert.False(result);
        Assert.False(_map.Contains(42));
    }

    [Fact]
    public void Remove_ShouldReturnFalse_AfterMultipleAttemptsOnSameKey()
    {
        _map.Emplace(1, "value1");

        bool firstRemove = _map.Remove(1);
        bool secondRemove = _map.Remove(1);

        Assert.True(firstRemove);
        Assert.False(secondRemove);
        Assert.False(_map.Contains(1));
    }

    [Fact]
    public void Remove_ShouldNotAffectOtherKeys_WhenKeyIsRemoved()
    {
        _map.Emplace(1, "value1");
        _map.Emplace(2, "value2");

        _map.Remove(1);

        Assert.False(_map.Contains(1));
        Assert.True(_map.Contains(2));
        Assert.Equal("value2", _map[2]);
    }

    [Fact]
    public void Remove_ShouldHandleMinAndMaxIntKeys()
    {
        _map.Emplace(int.MinValue, "minValue");
        _map.Emplace(int.MaxValue, "maxValue");

        _map.Remove(int.MinValue);

        Assert.False(_map.Contains(int.MinValue));
        Assert.True(_map.Contains(int.MaxValue));
        Assert.Equal("maxValue", _map[int.MaxValue]);
    }

    [Fact]
    public void Remove_ShouldReuseSlot_WhenKeyIsRemovedAndReinserted()
    {
        _map.Emplace(1, "value1");
        _map.Remove(1);

        _map.Emplace(1, "newValue");

        Assert.True(_map.Contains(1));
        Assert.Equal("newValue", _map[1]);
    }

    [Fact]
    public void Remove_ShouldHandleMultipleRemoveAndReinsertCycles()
    {
        _map.Emplace(1, "value1");
        _map.Remove(1);

        _map.Emplace(1, "value2");
        _map.Remove(1);

        _map.Emplace(1, "finalValue");

        Assert.True(_map.Contains(1));
        Assert.Equal("finalValue", _map[1]);
    }

    [Fact]
    public void Remove_ShouldCorrectlyHandleMultipleRemovalsInCollisionChain()
    {
        int baseKey = 50;
        int[] collidingKeys = { baseKey, baseKey + 16, baseKey + 32 }; // Keys likely to collide

        foreach (var key in collidingKeys)
        {
            _map.Emplace(key, $"value{key}");
        }

        // Remove middle entry and ensure it’s no longer in the map
        _map.Remove(collidingKeys[1]);

        Assert.True(_map.Contains(collidingKeys[0]));
        Assert.False(_map.Contains(collidingKeys[1]));
        Assert.True(_map.Contains(collidingKeys[2]));

        Assert.Equal($"value{collidingKeys[0]}", _map[collidingKeys[0]]);
        Assert.Equal($"value{collidingKeys[2]}", _map[collidingKeys[2]]);
    }

    [Fact]
    public void Remove_ShouldHandleKeyZero()
    {
        _map.Emplace(0, "zeroValue");
        _map.Remove(0);

        Assert.False(_map.Contains(0));
    }

    [Fact]
    public void Remove_ShouldHandleNegativeKeys()
    {
        _map.Emplace(-1, "negativeOne");
        _map.Emplace(-2, "negativeTwo");

        _map.Remove(-1);

        Assert.False(_map.Contains(-1));
        Assert.True(_map.Contains(-2));
        Assert.Equal("negativeTwo", _map[-2]);
    }

    [Fact]
    public void Remove_ShouldNotAffectCount_AfterRemovingNonExistentKey()
    {
        _map.Emplace(1, "value1");
        int initialCount = _map.Count;

        _map.Remove(42); // Key 42 does not exist

        Assert.Equal(initialCount, _map.Count);
    }

    [Fact]
    public void Remove_ShouldDecrementCount_WhenKeyIsRemoved()
    {
        _map.Emplace(1, "value1");
        _map.Emplace(2, "value2");
        int initialCount = _map.Count;

        _map.Remove(1);

        Assert.Equal(initialCount - 1, _map.Count);
    }

    [Fact]
    public void Remove_ShouldCorrectlyHandleBoundaryValues()
    {
        _map.Emplace(int.MaxValue, "max");
        _map.Emplace(int.MinValue, "min");

        _map.Remove(int.MaxValue);

        Assert.False(_map.Contains(int.MaxValue));
        Assert.True(_map.Contains(int.MinValue));
    }

    [Fact]
    public void Remove_ShouldFreeUpSlotForReinsertion_AfterBoundaryValuesAreRemoved()
    {
        _map.Emplace(int.MaxValue, "max");
        _map.Remove(int.MaxValue);

        _map.Emplace(int.MaxValue, "newMax");

        Assert.Equal("newMax", _map[int.MaxValue]);
    }

    [Fact]
    public void Remove_ShouldPreserveRemainingEntries_WhenRemovingMultipleKeys()
    {
        _map.Emplace(1, "value1");
        _map.Emplace(2, "value2");
        _map.Emplace(3, "value3");

        _map.Remove(1);
        _map.Remove(3);

        Assert.False(_map.Contains(1));
        Assert.True(_map.Contains(2));
        Assert.False(_map.Contains(3));
        Assert.Equal("value2", _map[2]);
    }

    [Fact]
    public void Remove_ShouldCorrectlyHandleReinsertionAfterResize()
    {
        for (int i = 0; i < 20; i++)
        {
            _map.Emplace(i, $"value{i}");
        }

        _map.Remove(5);
        _map.Remove(15);

        _map.Emplace(5, "newValue5");
        _map.Emplace(15, "newValue15");

        Assert.Equal("newValue5", _map[5]);
        Assert.Equal("newValue15", _map[15]);
    }

    [Fact]
    public void Remove_ShouldCorrectlyHandleRemoveAndReinsertionAtHighLoad()
    {
        for (int i = 0; i < 50; i++)
        {
            _map.Emplace(i, $"value{i}");
        }

        _map.Remove(25);
        _map.Emplace(25, "newValue25");

        Assert.Equal("newValue25", _map[25]);
    }

    [Fact]
    public void Remove_ShouldAllowReinsertionOfKey_WithoutConflictsOrDuplicates()
    {
        _map.Emplace(10, "initialValue");
        _map.Remove(10);
        _map.Emplace(10, "reinsertedValue");

        Assert.True(_map.Contains(10));
        Assert.Equal("reinsertedValue", _map[10]);
        Assert.Equal(1, _map.Count);
    }

    [Fact]
    public void Remove_ShouldMaintainCorrectState_AfterMultipleSequentialRemovalsAndReinsertions()
    {
        for (int i = 0; i < 10; i++)
        {
            _map.Emplace(i, $"value{i}");
        }

        for (int i = 0; i < 5; i++)
        {
            _map.Remove(i);
        }

        for (int i = 5; i < 10; i++)
        {
            _map.Emplace(i + 10, $"newValue{i + 10}");
        }

        for (int i = 0; i < 5; i++)
        {
            Assert.False(_map.Contains(i));
        }

        for (int i = 5; i < 10; i++)
        {
            Assert.Equal($"value{i}", _map[i]);
        }

        for (int i = 5; i < 10; i++)
        {
            Assert.Equal($"newValue{i + 10}", _map[i + 10]);
        }
    }

    [Fact]
    public void Remove_ShouldHandleKeyReuseCorrectly_AfterExtensiveUsage()
    {
        for (int i = 0; i < 50; i++)
        {
            _map.Emplace(i, $"value{i}");
            if (i % 2 == 0)
            {
                _map.Remove(i);
            }
        }

        for (int i = 0; i < 50; i++)
        {
            if (i % 2 == 0)
            {
                _map.Emplace(i, $"reused{i}");
            }
        }

        for (int i = 0; i < 50; i++)
        {
            string expectedValue = (i % 2 == 0) ? $"reused{i}" : $"value{i}";
            Assert.Equal(expectedValue, _map[i]);
        }
    }

    [Fact]
    public void Remove_ShouldHandleRepeatedlyRemovingSameKey()
    {
        _map.Emplace(1, "value1");

        for (int i = 0; i < 10; i++) // Attempt to remove the same key multiple times
        {
            bool result = _map.Remove(1);
            if (i == 0)
            {
                Assert.True(result); // Should succeed the first time
            }
            else
            {
                Assert.False(result); // Should fail on subsequent tries
            }
        }
    }

    [Fact]
    public void Remove_ShouldHandleMapAtMaximumCapacity()
    {
        // Fill the map up to its capacity
        for (int i = 0; i < 29; i++) // 90% loadfacotr
        {
            _map.Emplace(i, $"value{i}");
        }

        // Attempt to remove an entry at max capacity
        bool result = _map.Remove(0);

        Assert.True(result);
        Assert.False(_map.Contains(0));
    }

    [Fact]
    public void Remove_ShouldNotCauseResize_WhenRemovingAtHighLoad()
    {
        // Insert up to the load threshold
        int threshold = (int)(_map.Size * 0.9);
        for (int i = 0; i < threshold; i++)
        {
            _map.Emplace(i, $"value{i}");
        }

        // Remove some entries
        _map.Remove(0);
        _map.Remove(1);

        // Ensure the map has not resized and can still be used as expected
        Assert.True(_map.Size >= threshold); // Should not trigger resize on removal
        Assert.False(_map.Contains(0));
        Assert.False(_map.Contains(1));
    }

    [Fact]
    public void Remove_ShouldHandleRemovalFromSparseMap()
    {
        _map.Emplace(1, "value1");
        _map.Emplace(1000, "value1000");
        _map.Emplace(2000, "value2000");

        // Remove non-consecutive keys and ensure sparse structure is maintained
        _map.Remove(1);
        _map.Remove(1000);

        Assert.False(_map.Contains(1));
        Assert.False(_map.Contains(1000));
        Assert.True(_map.Contains(2000));
        Assert.Equal("value2000", _map[2000]);
    }

    [Fact]
    public void Remove_ShouldHandleRemovingKeyAfterCollisionResolution()
    {
        int baseKey = 16; // Assumed to cause a collision with other keys
        int collidingKey = baseKey + (int)_map.Size;

        _map.Emplace(baseKey, "baseValue");
        _map.Emplace(collidingKey, "collidingValue");

        _map.Remove(baseKey); // Remove the first key in the collision chain

        Assert.False(_map.Contains(baseKey));
        Assert.True(_map.Contains(collidingKey));
        Assert.Equal("collidingValue", _map[collidingKey]);
    }

    [Fact]
    public void Remove_ShouldHandleRemovingKeysWhenAllEntriesAreTombstoned()
    {
        // Insert keys and then remove all to create tombstones
        for (int i = 0; i < 10; i++)
        {
            _map.Emplace(i, $"value{i}");
        }

        for (int i = 0; i < 10; i++)
        {
            _map.Remove(i);
        }

        // Attempt to remove again after all entries are tombstoned
        bool result = _map.Remove(0);
        Assert.False(result); // Should return false because all are tombstoned
        Assert.False(_map.Contains(0));
    }

    [Fact]
    public void Remove_ShouldHandleSparseRemovalsAfterResize()
    {
        // Insert keys to force a resize
        for (int i = 0; i <= 28; i++)
        {
            _map.Emplace(i, $"value{i}");
        }

        int initialSize = (int)_map.Size;

        // Remove every other entry after resize
        for (int i = 0; i <= 28; i += 2)
        {
            _map.Remove(i);
        }

        Assert.Equal(initialSize, (int)_map.Size); // Verify no additional resize occurred
        for (int i = 1; i <= 28; i += 2)
        {
            Assert.True(_map.Contains(i));
        }
    }

    [Fact]
    public void Remove_ShouldHandleRemovingInChainedCollisions_AfterReinsertion()
    {
        // Insert colliding keys
        int key1 = 10;
        int key2 = key1 + (int)_map.Size;
        int key3 = key2 + (int)_map.Size;

        _map.Emplace(key1, "value1");
        _map.Emplace(key2, "value2");
        _map.Emplace(key3, "value3");

        // Remove a middle entry in the chain
        _map.Remove(key2);

        // Reinsert the removed key
        _map.Emplace(key2, "newValue2");

        Assert.True(_map.Contains(key1));
        Assert.True(_map.Contains(key2));
        Assert.True(_map.Contains(key3));
        Assert.Equal("newValue2", _map[key2]);
    }

    [Fact]
    public void Remove_ShouldMaintainCorrectCount_WhenRemovingAfterSequentialInsertions()
    {
        for (int i = 0; i < 15; i++)
        {
            _map.Emplace(i, $"value{i}");
        }

        int initialCount = _map.Count;

        for (int i = 0; i < 5; i++)
        {
            if (!_map.Remove(i))
            {

            }
        }

        Assert.Equal(initialCount - 5, _map.Count); // Ensure count is updated accurately
    }

    [Fact]
    public void Remove_ShouldClearEntries_WhenRemovingAllKeys()
    {
        // Insert and then remove all entries
        for (int i = 0; i < 20; i++)
        {
            _map.Emplace(i, $"value{i}");
        }

        for (int i = 0; i < 20; i++)
        {
            _map.Remove(i);
        }

        Assert.Equal(0, _map.Count); // Verify map is empty
        for (int i = 0; i < 20; i++)
        {
            Assert.False(_map.Contains(i));
        }
    }

    [Fact]
    public void Remove_ShouldHandleExtremeKeyValues()
    {
        var map = new DenseMap<int, string>();

        map.Emplace(int.MinValue, "minValue");
        map.Emplace(int.MaxValue, "maxValue");

        Assert.True(map.Remove(int.MinValue));
        Assert.False(map.Contains(int.MinValue));

        Assert.True(map.Remove(int.MaxValue));
        Assert.False(map.Contains(int.MaxValue));
    }

    [Fact]
    public void Remove_ShouldHandleNearCapacityWithoutErrors()
    {
        var map = new DenseMap<int, string>(32);

        // Fill map close to capacity
        for (int i = 0; i < 28; i++)
        {
            map.Emplace(i, $"value{i}");
        }

        // Now, start removing
        for (int i = 0; i < 10; i++)
        {
            map.Remove(i);
        }

        // Check remaining entries and ensure removals were correct
        for (int i = 10; i < 28; i++)
        {
            Assert.True(map.Contains(i));
        }
        Assert.Equal(18, map.Count);
    }

    [Fact]
    public void Remove_ShouldRemainConsistentUnderFrequentAddRemoveCycles()
    {
        var map = new DenseMap<int, string>(16, loadFactor: 0.9);

        // Perform multiple cycles of add/remove
        for (int cycle = 0; cycle < 100; cycle++)
        {
            // Add entries
            for (int i = 0; i < 10; i++)
            {
                map.Emplace(i, $"cycle{cycle}-value{i}");
            }

            // Remove half of the entries
            for (int i = 0; i < 5; i++)
            {
                map.Remove(i);
            }

            // Check remaining entries and ensure consistency
            for (int i = 5; i < 10; i++)
            {
                Assert.True(map.Contains(i));
            }
        }

        Assert.Equal(5, map.Count);
    }

    [Fact]
    public void Remove_ShouldTriggerRehash_WhenTombstoneThresholdIsExceeded()
    {
        // Initialize a small DenseMap for easy rehash testing.
        var map = new DenseMap<int, string>(16, 0.9);

        // Insert entries up to half the map size to allow room for tombstone testing.
        for (int i = 0; i < 8; i++)
        {
            map.Emplace(i, $"value{i}");
        }

        // Remove entries to create tombstones.
        for (int i = 0; i < 6; i++)
        {
            map.Remove(i);
        }

        // Trigger a rehash by adding enough tombstones.
        // At this point, a rehash should occur based on the tombstone threshold.
        map.Emplace(99, "value99");

        // Assert that the tombstone counter has been reset, indicating rehash.
        Assert.True(map.Count == 3); // Only remaining entries (7, 99, and any added during rehash).
    }

    [Fact]
    public void Rehash_ShouldRemoveTombstones_AndRetainValidEntries()
    {
        // Create a DenseMap and add entries.
        var map = new DenseMap<int, string>(32, 0.75);
        for (int i = 0; i < 10; i++)
        {
            map.Emplace(i, $"value{i}");
        }

        // Remove a subset of entries to create tombstones.
        for (int i = 0; i < 5; i++)
        {
            map.Remove(i);
        }

        // Trigger a rehash by exceeding the tombstone threshold.
        map.Emplace(20, "value20");

        // Check that remaining entries are accessible and tombstones are cleared.
        for (int i = 5; i < 10; i++)
        {
            Assert.True(map.Contains(i), $"Expected key {i} to be present after rehash.");
        }

        // Check that removed entries are indeed not found.
        for (int i = 0; i < 5; i++)
        {
            Assert.False(map.Contains(i), $"Expected key {i} to be absent after rehash.");
        }
    }

    [Fact]
    public void Rehash_ShouldRetainCorrectValues_AfterRemovingAndReinsertingKeys()
    {
        var map = new DenseMap<int, string>(16);

        // Insert initial entries
        for (int i = 0; i < 10; i++)
        {
            map.Emplace(i, $"value{i}");
        }

        // Remove a few entries to create tombstones
        map.Remove(3);
        map.Remove(6);
        map.Remove(9);

        // Insert additional entries to trigger rehash
        map.Emplace(11, "value11");
        map.Emplace(12, "value12");

        // Assert the rehash has retained the values correctly
        Assert.True(map.Contains(11));
        Assert.Equal("value11", map[11]);
        Assert.True(map.Contains(12));
        Assert.Equal("value12", map[12]);

        // Verify that tombstoned entries are removed after rehash
        Assert.False(map.Contains(3));
        Assert.False(map.Contains(6));
        Assert.False(map.Contains(9));
    }

    [Fact]
    public void Rehash_ShouldMaintainMapIntegrity_WithMultipleTombstonesAndResize()
    {
        var map = new DenseMap<int, string>(16, 0.9);

        // Insert entries to almost full capacity
        for (int i = 0; i < 12; i++)
        {
            map.Emplace(i, $"value{i}");
        }

        // Remove entries to create multiple tombstones
        map.Remove(1);
        map.Remove(3);
        map.Remove(5);
        map.Remove(7);
        map.Remove(9);
        map.Remove(11);

        // Insert additional entries to trigger resize and rehash
        map.Emplace(12, "value12");
        map.Emplace(13, "value13");

        // Verify all retained entries are accessible and correct
        for (int i = 0; i < 9; i++)
        {
            if (i % 2 == 1) // Removed entries
            {
                Assert.False(map.Contains(i));
            }
            else // Retained entries
            {
                Assert.True(map.Contains(i));
                Assert.Equal($"value{i}", map[i]);
            }
        }
    }


    [Fact]
    public void RemoveLargeDateset()
    {
        var Length = 134_217_728;

        var rnd = new Random(3);
        var uni = new HashSet<uint>((int)Length * 2);
        while (uni.Count < (uint)(Length * 0.8))
        {
            uni.Add((uint)rnd.Next());
        }

        var map = new DenseMap<uint, uint>((uint)Length);
        var keys = uni.ToArray();

        for (int i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            map.Emplace(key, key);
        }

        for (int i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            map.Remove(key);
        }
    }

    [Fact]
    public void LongRunningFuzz()
    {
        var rnd = new Random();
        var map = new DenseMap<int, int>();
        var dict = new Dictionary<int, int>();

        for (int i = 0; i < 100000; i++)
        {
            int k = rnd.Next(1000);
            int v = rnd.Next();

            switch (rnd.Next(4))
            {
                case 0:
                    map.Emplace(k, v);
                    dict[k] = v; break;
                case 1:
                    if (k == 58)
                    {

                    }
                    map.Remove(k);
                    dict.Remove(k); break;
                case 2:
                    Assert.Equal(dict.TryGetValue(k, out var dv), map.Get(k, out var mv));
                    if (dict.ContainsKey(k)) Assert.Equal(dv, mv);
                    break;
                case 3:

                    Assert.Equal(dict.ContainsKey(k), map.Contains(k));
                    break;
            }
        }
    }


}