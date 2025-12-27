using Faster.Map.Core;
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
            _map.InsertOrUpdate(1, "value1");
            Assert.True(_map.Contains(1));
            Assert.Equal("value1", _map[1]);
        }

        [Fact]
        public void Emplace_ShouldUpdateExistingKeyValuePair_WhenKeyAlreadyExists()
        {
            _map.InsertOrUpdate(1, "initialValue");
            _map.InsertOrUpdate(1, "updatedValue");

            Assert.True(_map.Contains(1));
            Assert.Equal("updatedValue", _map[1]);
        }

        [Fact]
        public void Emplace_ShouldResizeMap_WhenThresholdExceeded()
        {
            for (int i = 0; i < 20; i++)
            {
                _map.InsertOrUpdate(i, $"value{i}");
            }

            Assert.True(_map.Size > 16); // Assuming initial size was 16
        }

        [Fact]
        public void Emplace_ShouldHandleHashCollisionsCorrectly()
        {
            int key1 = 17; // Assuming key1 and key2 will collide
            int key2 = 33; // Example collision-causing key in hash table

            _map.InsertOrUpdate(key1, "value17");
            _map.InsertOrUpdate(key2, "value33");

            Assert.Equal("value17", _map[key1]);
            Assert.Equal("value33", _map[key2]);
        }

        [Fact]
        public void Emplace_ShouldReuseTombstonedEntries()
        {
            _map.InsertOrUpdate(1, "value1");
            _map.Remove(1);

            _map.InsertOrUpdate(1, "newValue");

            Assert.Equal("newValue", _map[1]);
            Assert.True(_map.Contains(1));
            Assert.Equal("newValue", _map[1]);
        }

        [Fact]
        public void Emplace_ShouldInsertAtLastIndex_WhenWrapAroundOccurs()
        {
            int lastIndex = (int)_map.Size - 1;

            _map.InsertOrUpdate(lastIndex, "lastValue");

            Assert.Equal("lastValue", _map[lastIndex]);
        }

        [Fact]
        public void Emplace_ShouldHandleNonLinearProbing()
        {
            for (int i = 0; i < 15; i++)
            {
                _map.InsertOrUpdate(i, $"value{i}");
            }

            _map.InsertOrUpdate(16, "probedValue");

            Assert.Equal("probedValue", _map[16]);
            Assert.True(_map.Contains(16));
            Assert.Equal("probedValue", _map[16]);
        }

        [Fact]
        public void Emplace_ShouldReturnDefault_WhenInsertingNewKey()
        {
            _map.InsertOrUpdate(1, "newValue");


            Assert.True(_map.Contains(1));
            Assert.Equal("newValue", _map[1]);
        }

        [Fact]
        public void Emplace_ShouldReturnOldValue_WhenUpdatingExistingKey()
        {
            _map.InsertOrUpdate(1, "initialValue");
            _map.InsertOrUpdate(1, "updatedValue");

            Assert.Equal("updatedValue", _map[1]);
        }

        [Fact]
        public void Emplace_ShouldUpdateValue_WhenDuplicateKeyIsInsertedConsecutively()
        {
            _map.InsertOrUpdate(1, "initialValue");
            _map.InsertOrUpdate(1, "duplicateValue");

            Assert.True(_map.Contains(1));
            Assert.Equal("duplicateValue", _map[1]);
        }

        [Fact]
        public void Emplace_ShouldUpdateValueCorrectly_WhenDuplicateKeyInsertedMultipleTimes()
        {
            _map.InsertOrUpdate(1, "value1");
            _map.InsertOrUpdate(1, "value2");
            _map.InsertOrUpdate(1, "finalValue");

            Assert.True(_map.Contains(1));
            Assert.Equal("finalValue", _map[1]);
        }

        [Fact]
        public void Emplace_ShouldReturnLastInsertedValue_WhenDuplicateKeysAreInsertedInSequence()
        {
            _map.InsertOrUpdate(1, "value1");
            _map.InsertOrUpdate(1, "value2");
            _map.InsertOrUpdate(1, "value3");

            Assert.Equal("value3", _map[1]);
        }

        [Fact]
        public void Emplace_ShouldHandleMinIntAndMaxIntKeys()
        {
            _map.InsertOrUpdate(int.MinValue, "minValue");
            _map.InsertOrUpdate(int.MaxValue, "maxValue");

            Assert.Equal("minValue", _map[int.MinValue]);
            Assert.Equal("maxValue", _map[int.MaxValue]);
        }

        [Fact]
        public void Emplace_ShouldAcceptNullValues()
        {
            _map.InsertOrUpdate(1, null);

            Assert.True(_map.Contains(1));
            Assert.Null(_map[1]);
        }

        [Fact]
        public void Emplace_ShouldResize_WhenThresholdIsExceeded()
        {
            for (int i = 0; i < 20; i++)
            {
                _map.InsertOrUpdate(i, $"value{i}");
            }

            Assert.True(_map.Size > 16); // Assuming initial capacity is 16
        }

        [Fact]
        public void Emplace_ShouldInsertAndRemoveSuccessfully()
        {
            _map.InsertOrUpdate(1, "value1");
            Assert.True(_map.Contains(1));

            _map.Remove(1);
            Assert.False(_map.Contains(1));
        }

        [Fact]
        public void Emplace_ShouldReuseSlot_WhenKeyIsRemovedAndReinserted()
        {
            _map.InsertOrUpdate(1, "value1");
            _map.Remove(1);

            _map.InsertOrUpdate(1, "newValue");

            Assert.True(_map.Contains(1));
            Assert.Equal("newValue", _map[1]);
        }

        [Fact]
        public void Emplace_ShouldHandleMultipleRemoveAndReinsertCycles()
        {
            _map.InsertOrUpdate(1, "value1");
            _map.Remove(1);

            _map.InsertOrUpdate(1, "value2");
            _map.Remove(1);

            _map.InsertOrUpdate(1, "finalValue");

            Assert.True(_map.Contains(1));
            Assert.Equal("finalValue", _map[1]);
        }

        [Fact]
        public void Emplace_ShouldNotAffectOtherKeys_WhenKeyIsRemovedAndReinserted()
        {
            _map.InsertOrUpdate(1, "value1");
            _map.InsertOrUpdate(2, "value2");

            _map.Remove(1);
            _map.InsertOrUpdate(1, "newValue1");

            Assert.Equal("newValue1", _map[1]);
            Assert.Equal("value2", _map[2]);
        }

        [Fact]
        public void Emplace_ShouldReuseTombstone_WhenInsertingAfterRemove()
        {
            _map.InsertOrUpdate(1, "value1");
            _map.InsertOrUpdate(2, "value2");
            _map.Remove(1);

            _map.InsertOrUpdate(1, "newValue");

            Assert.Equal("newValue", _map[1]);
            Assert.Equal("value2", _map[2]);
        }

        [Fact]
        public void Emplace_ShouldInsertIntoNextAvailableSlot_WhenTombstonedSlotsExist()
        {
            _map.InsertOrUpdate(1, "value1");
            _map.InsertOrUpdate(2, "value2");

            _map.Remove(1);
            _map.Remove(2);

            _map.InsertOrUpdate(3, "newValue");

            Assert.True(_map.Contains(3));
            Assert.Equal("newValue", _map[3]);
        }

        [Fact]
        public void Emplace_ShouldIncrementCount_WhenReinsertingRemovedKeys()
        {
            _map.InsertOrUpdate(1, "value1");
            _map.InsertOrUpdate(2, "value2");

            int initialCount = _map.Count;

            _map.Remove(1);
            _map.InsertOrUpdate(1, "newValue1");

            Assert.Equal(initialCount, _map.Count);
        }

        [Fact]
        public void Emplace_ShouldResizeCorrectly_AfterMultipleInsertRemoveCycles()
        {
            for (int i = 0; i < 16; i++)
            {
                _map.InsertOrUpdate(i, $"value{i}");
            }

            for (int i = 0; i < 8; i++)
            {
                _map.Remove(i);
            }

            for (int i = 16; i < 24; i++)
            {
                _map.InsertOrUpdate(i, $"value{i}");
            }

            Assert.True(_map.Size > 16); // Ensures resizing occurred
        }

        [Fact]
        public void Emplace_ShouldReturnPreviousValue_WhenInsertingAfterRemove()
        {
            _map.InsertOrUpdate(1, "value1");
            _map.Remove(1);

            _map.InsertOrUpdate(1, "newValue");

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
                _map.InsertOrUpdate(key, $"value{key}");
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
                _map.InsertOrUpdate(key, $"value{key}");
            }

            // Remove the last entry in the collision chain
            _map.Remove(collidingKeys[2]);

            // Reinsert the first key and ensure no duplicates and correct updates
            _map.InsertOrUpdate(collidingKeys[0], "newValue0");

            Assert.Equal("newValue0", _map[collidingKeys[0]]);
            Assert.Equal($"value{collidingKeys[1]}", _map[collidingKeys[1]]);
            Assert.False(_map.Contains(collidingKeys[2])); // Verify the removed key is absent
        }

        [Fact]
        public void Emplace_ShouldHandleMinAndMaxIntKeys()
        {
            // Test inserting boundary integer keys
            _map.InsertOrUpdate(int.MinValue, "minValue");
            _map.InsertOrUpdate(int.MaxValue, "maxValue");

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
                smallMap.InsertOrUpdate(i, $"value{i}");
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
            for (int i = 0; i <= 28; i++) // Close to initial capacity of 32 with load factor 0.9
            {
                _map.InsertOrUpdate(i, $"value{i}");
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
        public void Emplace_ShouldPreserveCorrectEntriesAfterResizeAndRemoveCycles()
        {
            // Insert keys up to resize limit
            for (int i = 0; i < 10; i++)
            {
                _map.InsertOrUpdate(i, $"value{i}");
            }

            // Remove half the entries and reinsert new entries
            for (int i = 0; i < 5; i++)
            {
                _map.Remove(i);
            }

            // Reinsert new entries after removal
            for (int i = 10; i < 15; i++)
            {
                _map.InsertOrUpdate(i, $"newValue{i}");
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
            _map.InsertOrUpdate(0, "zeroValue");

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
                _map.InsertOrUpdate(i, $"value{i}");
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
                _map.InsertOrUpdate(i, $"value{i}");
            }

            // Remove some entries to create tombstones
            for (int i = 0; i < 5; i++)
            {
                _map.Remove(i);
            }

            // Reinsert some keys to see if they are placed in tombstoned slots
            for (int i = 0; i < 5; i++)
            {
                _map.InsertOrUpdate(i, $"newValue{i}");
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
            _map.InsertOrUpdate(1, "value1");
            _map.InsertOrUpdate(2, "value2");
            _map.InsertOrUpdate(3, "value3");

            // Step 2: Verify all entries are correctly added
            Assert.Equal("value1", _map[1]);
            Assert.Equal("value2", _map[2]);
            Assert.Equal("value3", _map[3]);

            // Step 3: Remove one entry
            _map.Remove(2);

            // Step 4: Re-add an existing entry (should update, not duplicate)
            _map.InsertOrUpdate(1, "newValue1");

            // Step 5: Assertions   
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
                _map.InsertOrUpdate(coll.ElementAt(i), "test");
            }

            // Remove an item from the first group of 16
            _map.Remove(13);

            // Try to add a duplicate which is in the second group of 16
            _map.InsertOrUpdate(767, "UpdatedValue");

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
                _map.InsertOrUpdate(coll.ElementAt(i), "test");
            }

            // Remove an item from the first group of 16
            _map.Remove(13);

            // Try to add _map duplicate which is in the second group of 16
            _map.InsertOrUpdate(coll.ElementAt(100), "newValue");

            Assert.True(_map[coll.ElementAt(100)] == "newValue");
        }

        [Fact]
        public void Emplaxce_ShouldTriggerResize()
        {
            var map = new DenseMap<int, string>();
            var random = Random.Shared;
            for (int i = 0; i < 512; i++)
            {
               map.InsertOrUpdate(random.Next(), "test");
            }


        }

    }
}