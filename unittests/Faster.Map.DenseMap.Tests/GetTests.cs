using System;
using Xunit;

namespace Faster.Map.DenseMap.Tests
{
    public class GetTests
    {
        [Fact]
        public void Get_ShouldReturnTrueAndCorrectValue_WhenKeyExists()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(1, "value1");
            Assert.True(map.Get(1, out var value));
            Assert.Equal("value1", value);
        }

        [Fact]
        public void Get_ShouldReturnFalseAndDefaultValue_WhenKeyDoesNotExist()
        {
            var map = new DenseMap<int, string>();
            Assert.False(map.Get(99, out var value));
            Assert.Null(value);
        }

        [Fact]
        public void Get_ShouldReturnFalseAndDefaultValue_WhenMapIsEmpty()
        {
            var map = new DenseMap<int, string>();
            Assert.False(map.Get(0, out var value));
            Assert.Null(value);
        }

        [Fact]
        public void Get_ShouldReturnCorrectValue_WhenKeyIsZero()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(0, "zeroValue");
            Assert.True(map.Get(0, out var value));
            Assert.Equal("zeroValue", value);
        }

        [Fact]
        public void Get_ShouldReturnTrueAndCorrectValue_ForNegativeKeys()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(-1, "negativeValue");
            Assert.True(map.Get(-1, out var value));
            Assert.Equal("negativeValue", value);
        }

        [Fact]
        public void Get_ShouldReturnDefault_WhenKeyIsRemoved()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(1, "value1");
            map.Remove(1);
            Assert.False(map.Get(1, out var value));
            Assert.Null(value);
        }

        [Fact]
        public void Get_ShouldReturnUpdatedValue_WhenKeyIsReinsertedAfterRemoval()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(1, "value1");
            map.Remove(1);
            map.Emplace(1, "newValue1");
            Assert.True(map.Get(1, out var value));
            Assert.Equal("newValue1", value);
        }

        [Fact]
        public void Get_ShouldReturnCorrectValue_ForMinAndMaxIntKeys()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(int.MinValue, "minValue");
            map.Emplace(int.MaxValue, "maxValue");
            Assert.True(map.Get(int.MinValue, out var minValue));
            Assert.Equal("minValue", minValue);
            Assert.True(map.Get(int.MaxValue, out var maxValue));
            Assert.Equal("maxValue", maxValue);
        }

        [Fact]
        public void Get_ShouldReturnDefault_ForNonExistentKeyInHighLoadMap()
        {
            var map = new DenseMap<int, string>(16, 0.9);
            for (int i = 0; i < 14; i++)
            {
                map.Emplace(i, $"value{i}");
            }
            Assert.False(map.Get(99, out var value));
            Assert.Null(value);
        }

        [Fact]
        public void Get_ShouldWorkCorrectly_AfterMapResize()
        {
            var map = new DenseMap<int, string>(4, 0.9);
            for (int i = 0; i < 5; i++)
            {
                map.Emplace(i, $"value{i}");
            }
            Assert.True(map.Get(3, out var value));
            Assert.Equal("value3", value);
            Assert.True(map.Size > 4);
        }

        [Fact]
        public void Get_ShouldReturnDefault_AfterClear()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(1, "value1");
            map.Clear();
            Assert.False(map.Get(1, out var value));
            Assert.Null(value);
        }

        [Fact]
        public void Get_ShouldReturnCorrectValue_ForCollidingKeys()
        {
            var map = new DenseMap<int, string>();
            int baseKey = 100;
            int[] collidingKeys = { baseKey, baseKey + 16, baseKey + 32 };
            foreach (var key in collidingKeys)
            {
                map.Emplace(key, $"value{key}");
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
            var map = new DenseMap<int, string>(16, 0.9);
            for (int i = 0; i < 1000; i++)
            {
                map.Emplace(i, $"value{i}");
            }
            Assert.True(map.Get(999, out var value));
            Assert.Equal("value999", value);
        }

        [Fact]
        public void Get_ShouldReturnCorrectValue_WhenKeyIsReinsertedAfterRemovalInCollisionChain()
        {
            var map = new DenseMap<int, string>();
            int baseKey = 50;
            int[] collidingKeys = { baseKey, baseKey + 16, baseKey + 32 };
            foreach (var key in collidingKeys)
            {
                map.Emplace(key, $"value{key}");
            }
            map.Remove(collidingKeys[2]);
            map.Emplace(collidingKeys[2], "newValue");

            Assert.True(map.Get(collidingKeys[2], out var value));
            Assert.Equal("newValue", value);
        }

        [Fact]
        public void Get_ShouldReturnTrue_AfterReinsertionInTombstonedSlot()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(1, "value1");
            map.Remove(1);
            map.Emplace(1, "newValue");

            Assert.True(map.Get(1, out var value));
            Assert.Equal("newValue", value);
        }

        [Fact]
        public void Get_ShouldReturnFalse_ForNonExistentKeyAfterHighVolumeInsertionsAndRemovals()
        {
            var map = new DenseMap<int, string>();
            for (int i = 0; i < 1000; i++)
            {
                map.Emplace(i, $"value{i}");
            }
            map.Remove(500);
            Assert.False(map.Get(500, out var value));
            Assert.Null(value);
        }

        [Fact]
        public void Get_ShouldReturnFalse_ForRemovedKeyInHighLoadMap()
        {
            var map = new DenseMap<int, string>(16, 0.9);
            for (int i = 0; i < 14; i++)
            {
                map.Emplace(i, $"value{i}");
            }
            map.Remove(10);
            Assert.False(map.Get(10, out var value));
            Assert.Null(value);
        }

        [Fact]
        public void Get_ShouldReturnCorrectValue_WhenBoundaryKeysArePresent()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(int.MinValue, "minValue");
            map.Emplace(int.MaxValue, "maxValue");

            Assert.True(map.Get(int.MinValue, out var minValue));
            Assert.Equal("minValue", minValue);
            Assert.True(map.Get(int.MaxValue, out var maxValue));
            Assert.Equal("maxValue", maxValue);
        }

        [Fact]
        public void Get_ShouldReturnDefault_WhenKeyNotInMap()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(1, "value1");
            Assert.False(map.Get(999, out var value));
            Assert.Null(value);
        }

        [Fact]
        public void Get_ShouldReturnCorrectValue_ForZeroKey()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(0, "zeroValue");

            Assert.True(map.Get(0, out var value));
            Assert.Equal("zeroValue", value);
        }

        [Fact]
        public void Get_ShouldReturnCorrectValue_ForNegativeKey()
        {
            var map = new DenseMap<int, string>();
            map.Emplace(-10, "negativeValue");

            Assert.True(map.Get(-10, out var value));
            Assert.Equal("negativeValue", value);
        }

        [Fact]
        public void Get_ShouldReturnCorrectValue_WhenReinsertingInDifferentGroupsAfterRemoval()
        {
            var map = new DenseMap<int, string>(32);

            // Insert multiple keys to different groups
            int[] keys = { 3, 19, 35 }; // Assuming these hash to different groups
            foreach (var key in keys)
            {
                map.Emplace(key, $"value{key}");
            }

            // Remove a key and reinsert
            map.Remove(19);
            map.Emplace(19, "newValue19");

            Assert.True(map.Get(19, out var value));
            Assert.Equal("newValue19", value);
        }

        [Fact]
        public void Get_ShouldReturnTrueForReinsertedKey_WhenUsingHighLoadFactorAndResizing()
        {
            var map = new DenseMap<int, string>(4, 0.9);
            for (int i = 0; i < 10; i++)
            {
                map.Emplace(i, $"value{i}");
            }
            map.Remove(8);
            map.Emplace(8, "newEight");
            Assert.True(map.Get(8, out var value));
            Assert.Equal("newEight", value);
        }

        [Fact]
        public void Get_ShouldReturn_WhileMapIsEmpty()
        {
            var dense = new DenseMap<int, int>();
            for (int i = 1; i <= 16; ++i)
            {
                dense.Emplace(i, 0);
                dense.Remove(i);
            }

            var result = dense.Get(0, out int value);

            Assert.False(result);
        }
    }
}
