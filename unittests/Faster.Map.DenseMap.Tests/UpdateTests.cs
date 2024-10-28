using System;
using Xunit;

namespace Faster.Map.DenseMap.Tests
{
    public class UpdateTests
    {
        [Fact]
        public void Update_ShouldUpdateValue_WhenKeyExists()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(1, "initialValue");
            bool updated = map.Update(1, "newValue");

            Assert.True(updated);
            Assert.Equal("newValue", map[1]);
        }

        [Fact]
        public void Update_ShouldReturnFalse_WhenKeyDoesNotExist()
        {
            var map = new DenseMap<int, string>();
            bool updated = map.Update(99, "newValue");

            Assert.False(updated);
        }

        [Fact]
        public void Update_ShouldHandleMinAndMaxIntKeys()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(int.MinValue, "minValue");
            map.Emplace(int.MaxValue, "maxValue");

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
            var map = new DenseMap<int, string>();
            bool updated = map.Update(1, "value");

            Assert.False(updated);
        }

        [Fact]
        public void Update_ShouldHandleNullValues()
        {
            var map = new DenseMap<int, string?>();
            map.Emplace(1, "initialValue");

            bool updated = map.Update(1, null);

            Assert.True(updated);
            Assert.Null(map[1]);
        }

        [Fact]
        public void Update_ShouldHandleZeroKey()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(0, "initialValue");

            bool updated = map.Update(0, "updatedValue");

            Assert.True(updated);
            Assert.Equal("updatedValue", map[0]);
        }

        [Fact]
        public void Update_ShouldWorkNearLoadFactorLimit()
        {
            var map = new DenseMap<int, string>(16, 0.9);

            for (int i = 0; i < 14; i++)
            {
                map.Emplace(i, $"value{i}");
            }

            bool updated = map.Update(5, "updatedValue");

            Assert.True(updated);
            Assert.Equal("updatedValue", map[5]);
        }

        [Fact]
        public void Update_ShouldReuseTombstonedSlot_WhenKeyReinserted()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(1, "initialValue");
            map.Remove(1);
            map.Emplace(1, "newValue");

            bool updated = map.Update(1, "updatedAgain");

            Assert.True(updated);
            Assert.Equal("updatedAgain", map[1]);
        }

        [Fact]
        public void Update_ShouldHandleHighCollisionScenario()
        {
            var map = new DenseMap<int, string>();
            int baseKey = 100;
            int[] collidingKeys = { baseKey, baseKey + 16, baseKey + 32 };

            foreach (var key in collidingKeys)
            {
                map.Emplace(key, $"value{key}");
            }

            bool updated = map.Update(collidingKeys[1], "updatedValue");

            Assert.True(updated);
            Assert.Equal("updatedValue", map[collidingKeys[1]]);
        }

        [Fact]
        public void Update_ShouldNotAffectOtherEntries_InCollisionChain()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(1, "value1");
            map.Emplace(2, "value2");
            map.Emplace(17, "value17"); // Assuming it causes a collision with key 1

            bool updated = map.Update(1, "newValue1");

            Assert.True(updated);
            Assert.Equal("newValue1", map[1]);
            Assert.Equal("value2", map[2]);
            Assert.Equal("value17", map[17]);
        }

        [Fact]
        public void Update_ShouldReturnFalse_WhenTryingToUpdateTombstonedEntry()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(1, "value1");
            map.Remove(1);

            bool updated = map.Update(1, "newValue");

            Assert.False(updated);
        }

        [Fact]
        public void Update_ShouldHandleNegativeKeys()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(-1, "negativeValue");

            bool updated = map.Update(-1, "updatedNegativeValue");

            Assert.True(updated);
            Assert.Equal("updatedNegativeValue", map[-1]);
        }

        [Fact]
        public void Update_ShouldReturnTrue_WhenUpdatingKeyInCollisionGroup()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(1, "value1");
            map.Emplace(17, "value17"); // Assuming collision with key 1

            bool updated = map.Update(17, "updatedValue17");

            Assert.True(updated);
            Assert.Equal("updatedValue17", map[17]);
        }

        [Fact]
        public void Update_ShouldHandleMultipleUpdateOperations_OnSameKey()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(1, "initialValue");

            map.Update(1, "updatedValue1");
            map.Update(1, "updatedValue2");

            Assert.Equal("updatedValue2", map[1]);
        }

        [Fact]
        public void Update_ShouldHandleResizeCorrectly()
        {
            var map = new DenseMap<int, string>(4, 0.9);

            for (int i = 0; i < 5; i++)
            {
                map.Emplace(i, $"value{i}");
            }

            bool updated = map.Update(3, "updatedValue");

            Assert.True(updated);
            Assert.Equal("updatedValue", map[3]);
            Assert.True(map.Size > 4);
        }

        [Fact]
        public void Update_ShouldReturnFalse_WhenCalledAfterClear()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(1, "value1");

            map.Clear();

            bool updated = map.Update(1, "newValue");

            Assert.False(updated);
        }

        [Fact]
        public void Update_ShouldNotReinsertRemovedKey()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(1, "value1");
            map.Remove(1);

            bool updated = map.Update(1, "newValue");

            Assert.False(updated);
            Assert.False(map.Contains(1));
        }

        [Fact]
        public void Update_ShouldHandleReinsertedKey_AfterBeingRemovedAndReinserted()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(1, "initialValue");
            map.Remove(1);
            map.Emplace(1, "newValue");

            bool updated = map.Update(1, "finalValue");

            Assert.True(updated);
            Assert.Equal("finalValue", map[1]);
        }

        [Fact]
        public void Update_ShouldNotAffectAdjacentKeys_WhenUpdatingKeyInHighLoadMap()
        {
            var map = new DenseMap<int, string>(32);

            for (int i = 0; i < 28; i++)
            {
                map.Emplace(i, $"value{i}");
            }

            map.Update(4, "updatedValue4");

            Assert.Equal("updatedValue4", map[4]);
            Assert.Equal("value5", map[5]);
        }

        [Fact]
        public void Update_ShouldReturnFalse_WhenKeyIsNotPresentInHighLoadMap()
        {
            var map = new DenseMap<int, string>(32);

            for (int i = 0; i < 28; i++)
            {
                map.Emplace(i, $"value{i}");
            }

            bool updated = map.Update(29, "newValue");

            Assert.False(updated);
        }

        [Fact]
        public void Update_ShouldReturnFalse_WhenAttemptingToUpdateKeyInClearedMap()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(1, "initialValue");
            map.Clear();

            bool updated = map.Update(1, "newValue");

            Assert.False(updated);
        }

        [Fact]
        public void Update_ShouldWorkCorrectly_WhenUpdatingValueToEmptyString()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(1, "initialValue");

            bool updated = map.Update(1, "");

            Assert.True(updated);
            Assert.Equal("", map[1]);
        }

        [Fact]
        public void Update_ShouldHandleHighVolumeOfUpdates()
        {
            var map = new DenseMap<int, string>(16, 0.9);

            for (int i = 0; i < 1000; i++)
            {
                map.Emplace(i, $"initialValue{i}");
            }

            for (int i = 0; i < 1000; i++)
            {
                bool updated = map.Update(i, $"updatedValue{i}");
                Assert.True(updated);
                Assert.Equal($"updatedValue{i}", map[i]);
            }
        }
    }
}
