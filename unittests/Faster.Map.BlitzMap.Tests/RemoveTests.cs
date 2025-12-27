using Faster.Map.Core;
using Xunit;

namespace Faster.Map.BlitzMap.Tests;

public class RemoveTests
{
    private readonly BlitzMap<int, string> _map;

    public RemoveTests()
    {
        _map = new BlitzMap<int, string>(16, 0.75);
    }

    [Fact]
    public void RemovingOneEntry()
    {
        var map = new BlitzMap<uint, uint>(16, 0.8);
        map.Insert(50, 50);
        var result = map.Remove(50);
        Assert.True(result);
    }

    [Fact]

    public void RemovingBucket_ShouldReattachExistingChainPartOne()
    {
        var map = new BlitzMap<uint, uint>(16, 0.8);

        map.Insert(0xABCD0005, 1);
        map.Insert(0xABCD0015, 2);
        map.Insert(0xABCD0025, 3);
        map.Insert(0xABCD0035, 4);

        var result = map.Remove(0xABCD0015);

        Assert.True(result);

        var result1 = map.Get(0xABCD0015, out var v);
        var result2 = map.Get(0xABCD0025, out var v1);
        var result3 = map.Get(0xABCD0035, out var v2);
        Assert.True(result);
        Assert.False(result1);

        Assert.True(result2);
        Assert.True(result3);
        Assert.Equal(0u, v);
        Assert.Equal(3u, v1);
        Assert.Equal(4u, v2);

        result = map.Remove(0xABCD0025);

        Assert.True(result);
        result = map.Get(0xABCD0035, out v2);
        Assert.True(result);
        Assert.Equal(4u, v2);
    }

    [Fact]
    public void RemovingBucket_ShouldReattachExistingChainPartTwo()
    {
        var map = new BlitzMap<uint, uint>(16, 0.8);
        map.Insert(0xABCD0005, 1);
        map.Insert(0xABCD0015, 2);
        map.Insert(0xABCD0025, 3);
        map.Insert(0xABCD0035, 4);

        var result = map.Remove(0xABCD0025);

        Assert.True(result);
        result = map.Remove(0xABCD0035);
        Assert.True(result);
    }

    [Fact]
    public void RemovingBucket_ShouldReattachExistingChain_Part_Tree()
    {
        var map = new BlitzMap<uint, uint>(16, 0.8);
        map.Insert(0xABCD0005, 1);
        map.Insert(0xABCD0015, 2);
        map.Insert(0xABCD0025, 3);
        map.Insert(0xABCD0035, 4);

        var result = map.Remove(0xABCD0015);
        var result1 = map.Remove(0xABCD0025);
        var result2 = map.Get(0xABCD0035, out var x);
        Assert.True(result);
        Assert.True(result1);
        Assert.True(result2);
        Assert.Equal(4u, x);
    }

    [Fact]
    public void RemovingBucket_ShouldReattachExistingChainPartFour()
    {
        var map = new BlitzMap<uint, uint>(16, 0.8);
        map.Insert(0xABCD0005, 1);
        map.Insert(0xABCD0015, 2);
        map.Insert(0xABCD0025, 3);
        map.Insert(0xABCD0035, 4);

        map.Remove(0xABCD0015);
        map.Remove(0xABCD0025);
        map.Insert(0xABCD0025, 33);

        var result = map.Get(0xABCD0025, out var r);
        Assert.True(result);
        Assert.Equal(33u, r);
    }

    [Fact]
    public void Remove_ExistingKey_Success()
    {
        var map = new BlitzMap<int, string>(16, 0.75);
        map.Insert(1, "One");
        Assert.True(map.Remove(1));
        Assert.False(map.Get(1, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Remove_NonExistentKey_ReturnsFalse()
    {
        var map = new BlitzMap<int, string>(16, 0.75);
        Assert.False(map.Remove(99));
    }

    [Fact]
    public void Remove_FromEmptyMap_ReturnsFalse()
    {
        var map = new BlitzMap<int, string>(16, 0.75);
        Assert.False(map.Remove(1));
    }

    [Fact]
    public void Remove_AllElements_MapIsEmpty()
    {
        var map = new BlitzMap<int, string>(16, 0.75);
        map.Insert(1, "One");
        map.Insert(2, "Two");
        map.Remove(1);
        map.Remove(2);

        Assert.False(map.Get(1, out var value));
        Assert.False(map.Get(2, out value));
        Assert.Equal(0, map.Count);
    }

    [Fact]
    public void Remove_RepeatedRemovals_ReturnsFalse()
    {
        var map = new BlitzMap<int, string>(16, 0.75);
        map.Insert(1, "One");
        Assert.True(map.Remove(1));
        Assert.False(map.Remove(1));
    }

    [Fact]
    public void Remove_MultipleCollisions_Success()
    {
        var map = new BlitzMap<int, string>(16, 0.75);
        map.Insert(1, "One");
        map.Insert(17, "Seventeen");
        map.Insert(33, "ThirtyThree"); // Collisions in the same bucket chain

        Assert.True(map.Remove(17));
        Assert.False(map.Get(17, out var value));
        Assert.True(map.Get(1, out value));
        Assert.True(map.Get(33, out value));
    }

    [Fact]
    public void Remove_EntryAfterRehashing_Success()
    {
        var map = new BlitzMap<int, string>(16, 0.75);

        for (int i = 0; i < 20; i++)
        {
            map.Insert(i, "Value" + i);
        }
        Assert.True(map.Remove(10));
        Assert.False(map.Get(10, out var value));
    }

    [Fact]
    public void Remove_ReturnsFalse_WhenKeyDoesNotExist()
    {
        Assert.False(_map.Remove(99));
    }

    [Fact]
    public void Remove_HandlesExtremeValues_Correctly()
    {
        _map.Insert(int.MinValue, "MinValue");
        _map.Insert(int.MaxValue, "MaxValue");
        Assert.True(_map.Remove(int.MinValue));
        Assert.False(_map.Get(int.MinValue, out var v));
        Assert.True(_map.Remove(int.MaxValue));
        Assert.False(_map.Get(int.MaxValue, out var v1));
    }

    [Fact]
    public void Remove_MaintainsDataConsistency_AfterMultipleRemovals()
    {
        for (int i = 0; i < 10; i++)
        {
            _map.Insert(i, $"Value{i}");
        }
        _map.Remove(5);
        _map.Remove(9);
        Assert.False(_map.Get(5, out var value1));
        Assert.False(_map.Get(9, out var value2));
        Assert.True(_map.Get(1, out var existingValue));
        Assert.Equal("Value1", existingValue);
    }

    [Fact]
    public void Remove_ReturnsFalse_WhenMapIsEmpty()
    {
        Assert.False(_map.Remove(1));
    }

    [Fact]
    public void Remove_ShouldReturnTrue_WhenKeyExists()
    {
        _map.Insert(1, "value1");
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
        _map.Insert(1, "value1");

        bool firstRemove = _map.Remove(1);
        bool secondRemove = _map.Remove(1);

        Assert.True(firstRemove);
        Assert.False(secondRemove);
        Assert.False(_map.Contains(1));
    }

    [Fact]
    public void Remove_ShouldNotAffectOtherKeys_WhenKeyIsRemoved()
    {
        _map.Insert(1, "value1");
        _map.Insert(2, "value2");

        _map.Remove(1);

        Assert.False(_map.Contains(1));
        Assert.True(_map.Contains(2));
        Assert.Equal("value2", _map[2]);
    }

    [Fact]
    public void Remove_ShouldHandleMinAndMaxIntKeys()
    {
        _map.Insert(int.MinValue, "minValue");
        _map.Insert(int.MaxValue, "maxValue");

        _map.Remove(int.MinValue);

        Assert.False(_map.Contains(int.MinValue));
        Assert.True(_map.Contains(int.MaxValue));
        Assert.Equal("maxValue", _map[int.MaxValue]);
    }

    [Fact]
    public void Remove_ShouldReuseSlot_WhenKeyIsRemovedAndReinserted()
    {
        _map.Insert(1, "value1");
        _map.Remove(1);

        _map.Insert(1, "newValue");

        Assert.True(_map.Contains(1));
        Assert.Equal("newValue", _map[1]);
    }

    [Fact]
    public void Remove_ShouldHandleMultipleRemoveAndReinsertCycles()
    {
        _map.Insert(1, "value1");
        _map.Remove(1);

        _map.Insert(1, "value2");
        _map.Remove(1);

        _map.Insert(1, "finalValue");

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
            _map.Insert(key, $"value{key}");
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
        _map.Insert(0, "zeroValue");
        _map.Remove(0);

        Assert.False(_map.Contains(0));
    }

    [Fact]
    public void Remove_ShouldHandleNegativeKeys()
    {
        _map.Insert(-1, "negativeOne");
        _map.Insert(-2, "negativeTwo");

        _map.Remove(-1);

        Assert.False(_map.Contains(-1));
        Assert.True(_map.Contains(-2));
        Assert.Equal("negativeTwo", _map[-2]);
    }

    [Fact]
    public void Remove_ShouldNotAffectCount_AfterRemovingNonExistentKey()
    {
        _map.Insert(1, "value1");
        int initialCount = _map.Count;

        _map.Remove(42); // Key 42 does not exist

        Assert.Equal(initialCount, _map.Count);
    }

    [Fact]
    public void Remove_ShouldDecrementCount_WhenKeyIsRemoved()
    {
        _map.Insert(1, "value1");
        _map.Insert(2, "value2");
        int initialCount = _map.Count;

        _map.Remove(1);

        Assert.Equal(initialCount - 1, _map.Count);
    }

    [Fact]
    public void Remove_ShouldCorrectlyHandleBoundaryValues()
    {
        _map.Insert(int.MaxValue, "max");
        _map.Insert(int.MinValue, "min");

        _map.Remove(int.MaxValue);

        Assert.False(_map.Contains(int.MaxValue));
        Assert.True(_map.Contains(int.MinValue));
    }

    [Fact]
    public void Remove_ShouldFreeUpSlotForReinsertion_AfterBoundaryValuesAreRemoved()
    {
        _map.Insert(int.MaxValue, "max");
        _map.Remove(int.MaxValue);

        _map.Insert(int.MaxValue, "newMax");

        Assert.Equal("newMax", _map[int.MaxValue]);
    }

    [Fact]
    public void Remove_ShouldPreserveRemainingEntries_WhenRemovingMultipleKeys()
    {
        _map.Insert(1, "value1");
        _map.Insert(2, "value2");
        _map.Insert(3, "value3");

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
            _map.Insert(i, $"value{i}");
        }

        _map.Remove(5);
        _map.Remove(15);

        _map.Insert(5, "newValue5");
        _map.Insert(15, "newValue15");

        Assert.Equal("newValue5", _map[5]);
        Assert.Equal("newValue15", _map[15]);
    }

    [Fact]
    public void Remove_ShouldCorrectlyHandleRemoveAndReinsertionAtHighLoad()
    {
        for (int i = 0; i < 50; i++)
        {
            _map.Insert(i, $"value{i}");
        }

        _map.Remove(25);
        _map.Insert(25, "newValue25");

        Assert.Equal("newValue25", _map[25]);
    }

    [Fact]
    public void Remove_ShouldAllowReinsertionOfKey_WithoutConflictsOrDuplicates()
    {
        _map.Insert(10, "initialValue");
        _map.Remove(10);
        _map.Insert(10, "reinsertedValue");

        Assert.True(_map.Contains(10));
        Assert.Equal("reinsertedValue", _map[10]);
        Assert.Equal(1, _map.Count);
    }

    [Fact]
    public void Remove_ShouldMaintainCorrectState_AfterMultipleSequentialRemovalsAndReinsertions()
    {
        for (int i = 0; i < 10; i++)
        {
            _map.Insert(i, $"value{i}");
        }

        for (int i = 0; i < 5; i++)
        {
            _map.Remove(i);
        }

        for (int i = 5; i < 10; i++)
        {
            _map.Insert(i + 10, $"newValue{i + 10}");
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
            _map.Insert(i, $"value{i}");
            if (i % 2 == 0)
            {
                _map.Remove(i);
            }
        }

        for (int i = 0; i < 50; i++)
        {
            if (i % 2 == 0)
            {
                _map.Insert(i, $"reused{i}");
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
        _map.Insert(1, "value1");

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
            _map.Insert(i, $"value{i}");
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
            _map.Insert(i, $"value{i}");
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
        _map.Insert(1, "value1");
        _map.Insert(1000, "value1000");
        _map.Insert(2000, "value2000");

        // Remove non-consecutive keys and ensure sparse structure is maintained
        _map.Remove(1);
        _map.Remove(1000);

        Assert.False(_map.Contains(1));
        Assert.False(_map.Contains(1000));
        Assert.True(_map.Contains(2000));
        Assert.Equal("value2000", _map[2000]);
    }

    [Fact]
    public void Remove_ShouldHandleRemovingKeysWhenAllEntriesAreTombstoned()
    {
        // Insert keys and then remove all to create tombstones
        for (int i = 0; i < 10; i++)
        {
            _map.Insert(i, $"value{i}");
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
            _map.Insert(i, $"value{i}");
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

        _map.Insert(key1, "value1");
        _map.Insert(key2, "value2");
        _map.Insert(key3, "value3");

        // Remove a middle entry in the chain
        _map.Remove(key2);

        // Reinsert the removed key
        _map.Insert(key2, "newValue2");

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
            _map.Insert(i, $"value{i}");
        }

        int initialCount = _map.Count;

        for (int i = 0; i < 5; i++)
        {
            _map.Remove(i);
        }

        Assert.Equal(initialCount - 5, _map.Count); // Ensure count is updated accurately
    }

    [Fact]
    public void Remove_ShouldClearEntries_WhenRemovingAllKeys()
    {
        // Insert and then remove all entries
        for (int i = 0; i < 20; i++)
        {
            if (!_map.Insert(i, $"value{i}"))
            {
                Assert.Fail();
            }
        }

        Assert.Equal(20, _map.Count); // Verify map is empty


        for (int i = 0; i < 20; i++)
        {
            if (!_map.Remove(i))
            {
                Assert.Fail();
            };
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
        var map = new BlitzMap<int, string>();

        map.Insert(int.MinValue, "minValue");
        map.Insert(int.MaxValue, "maxValue");

        Assert.True(map.Remove(int.MinValue));
        Assert.False(map.Contains(int.MinValue));

        Assert.True(map.Remove(int.MaxValue));
        Assert.False(map.Contains(int.MaxValue));
    }

    [Fact]
    public void Remove_ShouldHandleNearCapacityWithoutErrors()
    {
        var map = new BlitzMap<int, string>(32);

        // Fill map close to capacity
        for (int i = 0; i < 28; i++)
        {
            map.Insert(i, $"value{i}");
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
        var map = new BlitzMap<int, string>(16, 0.9);

        // Perform multiple cycles of add/remove
        for (int cycle = 0; cycle < 100; cycle++)
        {
            // Add entries
            for (int i = 0; i < 10; i++)
            {
                map.Insert(i, $"cycle{cycle}-value{i}");
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
        // Initialize a small BlitzMap for easy rehash testing.
        var map = new BlitzMap<int, string>(16, 0.9);

        // Insert entries up to half the map size to allow room for tombstone testing.
        for (int i = 0; i < 8; i++)
        {
            map.Insert(i, $"value{i}");
        }

        // Remove entries to create tombstones.
        for (int i = 0; i < 6; i++)
        {
            map.Remove(i);
        }

        // Trigger a rehash by adding enough tombstones.
        // At this point, a rehash should occur based on the tombstone threshold.
        map.Insert(99, "value99");

        // Assert that the tombstone counter has been reset, indicating rehash.
        Assert.True(map.Count == 3); // Only remaining entries (7, 99, and any added during rehash).
    }

    [Fact]
    public void Rehash_ShouldRetainCorrectValues_AfterRemovingAndReinsertingKeys()
    {
        var map = new BlitzMap<int, string>(16);

        // Insert initial entries
        for (int i = 0; i < 10; i++)
        {
            map.Insert(i, $"value{i}");
        }

        // Remove a few entries to create tombstones
        map.Remove(3);
        map.Remove(6);
        map.Remove(9);

        // Insert additional entries to trigger rehash
        map.Insert(11, "value11");
        map.Insert(12, "value12");

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

    //[Fact]
    //public void RemoveLargeDateset()
    //{
    //    var Length = 134_217_728;
    //    var rnd = new FastRandom(3);
    //    var uni = new HashSet<uint>((int)Length * 2);
    //    while (uni.Count < (uint)(Length * 0.9))
    //    {
    //        uni.Add((uint)rnd.Next());
    //    }

    //    var map = new BlitzMap<uint, uint>(Length);
    //    var keys = uni.ToArray();

    //    for (int i = 0; i < keys.Length; i++)
    //    {
    //        var key = keys[i];
    //        map.Insert(key, key);
    //    }
        

    //    for (int i = 0; i < keys.Length; i++)
    //    {
    //        var key = keys[i];
    //        map.Remove(key);
    //    }
    //}

    [Fact]
    public void Remove_MiddleOfChain_PreservesLookup()
    {
        var map = new BlitzMap<int, string>(4);

        // Force collisions
        map.Insert(1, "A");
        map.Insert(5, "B"); // same bucket as 1 if mask=3
        map.Insert(9, "C");

        Assert.True(map.Remove(5));

        Assert.True(map.Contains(1));
        Assert.False(map.Contains(5));
        Assert.True(map.Contains(9));

        Assert.Equal("A", map[1]);
        Assert.Equal("C", map[9]);
    }

    [Fact]
    public void Remove_HeadOfChain_Works()
    {
        var map = new BlitzMap<int, int>(4);

        map.Insert(1, 10);
        map.Insert(5, 20);

        Assert.True(map.Remove(1));
        Assert.False(map.Contains(1));
        Assert.True(map.Contains(5));
        Assert.Equal(20, map[5]);
    }


    [Fact]
    public void Remove_TailOfChain_Works()
    {
        var map = new BlitzMap<int, int>(4);

        map.Insert(1, 10);
        map.Insert(5, 20);
        map.Insert(9, 30);

        Assert.True(map.Remove(9));

        Assert.True(map.Contains(1));
        Assert.True(map.Contains(5));
        Assert.False(map.Contains(9));
    }

    [Fact]
    public void Remove_ThenInsert_ReusesSlotCorrectly()
    {
        var map = new BlitzMap<int, int>(4);

        map.Insert(1, 1);
        map.Insert(5, 2);

        map.Remove(1);
        map.Insert(9, 3);

        Assert.False(map.Contains(1));
        Assert.True(map.Contains(5));
        Assert.True(map.Contains(9));
    }

    [Fact]
    public void Stress_CollisionsAndRemovals()
    {
        var map = new BlitzMap<int, int>(8);

        for (int i = 0; i < 100; i++)
            map.Insert(i * 4, i);

        for (int i = 0; i < 100; i++)
            Assert.True(map.Remove(i * 4));

        for (int i = 0; i < 100; i++)
            Assert.False(map.Get(i * 4, out var _));
    }

    [Fact]
    public void LongRunningFuzz()
    {
        var rnd = new Random();
        var map = new BlitzMap<int, int>();
        var dict = new Dictionary<int, int>();

        for (int i = 0; i < 100000; i++)
        {
            int k = rnd.Next(1000);
            int v = rnd.Next();

            switch (rnd.Next(4))
            {
                case 0:
                    map.InsertOrUpdate(k, v); dict[k] = v; break;
                case 1:                  
                    map.Remove(k); dict.Remove(k); break;
                case 2:
                    Assert.Equal(
                        dict.TryGetValue(k, out var dv),
                        map.Get(k, out var mv));
                    if (dict.ContainsKey(k)) Assert.Equal(dv, mv);
                    break;
                case 3:

                    Assert.Equal(dict.ContainsKey(k), map.Contains(k));
                    break;
            }
        }
    }

}