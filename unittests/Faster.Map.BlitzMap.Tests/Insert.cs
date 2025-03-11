using Faster.Map.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

namespace Faster.Map.BlitzMap.Tests;
public class InsertTests
{

    private readonly BlitzMap<int, string> _map;

    public InsertTests()
    {
        _map = new BlitzMap<int, string>(16, 0.75);
    }

    [Fact]
    public void Assert_HashTotheSameBucket_ShouldResolveAccordingly()
    {
        var map = new BlitzMap<uint, uint>(16, 0.8);
        map.Insert(0xABCD0005, 0);
        map.Insert(0xABCD0015, 0);
        map.Insert(0xABCD0025, 0);
        map.Insert(0xABCD0035, 0);

        Assert.True(map.Count == 4);

        // will perform a kickout, atleast it should
        map.Insert(0b01110111, 0);
    }


    [Fact]
    public void Assert_HashToBucketWHileHomeBucketIsTaken_ShouldKickoutBucket()
    {
        var map = new BlitzMap<uint, uint>(16, 0.8);
        map.Insert(0xABCD0005, 0);
        map.Insert(0xABCD0015, 0);
        map.Insert(0xABCD0025, 0);
        map.Insert(0xABCD0035, 0);

        Assert.True(map.Count == 4);

        map.Insert(0xABCD0000, 0);
    }

    [Fact]
    public void Insert_Should_Add_New_Entry()
    {
        // Arrange
        var map = new BlitzMap<uint, uint>(16, 0.8);

        // Act
        bool inserted = map.Insert(0xABCD0005, 100);

        // Assert
        Assert.True(inserted);
        Assert.Equal(1, map.Count);
    }

    [Fact]
    public void Insert_Should_Reject_Duplicate_Key()
    {
        // Arrange
        var map = new BlitzMap<uint, uint>(16, 0.8);
        map.Insert(0xABCD0005, 100);

        // Act
        bool insertedAgain = map.Insert(0xABCD0005, 200);

        // Assert
        Assert.False(insertedAgain); // Should return false because key exists
        Assert.Equal(1, map.Count); // Count should remain unchanged
    }

    [Fact]
    public void Insert_Should_Handle_Collisions_Properly()
    {
        // Arrange
        var map = new BlitzMap<uint, uint>(16, 0.8);

        // Insert 5 values that hash to the same bucket (index = 5)
        map.Insert(0xABCD0005, 100);
        map.Insert(0xABCD0015, 200);
        map.Insert(0xABCD0025, 300);
        map.Insert(0xABCD0035, 400);
        map.Insert(0xABCD0045, 500);

        // Assert
        Assert.Equal(5, map.Count);
    }

    private static ulong g_lehmer64_state = 3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Lehmer64()
    {
        g_lehmer64_state *= 0xda942042e4dd58b5UL;
        return g_lehmer64_state >> 64;
    }

    [Theory]
    [InlineData(134217728)]

    public void Insert_Entries_And_Retrieve_Same_Buckets(uint length)
    {
        var rnd = new Random(3);

        var uni = new HashSet<uint>((int)length * 2);

        while (uni.Count < (uint)(length * 0.5))
        {
            uni.Add((uint)Lehmer64());
        }

       var keys = uni.ToArray();


        // Arrange
        var map = new BlitzMap<uint, uint>((int)BitOperations.RoundUpToPowerOf2(length), 0.5);
           
        // Act - Insert all keys
        for (int i = 0; i < keys.Length; i++)
        {
            bool inserted = map.Insert(keys[i], 1);
            Assert.True(inserted, $"Insert failed for key {keys[i]}");
        }

        // Assert - Ensure all keys are retrievable with correct values
        for (int i = 0; i < keys.Length; i++)
        {
            bool found = map.Get(keys[i], out var retrievedValue);
            if (found == false) 
            {
            
            }

            Assert.True(found, $"Key {keys[i]} was not found.");
            Assert.Equal(1u, retrievedValue);
        }

        // Verify total count
        Assert.Equal(keys.Length, map.Count);
    }

    [Fact]
    public void Insert_ShouldInsertNewKeyValuePair_WhenKeyDoesNotExist()
    {
        _map.Insert(1, "value1");
        Assert.True(_map.Contains(1));
        Assert.Equal("value1", _map[1]);
    }

    [Fact]
    public void Insert_ShouldResizeMap_WhenThresholdExceeded()
    {
        for (int i = 0; i < 20; i++)
        {
            _map.Insert(i, $"value{i}");
        }

        Assert.True(_map.Size > 16); // Assuming initial size was 16
    }

    [Fact]
    public void Insert_ShouldHandleHashCollisionsCorrectly()
    {
        int key1 = 17; // Assuming key1 and key2 will collide
        int key2 = 33; // Example collision-causing key in hash table

        _map.Insert(key1, "value17");
        _map.Insert(key2, "value33");

        Assert.Equal("value17", _map[key1]);
        Assert.Equal("value33", _map[key2]);
    }

    [Fact]
    public void Insert_ShouldReuseTombstonedEntries()
    {
        _map.Insert(1, "value1");
        _map.Remove(1);

        _map.Insert(1, "newValue");

        Assert.Equal("newValue", _map[1]);
        Assert.True(_map.Contains(1));
        Assert.Equal("newValue", _map[1]);
    }

    [Fact]
    public void Insert_ShouldInsertAtLastIndex_WhenWrapAroundOccurs()
    {
        int lastIndex = (int)_map.Size - 1;

        _map.Insert(lastIndex, "lastValue");

        Assert.Equal("lastValue", _map[lastIndex]);
    }

    [Fact]
    public void Insert_ShouldHandleNonLinearProbing()
    {
        for (int i = 0; i < 15; i++)
        {
            _map.Insert(i, $"value{i}");
        }

        _map.Insert(16, "probedValue");

        Assert.Equal("probedValue", _map[16]);
        Assert.True(_map.Contains(16));
        Assert.Equal("probedValue", _map[16]);
    }

    [Fact]
    public void Insert_ShouldReturnDefault_WhenInsertingNewKey()
    {
        _map.Insert(1, "newValue");


        Assert.True(_map.Contains(1));
        Assert.Equal("newValue", _map[1]);
    }

    [Fact]
    public void Insert_ShouldHandleMinIntAndMaxIntKeys()
    {
        _map.Insert(int.MinValue, "minValue");
        _map.Insert(int.MaxValue, "maxValue");

        Assert.Equal("minValue", _map[int.MinValue]);
        Assert.Equal("maxValue", _map[int.MaxValue]);
    }

    [Fact]
    public void Insert_ShouldAcceptNullValues()
    {
        _map.Insert(1, null);

        Assert.True(_map.Contains(1));
        Assert.Null(_map[1]);
    }

    [Fact]
    public void Insert_ShouldResize_WhenThresholdIsExceeded()
    {
        for (int i = 0; i < 20; i++)
        {
            _map.Insert(i, $"value{i}");
        }

        Assert.True(_map.Size > 16); // Assuming initial capacity is 16
    }

    [Fact]
    public void Insert_ShouldInsertAndRemoveSuccessfully()
    {
        _map.Insert(1, "value1");
        Assert.True(_map.Contains(1));

        _map.Remove(1);
        Assert.False(_map.Contains(1));
    }

    [Fact]
    public void Insert_ShouldReuseSlot_WhenKeyIsRemovedAndReinserted()
    {
        _map.Insert(1, "value1");
        _map.Remove(1);

        _map.Insert(1, "newValue");

        Assert.True(_map.Contains(1));
        Assert.Equal("newValue", _map[1]);
    }

    [Fact]
    public void Insert_ShouldHandleMultipleRemoveAndReinsertCycles()
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
    public void Insert_ShouldNotAffectOtherKeys_WhenKeyIsRemovedAndReinserted()
    {
        _map.Insert(1, "value1");
        _map.Insert(2, "value2");

        _map.Remove(1);
        _map.Insert(1, "newValue1");

        Assert.Equal("newValue1", _map[1]);
        Assert.Equal("value2", _map[2]);
    }

    [Fact]
    public void Insert_ShouldReuseTombstone_WhenInsertingAfterRemove()
    {
        _map.Insert(1, "value1");
        _map.Insert(2, "value2");
        _map.Remove(1);

        _map.Insert(1, "newValue");

        Assert.Equal("newValue", _map[1]);
        Assert.Equal("value2", _map[2]);
    }

    [Fact]
    public void Insert_ShouldInsertIntoNextAvailableSlot_WhenTombstonedSlotsExist()
    {
        _map.Insert(1, "value1");
        _map.Insert(2, "value2");

        _map.Remove(1);
        _map.Remove(2);

        _map.Insert(3, "newValue");

        Assert.True(_map.Contains(3));
        Assert.Equal("newValue", _map[3]);
    }

    [Fact]
    public void Insert_ShouldIncrementCount_WhenReinsertingRemovedKeys()
    {
        _map.Insert(1, "value1");
        _map.Insert(2, "value2");

        int initialCount = _map.Count;

        _map.Remove(1);
        _map.Insert(1, "newValue1");

        Assert.Equal(initialCount, _map.Count);
    }

    [Fact]
    public void Insert_ShouldResizeCorrectly_AfterMultipleInsertRemoveCycles()
    {
        for (int i = 0; i < 16; i++)
        {
            _map.Insert(i, $"value{i}");
        }

        for (int i = 0; i < 8; i++)
        {
            _map.Remove(i);
        }

        for (int i = 16; i < 24; i++)
        {
            _map.Insert(i, $"value{i}");
        }

        Assert.True(_map.Size > 16); // Ensures resizing occurred
    }

    [Fact]
    public void Insert_ShouldReturnPreviousValue_WhenInsertingAfterRemove()
    {
        _map.Insert(1, "value1");
        _map.Remove(1);

        _map.Insert(1, "newValue");

        Assert.Equal("newValue", _map[1]);
    }

    [Fact]
    public void Insert_ShouldHandleHighCollisionScenario()
    {
        // Intentionally create keys that hash to the same bucket
        int baseKey = 100;
        int[] collidingKeys = { baseKey, baseKey + 16, baseKey + 32 }; // Adjusted to likely collide in small table

        // Insert colliding keys
        foreach (var key in collidingKeys)
        {
            _map.Insert(key, $"value{key}");
        }

        // Check all entries are correctly inserted
        foreach (var key in collidingKeys)
        {
            Assert.True(_map.Contains(key));
            Assert.Equal($"value{key}", _map[key]);
        }
    }

    [Fact]
    public void Insert_ShouldHandleMinAndMaxIntKeys()
    {
        // Test inserting boundary integer keys
        _map.Insert(int.MinValue, "minValue");
        _map.Insert(int.MaxValue, "maxValue");

        // Validate both boundary keys are correctly stored and retrievable
        Assert.Equal("minValue", _map[int.MinValue]);
        Assert.Equal("maxValue", _map[int.MaxValue]);
    }

    [Fact]
    public void Insert_ShouldWrapAroundCorrectly_WhenArrayBoundExceeded()
    {
        // Set a smaller map to ensure we quickly wrap around the array bounds
        var smallMap = new BlitzMap<int, string>(8);

        for (int i = 0; i < 12; i++)
        {
            smallMap.Insert(i, $"value{i}");
        }

        // Ensure all inserted keys are retrievable and correctly handled at wrapped bounds
        for (int i = 0; i < 12; i++)
        {
            Assert.True(smallMap.Contains(i));
            Assert.Equal($"value{i}", smallMap[i]);
        }
    }

    [Fact]
    public void Insert_ShouldResizeCorrectlyAtHighLoadFactor()
    {
        // Insert entries to reach the resizing threshold
        for (int i = 0; i <= 28; i++) // Close to initial capacity of 32 with load factor 0.9
        {
            _map.Insert(i, $"value{i}");
        }

        // The map should have resized to accommodate the entries
        Assert.True(_map.Size > 32); // Initial capacity exceeded due to resizing

        // Ensure no data is lost after resizing
        for (int i = 0; i <= 28; i++)
        {
            Assert.Equal($"value{i}", _map[i]);
        }
    }

    [Fact]
    public void Insert_ShouldPreserveCorrectEntriesAfterResizeAndRemoveCycles()
    {
        // Insert keys up to resize limit
        for (int i = 0; i < 10; i++)
        {
            _map.Insert(i, $"value{i}");
        }

        // Remove half the entries and reinsert new entries
        for (int i = 0; i < 5; i++)
        {
            _map.Remove(i);
        }

        // Reinsert new entries after removal
        for (int i = 10; i < 15; i++)
        {
            _map.Insert(i, $"newValue{i}");
        }

        // Verify final state of map entries after resizing and removals
        for (int i = 5; i < 10; i++)
        {
            Assert.Equal($"value{i}", _map[i]);
        }
        for (int i = 10; i < 15; i++)
        {
            Assert.Equal($"newValue{i}", _map[i]);
        }
    }

    [Fact]
    public void Insert_ShouldHandleZeroAsAKey()
    {
        // Insert zero as a key
        _map.Insert(0, "zeroValue");

        // Verify zero key is correctly inserted and retrievable
        Assert.True(_map.Contains(0));
        Assert.Equal("zeroValue", _map[0]);
    }

    [Fact]
    public void Insert_ShouldHandleNegativeKeys()
    {
        // Insert a series of negative keys
        for (int i = -1; i > -5; i--)
        {
            _map.Insert(i, $"value{i}");
        }

        // Verify all negative keys are correctly inserted and retrievable
        for (int i = -1; i > -5; i--)
        {
            Assert.Equal($"value{i}", _map[i]);
        }
    }

    [Fact]
    public void Insert_ShouldHandleReinsertionIntoMultipleTombstonedSlots()
    {
        // Insert keys to create tombstones
        for (int i = 0; i < 10; i++)
        {
            _map.Insert(i, $"value{i}");
        }

        // Remove some entries to create tombstones
        for (int i = 0; i < 5; i++)
        {
            _map.Remove(i);
        }

        // Reinsert some keys to see if they are placed in tombstoned slots
        for (int i = 0; i < 5; i++)
        {
            _map.Insert(i, $"newValue{i}");
        }

        // Ensure all keys are correctly stored without duplication
        for (int i = 0; i < 10; i++)
        {
            string expectedValue = i < 5 ? $"newValue{i}" : $"value{i}";
            Assert.Equal(expectedValue, _map[i]);
        }
    }

    [Fact]
    public void Insert_ShouldEmlace_WhenReAddedAfterRemoval_WhileInDifferentGroups()
    {
        // Step 1: Add multiple entries
        List<int> coll = new List<int>();

        // Generate unique numbers that hash to 1
        for (uint i = 1; i < 1000000; i++)
        {
            var x = 0x9E3779B9 * i >> 27;
            if (x == 1)
            {
                coll.Add((int)i);
            }
        }

        for (int i = 0; i < 25; i++)
        {
            _map.Insert(coll.ElementAt(i), "test");
        }

        // Remove an item from the first group of 16
        _map.Remove(13);

        // Try to add _map duplicate which is in the second group of 16
        _map.Insert(coll.ElementAt(100), "newValue");

        Assert.True(_map[coll.ElementAt(100)] == "newValue");
    }

    [Fact]
    public void Emplaxce_ShouldTriggerResize()
    {
        var map = new BlitzMap<int, string>();
        var random = Random.Shared;
        for (int i = 0; i < 512; i++)
        {
            map.Insert(random.Next(), "test");
        }


    }


}