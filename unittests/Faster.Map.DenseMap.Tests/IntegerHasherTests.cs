using System;
using Xunit;
using System.Collections.Generic;
using Faster.Map.Hasher;
using Faster.Map.Core;
using Faster.Map.Hashing;

namespace Faster.Map.DenseMap.Tests
{
    public class IntegerHasherTests
    {    
        private DenseMap<int, string, XxHash3Hasher<int>> CreateMap() => new DenseMap<int, string, XxHash3Hasher<int>>(16, 0.875);

        [Fact]
        public void Emplace_AddsNewKeyValuePairs()
        {
            var map = CreateMap();
            map.InsertOrUpdate(1, "One");
            map.InsertOrUpdate(2, "Two");

            Assert.Equal(2, map.Count);
            Assert.Equal("One", map[1]);
            Assert.Equal("Two", map[2]);
        }

        [Fact]
        public void Emplace_UpdatesValueForExistingKey()
        {
            var map = CreateMap();
            map.InsertOrUpdate(1, "One");
            map.InsertOrUpdate(1, "UpdatedOne");

            Assert.Equal(1, map.Count);
            Assert.Equal("UpdatedOne", map[1]);
        }

        [Fact]
        public void Get_ReturnsValueIfExists()
        {
            var map = CreateMap();
            map.InsertOrUpdate(1, "One");

            Assert.True(map.Get(1, out var value));
            Assert.Equal("One", value);
        }

        [Fact]
        public void Get_ReturnsFalseIfKeyDoesNotExist()
        {
            var map = CreateMap();

            Assert.False(map.Get(99, out var value));
            Assert.Null(value);
        }

        [Fact]
        public void GetValueRefOrAddDefault_ReturnsReferenceToNewOrExistingValue()
        {
            var map = new DenseMap<int, int, XxHash3Hasher<int>>(16, 0.875);

            ref var refValue = ref map.GetValueRefOrAddDefault(1);
            refValue = 10;

            Assert.True(map.Get(1, out var storedValue));
            Assert.Equal(10, storedValue);
        }

        [Fact]
        public void Update_ChangesValueIfKeyExists()
        {
            var map = CreateMap();
            map.InsertOrUpdate(1, "One");

            Assert.True(map.Update(1, "UpdatedOne"));
            Assert.Equal("UpdatedOne", map[1]);
        }

        [Fact]
        public void Update_ReturnsFalseIfKeyDoesNotExist()
        {
            var map = CreateMap();

            Assert.False(map.Update(99, "ShouldNotBeAdded"));
        }

        [Fact]
        public void Remove_DeletesExistingKey()
        {
            var map = CreateMap();
            map.InsertOrUpdate(1, "One");
            Assert.True(map.Remove(1));
            Assert.False(map.Contains(1));
        }

        [Fact]
        public void Remove_ReturnsFalseIfKeyDoesNotExist()
        {
            var map = CreateMap();
            Assert.False(map.Remove(99));
        }

        [Fact]
        public void Contains_ReturnsTrueForExistingKey()
        {
            var map = CreateMap();
            map.InsertOrUpdate(1, "One");
            Assert.True(map.Contains(1));
        }

        [Fact]
        public void Contains_ReturnsFalseForNonExistingKey()
        {
            var map = CreateMap();
            Assert.False(map.Contains(99));
        }

        [Fact]
        public void Clear_RemovesAllEntries()
        {
            var map = CreateMap();
            map.InsertOrUpdate(1, "One");
            map.InsertOrUpdate(2, "Two");

            map.Clear();

            Assert.Equal(0, map.Count);
            Assert.False(map.Contains(1));
            Assert.False(map.Contains(2));
        }

        [Fact]
        public void Resize_IncreasesCapacityCorrectly()
        {
            var map = CreateMap();

            // Emplace more than initial capacity to force resize
            for (int i = 0; i < 20; i++)
            {
                map.InsertOrUpdate(i, $"Value{i}");
            }

            Assert.Equal(20, map.Count);

            for (int i = 0; i < 20; i++)
            {
                Assert.True(map.Contains(i));
                Assert.Equal($"Value{i}", map[i]);
            }
        }

        [Fact]
        public void Enumerator_ReturnsAllEntries()
        {
            var map = CreateMap();
            map.InsertOrUpdate(1, "One");
            map.InsertOrUpdate(2, "Two");

            var entries = new List<KeyValuePair<int, string>>(map.Entries);

            Assert.Equal(2, entries.Count);
            Assert.Contains(new KeyValuePair<int, string>(1, "One"), entries);
            Assert.Contains(new KeyValuePair<int, string>(2, "Two"), entries);
        }

        [Fact]
        public void Indexer_ThrowsKeyNotFoundExceptionForNonExistingKey()
        {
            var map = CreateMap();

            Assert.Throws<KeyNotFoundException>(() => map[99]);
        }

        [Fact]
        public void Indexer_Get_ReturnsValueForExistingKey()
        {
            var map = CreateMap();
            map.InsertOrUpdate(1, "One");

            Assert.Equal("One", map[1]);
        }

        [Fact]
        public void Indexer_Set_UpdatesValueForExistingKey()
        {
            var map = CreateMap();
            map.InsertOrUpdate(1, "One");

            map[1] = "UpdatedOne";

            Assert.Equal("UpdatedOne", map[1]);
        }

        [Fact]
        public void LoadFactorLimit_DoesNotExceed()
        {
            var map = new DenseMap<int, string, XxHash3Hasher<int>>(4, 0.75);

            map.InsertOrUpdate(1, "One");
            map.InsertOrUpdate(2, "Two");
            map.InsertOrUpdate(3, "Three");

            Assert.True(map.Count <= 4 * 0.75);
        }
    }
}