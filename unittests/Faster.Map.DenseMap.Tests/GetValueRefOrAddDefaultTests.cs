using Faster.Map.Core;
using System;
using Xunit;

namespace Faster.Map.DenseMap.Tests
{    public class GetValueRefOrAddDefaultTests
    {
        private DenseMap<int, int> _map;

        public GetValueRefOrAddDefaultTests()
        {
            _map = new DenseMap<int, int>();
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldAddNewEntry_WhenKeyDoesNotExist()
        {
            ref int value = ref _map.GetValueRefOrAddDefault(1);
            Assert.Equal(0, value); // Default value for int is 0
            value = 10;

            Assert.True(_map.Contains(1));
            Assert.Equal(10, _map[1]);
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldReturnExistingEntry_WhenKeyExists()
        {
            _map.InsertOrUpdate(2, 20);
            ref int value = ref _map.GetValueRefOrAddDefault(2);

            Assert.Equal(20, value);

            value = 30; // Update through ref
            Assert.Equal(30, _map[2]); // Verify updated value
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldHandleCollision_Correctly()
        {
            int key1 = 1;
            int key2 = 1 + 16; // Assuming 1 and 17 collide

            ref int value1 = ref _map.GetValueRefOrAddDefault(key1);
            value1 = 100;

            ref int value2 = ref _map.GetValueRefOrAddDefault(key2);
            value2 = 200;

            Assert.Equal(100, _map[key1]);
            Assert.Equal(200, _map[key2]);
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldReuseTombstonedSlot()
        {
            _map.InsertOrUpdate(3, 300);
            _map.Remove(3);

            ref int value = ref _map.GetValueRefOrAddDefault(3);
            Assert.Equal(0, value); // Default int value
            value = 400;

            Assert.True(_map.Contains(3));
            Assert.Equal(400, _map[3]);
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldHandleResize_Correctly()
        {
            for (int i = 0; i < 20; i++)
            {
                ref int value = ref _map.GetValueRefOrAddDefault(i);
                value = i * 10;
            }

            Assert.True(_map.Size > 16); // Check resize occurred
            for (int i = 0; i < 20; i++)
            {
                Assert.Equal(i * 10, _map[i]);
            }
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldHandleNegativeKeys()
        {
            ref int value = ref _map.GetValueRefOrAddDefault(-1);
            Assert.Equal(0, value);
            value = -100;

            Assert.True(_map.Contains(-1));
            Assert.Equal(-100, _map[-1]);
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldReturnSameReference_ForMultipleCallsWithSameKey()
        {
            ref int value1 = ref _map.GetValueRefOrAddDefault(5);
            value1 = 50;

            ref int value2 = ref _map.GetValueRefOrAddDefault(5);
            Assert.True(value1 == value2);

            value2 = 100;
            Assert.Equal(100, _map[5]);
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldReturnDefaultForNewKey_AfterResize()
        {
            for (int i = 0; i < 20; i++)
            {
                _map.GetValueRefOrAddDefault(i);
            }

            Assert.True(_map.Size > 16); // Confirm resize
            ref int value = ref _map.GetValueRefOrAddDefault(21);
            Assert.Equal(0, value); // Default value
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldNotAffectOtherKeys_WhenOneKeyIsModified()
        {
            ref int value1 = ref _map.GetValueRefOrAddDefault(10);
            ref int value2 = ref _map.GetValueRefOrAddDefault(20);

            value1 = 100;
            value2 = 200;

            Assert.Equal(100, _map[10]);
            Assert.Equal(200, _map[20]);
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldCorrectlyHandleMinAndMaxIntKeys()
        {
            ref int minValueRef = ref _map.GetValueRefOrAddDefault(int.MinValue);
            ref int maxValueRef = ref _map.GetValueRefOrAddDefault(int.MaxValue);

            minValueRef = -123;
            maxValueRef = 456;

            Assert.Equal(-123, _map[int.MinValue]);
            Assert.Equal(456, _map[int.MaxValue]);
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldReturnUpdatedValue_AfterPreviousRemoval()
        {
            _map.InsertOrUpdate(15, 150);
            _map.Remove(15);

            ref int value = ref _map.GetValueRefOrAddDefault(15);
            Assert.Equal(0, value); // After removal, default is returned
            value = 250;

            Assert.Equal(250, _map[15]);
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldAddEntry_WhenMapIsEmpty()
        {
            ref int value = ref _map.GetValueRefOrAddDefault(99);
            Assert.Equal(0, value); // Check initial value for empty map
            value = 199;

            Assert.True(_map.Contains(99));
            Assert.Equal(199, _map[99]);
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldReturnDefault_WhenAddingLargeNegativeKey()
        {
            ref int value = ref _map.GetValueRefOrAddDefault(int.MinValue + 1);
            Assert.Equal(0, value);

            value = -1000;
            Assert.Equal(-1000, _map[int.MinValue + 1]);
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldUseNewBucket_AfterRehash()
        {
            for (int i = 0; i < 100; i++)
            {
                ref int value = ref _map.GetValueRefOrAddDefault(i);
                value = i;
            }

            // Verify size increase and all entries present
            Assert.True(_map.Size > 16);
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(i, _map[i]);
            }
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldReturnUpdatedReference_ForExistingKey()
        {
            ref int value1 = ref _map.GetValueRefOrAddDefault(77);
            value1 = 700;

            ref int value2 = ref _map.GetValueRefOrAddDefault(77);
            Assert.Equal(700, value2);
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldHandleHighLoadFactor_WithoutLosingData()
        {
            for (int i = 0; i < 50; i++)
            {
                ref int value = ref _map.GetValueRefOrAddDefault(i);
                value = i * 5;
            }

            for (int i = 0; i < 50; i++)
            {
                Assert.Equal(i * 5, _map[i]);
            }
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldNotCreateDuplicates_AfterMultipleRemovalsAndAdditions()
        {
            ref int ref1 = ref _map.GetValueRefOrAddDefault(1);
            ref1 = 10;
            _map.Remove(1);

            ref int ref2 = ref _map.GetValueRefOrAddDefault(1);
            Assert.Equal(0, ref2);
            ref2 = 20;

            Assert.Equal(20, _map[1]);
            Assert.Equal(1, _map.Count);
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldCorrectlyInsertIntoTombstonedSlot()
        {
            for (int i = 0; i < 10; i++)
            {
                _map.GetValueRefOrAddDefault(i) = i * 10;
            }

            _map.Remove(5);

            ref int valueRef = ref _map.GetValueRefOrAddDefault(5);
            Assert.Equal(0, valueRef);
            valueRef = 50;

            Assert.Equal(50, _map[5]);
            Assert.Equal(10, _map.Count);
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldNotConflictAcrossLargeRangeOfKeys()
        {
            for (int i = -500; i < 500; i += 100)
            {
                ref int valueRef = ref _map.GetValueRefOrAddDefault(i);
                valueRef = i * 2;
            }

            for (int i = -500; i < 500; i += 100)
            {
                Assert.Equal(i * 2, _map[i]);
            }
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldHandleZeroAsKey()
        {
            ref int value = ref _map.GetValueRefOrAddDefault(0);
            Assert.Equal(0, value); // Default for int
            value = 50;

            Assert.Equal(50, _map[0]);
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldHandleMaxIntAndMinIntKeys_WithoutConflict()
        {
            ref int minValueRef = ref _map.GetValueRefOrAddDefault(int.MinValue);
            ref int maxValueRef = ref _map.GetValueRefOrAddDefault(int.MaxValue);

            minValueRef = -999;
            maxValueRef = 999;

            Assert.Equal(-999, _map[int.MinValue]);
            Assert.Equal(999, _map[int.MaxValue]);
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldReturnCorrectReference_AfterMapResize()
        {
            // Fill map to trigger resize
            for (int i = 0; i < 32; i++)
            {
                ref int value = ref _map.GetValueRefOrAddDefault(i);
                value = i * 10;
            }

            // Add a new entry after resize and verify it’s added correctly
            ref int newEntry = ref _map.GetValueRefOrAddDefault(33);
            newEntry = 330;

            Assert.Equal(330, _map[33]);
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldHandleMultipleInsertAndRemoveCycles()
        {
            for (int i = 0; i < 10; i++)
            {
                ref int value = ref _map.GetValueRefOrAddDefault(i);
                value = i * 100;
            }

            // Remove and re-add several keys to test tombstone handling
            for (int i = 5; i < 10; i++)
            {
                _map.Remove(i);
                ref int value = ref _map.GetValueRefOrAddDefault(i);
                value = i * 200;
            }

            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(i * 100, _map[i]);
            }

            for (int i = 5; i < 10; i++)
            {
                Assert.Equal(i * 200, _map[i]);
            }
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldInsertAndRetrieve_UsingLargePositiveAndNegativeKeys()
        {
            ref int posLarge = ref _map.GetValueRefOrAddDefault(int.MaxValue - 1);
            ref int negLarge = ref _map.GetValueRefOrAddDefault(int.MinValue + 1);

            posLarge = 1000;
            negLarge = -1000;

            Assert.Equal(1000, _map[int.MaxValue - 1]);
            Assert.Equal(-1000, _map[int.MinValue + 1]);
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldCorrectlyHandleMultipleEntriesWithCollisions()
        {
            int baseKey = 100;
            int[] collidingKeys = { baseKey, baseKey + 16, baseKey + 32 }; // Simulate collision scenario

            foreach (var key in collidingKeys)
            {
                ref int valueRef = ref _map.GetValueRefOrAddDefault(key);
                valueRef = key * 10;
            }

            foreach (var key in collidingKeys)
            {
                Assert.Equal(key * 10, _map[key]);
            }
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldUpdateExistingEntry_CorrectlyAfterResize()
        {
            for (int i = 0; i < 20; i++)
            {
                ref int value = ref _map.GetValueRefOrAddDefault(i);
                value = i;
            }

            ref int updatedValue = ref _map.GetValueRefOrAddDefault(10);
            updatedValue = 999;

            Assert.Equal(999, _map[10]);
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldInsertCorrectly_AfterMultipleRemoveOperations()
        {
            _map.GetValueRefOrAddDefault(1) = 10;
            _map.GetValueRefOrAddDefault(2) = 20;
            _map.GetValueRefOrAddDefault(3) = 30;

            _map.Remove(2);

            ref int newRef = ref _map.GetValueRefOrAddDefault(4);
            newRef = 40;

            Assert.True(_map.Contains(1));
            Assert.False(_map.Contains(2));
            Assert.True(_map.Contains(3));
            Assert.True(_map.Contains(4));
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldNotCreateDuplicateEntries_AfterMultipleAddAndRemoveCycles()
        {
            _map.GetValueRefOrAddDefault(1) = 100;
            _map.Remove(1);
            _map.GetValueRefOrAddDefault(2) = 200;
            _map.Remove(2);

            // Re-add both keys
            ref int ref1 = ref _map.GetValueRefOrAddDefault(1);
            ref1 = 150;

            ref int ref2 = ref _map.GetValueRefOrAddDefault(2);
            ref2 = 250;

            Assert.Equal(150, _map[1]);
            Assert.Equal(250, _map[2]);
            Assert.Equal(2, _map.Count); // Confirm no duplicates
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldCorrectlyHandleVeryLargeKeys()
        {
            // Use extreme large key values
            ref int valueLargePos = ref _map.GetValueRefOrAddDefault(int.MaxValue);
            ref int valueLargeNeg = ref _map.GetValueRefOrAddDefault(int.MinValue);

            valueLargePos = 12345;
            valueLargeNeg = -12345;

            Assert.Equal(12345, _map[int.MaxValue]);
            Assert.Equal(-12345, _map[int.MinValue]);
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldCorrectlyUpdateValue_WhenUsingExistingKey()
        {
            ref int value = ref _map.GetValueRefOrAddDefault(100);
            value = 300;

            ref int valueUpdate = ref _map.GetValueRefOrAddDefault(100);
            valueUpdate = 500;

            Assert.Equal(500, _map[100]);
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldReturnSameReference_ForRepeatedKeyAccess()
        {
            var denseMap = new DenseMap<int, string>();

            ref string value1 = ref denseMap.GetValueRefOrAddDefault(25);
            ref string value2 = ref denseMap.GetValueRefOrAddDefault(25);

            Assert.True(object.ReferenceEquals(value1, value2));          
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldReturnZero_ForNewKeysAfterMultipleAddRemoveCycles()
        {
            for (int i = 0; i < 10; i++)
            {
                _map.GetValueRefOrAddDefault(i) = i * 10;
            }

            for (int i = 5; i < 10; i++)
            {
                _map.Remove(i);
            }

            ref int newValueRef = ref _map.GetValueRefOrAddDefault(15);
            Assert.Equal(0, newValueRef); // New key after removals
        }

        [Fact]
        public void GetValueRefOrAddDefault_ShouldPreserveData_WhenAddingMoreKeysThanInitialCapacity()
        {
            int expectedCapacity = 16;
            for (int i = 0; i < expectedCapacity * 2; i++)
            {
                ref int value = ref _map.GetValueRefOrAddDefault(i);
                value = i * 2;
            }

            for (int i = 0; i < expectedCapacity * 2; i++)
            {
                Assert.Equal(i * 2, _map[i]);
            }
        }
    }
}
