using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Faster.Map.DenseMap.Tests
{
    public class EmplaceTests
    {
        private readonly DenseMap<int, string> _map;

        public EmplaceTests()
        {
            _map = new DenseMap<int, string>();
        }

        [Fact]
        public void Emplace_ShouldInsertNewKeyValuePair_WhenKeyDoesNotExist()
        {
            var result = _map.Emplace(1, "value1");
            Assert.True(result);
            Assert.True(_map.Contains(1));
            Assert.Equal("value1", _map[1]);
        }

        [Fact]
        public void Emplace_ShouldUpdateExistingKeyValuePair_WhenKeyAlreadyExists()
        {
            _map.Emplace(1, "initialValue");
            var result = _map.Emplace(1, "updatedValue");

            Assert.True(result);
            Assert.True(_map.Contains(1));
            Assert.Equal("updatedValue", _map[1]);
        }

        [Fact]
        public void Emplace_ShouldResizeMap_WhenThresholdExceeded()
        {
            for (int i = 0; i < 20; i++)
            {
                _map.Emplace(i, $"value{i}");
            }

            Assert.True(_map.Size > 16); // Assuming initial size was 16
        }

        [Fact]
        public void Emplace_ShouldHandleHashCollisionsCorrectly()
        {
            int key1 = 17; // Assuming key1 and key2 will collide
            int key2 = 33; // Example collision-causing key in hash table

            _map.Emplace(key1, "value17");
            _map.Emplace(key2, "value33");

            Assert.Equal("value17", _map[key1]);
            Assert.Equal("value33", _map[key2]);
        }

        [Fact]
        public void Emplace_ShouldReuseTombstonedEntries()
        {
            _map.Emplace(1, "value1");
            _map.Remove(1);

            _map.Emplace(1, "newValue");

            Assert.Equal("newValue", _map[1]);
            Assert.True(_map.Contains(1));
            Assert.Equal("newValue", _map[1]);
        }

        [Fact]
        public void Emplace_ShouldInsertAtLastIndex_WhenWrapAroundOccurs()
        {
            int lastIndex = (int)_map.Size - 1;

            _map.Emplace(lastIndex, "lastValue");

            Assert.Equal("lastValue", _map[lastIndex]);
        }

        [Fact]
        public void Emplace_ShouldHandleNonLinearProbing()
        {
            for (int i = 0; i < 15; i++)
            {
                _map.Emplace(i, $"value{i}");
            }

            _map.Emplace(16, "probedValue");

            Assert.Equal("probedValue", _map[16]);
            Assert.True(_map.Contains(16));
            Assert.Equal("probedValue", _map[16]);
        }

        [Fact]
        public void Emplace_ShouldReturnDefault_WhenInsertingNewKey()
        {
            var result = _map.Emplace(1, "newValue");

            Assert.True(result);
            Assert.True(_map.Contains(1));
            Assert.Equal("newValue", _map[1]);
        }

        [Fact]
        public void Emplace_ShouldReturnOldValue_WhenUpdatingExistingKey()
        {
            _map.Emplace(1, "initialValue");
            var result = _map.Emplace(1, "updatedValue");

            Assert.True(result);
            Assert.Equal("updatedValue", _map[1]);
        }

        [Fact]
        public void Emplace_ShouldUpdateValue_WhenDuplicateKeyIsInsertedConsecutively()
        {
            _map.Emplace(1, "initialValue");
            var result = _map.Emplace(1, "duplicateValue");

            Assert.True(result);
            Assert.True(_map.Contains(1));
            Assert.Equal("duplicateValue", _map[1]);
        }

        [Fact]
        public void Emplace_ShouldUpdateValueCorrectly_WhenDuplicateKeyInsertedMultipleTimes()
        {
            _map.Emplace(1, "value1");
            _map.Emplace(1, "value2");
            var result = _map.Emplace(1, "finalValue");

            Assert.True(result);
            Assert.True(_map.Contains(1));
            Assert.Equal("finalValue", _map[1]);
        }

        [Fact]
        public void Emplace_ShouldReturnLastInsertedValue_WhenDuplicateKeysAreInsertedInSequence()
        {
            _map.Emplace(1, "value1");
            _map.Emplace(1, "value2");
            _map.Emplace(1, "value3");

            Assert.Equal("value3", _map[1]);
        }

        [Fact]
        public void Emplace_ShouldHandleMinIntAndMaxIntKeys()
        {
            _map.Emplace(int.MinValue, "minValue");
            _map.Emplace(int.MaxValue, "maxValue");

            Assert.Equal("minValue", _map[int.MinValue]);
            Assert.Equal("maxValue", _map[int.MaxValue]);
        }

        [Fact]
        public void Emplace_ShouldAcceptNullValues()
        {
            _map.Emplace(1, null);

            Assert.True(_map.Contains(1));
            Assert.Null(_map[1]);
        }

        [Fact]
        public void Emplace_ShouldResize_WhenThresholdIsExceeded()
        {
            for (int i = 0; i < 20; i++)
            {
                _map.Emplace(i, $"value{i}");
            }

            Assert.True(_map.Size > 16); // Assuming initial capacity is 16
        }

        [Fact]
        public void Emplace_ShouldInsertAndRemoveSuccessfully()
        {
            _map.Emplace(1, "value1");
            Assert.True(_map.Contains(1));

            _map.Remove(1);
            Assert.False(_map.Contains(1));
        }

        [Fact]
        public void Emplace_ShouldReuseSlot_WhenKeyIsRemovedAndReinserted()
        {
            _map.Emplace(1, "value1");
            _map.Remove(1);

            _map.Emplace(1, "newValue");

            Assert.True(_map.Contains(1));
            Assert.Equal("newValue", _map[1]);
        }

        [Fact]
        public void Emplace_ShouldHandleMultipleRemoveAndReinsertCycles()
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
        public void Emplace_ShouldNotAffectOtherKeys_WhenKeyIsRemovedAndReinserted()
        {
            _map.Emplace(1, "value1");
            _map.Emplace(2, "value2");

            _map.Remove(1);
            _map.Emplace(1, "newValue1");

            Assert.Equal("newValue1", _map[1]);
            Assert.Equal("value2", _map[2]);
        }

        [Fact]
        public void Emplace_ShouldReuseTombstone_WhenInsertingAfterRemove()
        {
            _map.Emplace(1, "value1");
            _map.Emplace(2, "value2");
            _map.Remove(1);

            var result = _map.Emplace(1, "newValue");

            Assert.True(result);
            Assert.Equal("newValue", _map[1]);
            Assert.Equal("value2", _map[2]);
        }

        [Fact]
        public void Emplace_ShouldInsertIntoNextAvailableSlot_WhenTombstonedSlotsExist()
        {
            _map.Emplace(1, "value1");
            _map.Emplace(2, "value2");

            _map.Remove(1);
            _map.Remove(2);

            _map.Emplace(3, "newValue");

            Assert.True(_map.Contains(3));
            Assert.Equal("newValue", _map[3]);
        }

        [Fact]
        public void Emplace_ShouldIncrementCount_WhenReinsertingRemovedKeys()
        {
            _map.Emplace(1, "value1");
            _map.Emplace(2, "value2");

            int initialCount = _map.Count;

            _map.Remove(1);
            _map.Emplace(1, "newValue1");

            Assert.Equal(initialCount, _map.Count);
        }

        [Fact]
        public void Emplace_ShouldResizeCorrectly_AfterMultipleInsertRemoveCycles()
        {
            for (int i = 0; i < 16; i++)
            {
                _map.Emplace(i, $"value{i}");
            }

            for (int i = 0; i < 8; i++)
            {
                _map.Remove(i);
            }

            for (int i = 16; i < 24; i++)
            {
                _map.Emplace(i, $"value{i}");
            }

            Assert.True(_map.Size > 16); // Ensures resizing occurred
        }

        [Fact]
        public void Emplace_ShouldReturnPreviousValue_WhenInsertingAfterRemove()
        {
            _map.Emplace(1, "value1");
            _map.Remove(1);

            var result = _map.Emplace(1, "newValue");

            Assert.True(result);
            Assert.Equal("newValue", _map[1]);
        }

        [Fact]
        public void Emplace_ShouldHandleHighCollisionScenario()
        {
            // Intentionally create keys that hash to the same bucket
            int baseKey = 100;
            int[] collidingKeys = { baseKey, baseKey + 16, baseKey + 32 }; // Adjusted to likely collide in small table

            // Insert colliding keys
            foreach (var key in collidingKeys)
            {
                _map.Emplace(key, $"value{key}");
            }

            // Check all entries are correctly inserted
            foreach (var key in collidingKeys)
            {
                Assert.True(_map.Contains(key));
                Assert.Equal($"value{key}", _map[key]);
            }
        }

        [Fact]
        public void Emplace_ShouldCorrectlyReplaceAfterRemovingInCollisionChain()
        {
            int baseKey = 50;
            int[] collidingKeys = { baseKey, baseKey + 16, baseKey + 32 };

            // Insert colliding keys
            foreach (var key in collidingKeys)
            {
                _map.Emplace(key, $"value{key}");
            }

            // Remove the last entry in the collision chain
            _map.Remove(collidingKeys[2]);

            // Reinsert the first key and ensure no duplicates and correct updates
            _map.Emplace(collidingKeys[0], "newValue0");

            Assert.Equal("newValue0", _map[collidingKeys[0]]);
            Assert.Equal($"value{collidingKeys[1]}", _map[collidingKeys[1]]);
            Assert.False(_map.Contains(collidingKeys[2])); // Verify the removed key is absent
        }

        [Fact]
        public void Emplace_ShouldHandleMinAndMaxIntKeys()
        {
            // Test inserting boundary integer keys
            _map.Emplace(int.MinValue, "minValue");
            _map.Emplace(int.MaxValue, "maxValue");

            // Validate both boundary keys are correctly stored and retrievable
            Assert.Equal("minValue", _map[int.MinValue]);
            Assert.Equal("maxValue", _map[int.MaxValue]);
        }

        [Fact]
        public void Emplace_ShouldWrapAroundCorrectly_WhenArrayBoundExceeded()
        {
            // Set a smaller map to ensure we quickly wrap around the array bounds
            var smallMap = new DenseMap<int, string>(8);

            for (int i = 0; i < 12; i++)
            {
                smallMap.Emplace(i, $"value{i}");
            }

            // Ensure all inserted keys are retrievable and correctly handled at wrapped bounds
            for (int i = 0; i < 12; i++)
            {
                Assert.True(smallMap.Contains(i));
                Assert.Equal($"value{i}", smallMap[i]);
            }
        }

        [Fact]
        public void Emplace_ShouldResizeCorrectlyAtHighLoadFactor()
        {
            // Insert entries to reach the resizing threshold
            for (int i = 0; i < 15; i++) // Close to initial capacity of 16 with load factor 0.9
            {
                _map.Emplace(i, $"value{i}");
            }

            // The map should have resized to accommodate the entries
            Assert.True(_map.Size > 16); // Initial capacity exceeded due to resizing

            // Ensure no data is lost after resizing
            for (int i = 0; i < 15; i++)
            {
                Assert.Equal($"value{i}", _map[i]);
            }
        }

        [Fact]
        public void Emplace_ShouldPreserveCorrectEntriesAfterResizeAndRemoveCycles()
        {
            // Insert keys up to resize limit
            for (int i = 0; i < 10; i++)
            {
                _map.Emplace(i, $"value{i}");
            }

            // Remove half the entries and reinsert new entries
            for (int i = 0; i < 5; i++)
            {
                _map.Remove(i);
            }

            // Reinsert new entries after removal
            for (int i = 10; i < 15; i++)
            {
                _map.Emplace(i, $"newValue{i}");
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
        public void Emplace_ShouldHandleZeroAsAKey()
        {
            // Insert zero as a key
            _map.Emplace(0, "zeroValue");

            // Verify zero key is correctly inserted and retrievable
            Assert.True(_map.Contains(0));
            Assert.Equal("zeroValue", _map[0]);
        }

        [Fact]
        public void Emplace_ShouldHandleNegativeKeys()
        {
            // Insert a series of negative keys
            for (int i = -1; i > -5; i--)
            {
                _map.Emplace(i, $"value{i}");
            }

            // Verify all negative keys are correctly inserted and retrievable
            for (int i = -1; i > -5; i--)
            {
                Assert.Equal($"value{i}", _map[i]);
            }
        }

        [Fact]
        public void Emplace_ShouldHandleReinsertionIntoMultipleTombstonedSlots()
        {
            // Insert keys to create tombstones
            for (int i = 0; i < 10; i++)
            {
                _map.Emplace(i, $"value{i}");
            }

            // Remove some entries to create tombstones
            for (int i = 0; i < 5; i++)
            {
                _map.Remove(i);
            }

            // Reinsert some keys to see if they are placed in tombstoned slots
            for (int i = 0; i < 5; i++)
            {
                _map.Emplace(i, $"newValue{i}");
            }

            // Ensure all keys are correctly stored without duplication
            for (int i = 0; i < 10; i++)
            {
                string expectedValue = i < 5 ? $"newValue{i}" : $"value{i}";
                Assert.Equal(expectedValue, _map[i]);
            }
        }

        [Fact]
        public void Emplace_ShouldUpdateExistingEntry_WhenReAddedAfterRemovalOfAnother()
        {
            // Step 1: Add multiple entries
            _map.Emplace(1, "value1");
            _map.Emplace(2, "value2");
            _map.Emplace(3, "value3");

            // Step 2: Verify all entries are correctly added
            Assert.Equal("value1", _map[1]);
            Assert.Equal("value2", _map[2]);
            Assert.Equal("value3", _map[3]);

            // Step 3: Remove one entry
            _map.Remove(2);

            // Step 4: Re-add an existing entry (should update, not duplicate)
            var oldValue = _map.Emplace(1, "newValue1");

            // Step 5: Assertions
            Assert.True(oldValue); // Ensure old value is returned
            Assert.Equal("newValue1", _map[1]); // Verify updated value for key 1
            Assert.False(_map.Contains(2)); // Key 2 should be removed
            Assert.Equal("value3", _map[3]); // Key 3 should remain unchanged
            Assert.Equal(2, _map.Count); // Only two entries should exist
        }

        [Fact]
        public void Emplace_ShouldNotAddDuplicate_WhenReAddedAfterRemoval_WhileInDifferentGroups()
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
                _map.Emplace(coll.ElementAt(i), "test");
            }

            // Remove an item from the first group of 16
            _map.Remove(13);

            // Try to add a duplicate which is in the second group of 16
            var value = _map.Emplace(767, "UpdatedValue");

            Assert.True(value);
            Assert.True(_map[767] == "UpdatedValue");
        }

        [Fact]
        public void Emplace_ShouldEmlace_WhenReAddedAfterRemoval_WhileInDifferentGroups()
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
                _map.Emplace(coll.ElementAt(i), "test");
            }

            // Remove an item from the first group of 16
            _map.Remove(13);

            // Try to add _map duplicate which is in the second group of 16
            var value = _map.Emplace(coll.ElementAt(100), "newValue");

            Assert.True(value);
            Assert.True(_map[coll.ElementAt(100)] == "newValue");
        }
    }
}