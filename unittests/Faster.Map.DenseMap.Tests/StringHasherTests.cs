using System;
using Xunit;
using Faster.Map;
using Faster.Map.Core;
using System.Collections.Generic;
using Faster.Map.Hasher;

namespace Faster.Map.DenseMap.Tests
{
    public class StringHasherTests
    {
        private DenseMap<string, string> _map;

        public StringHasherTests()
        {
            _map = new DenseMap<string, string>(16, 0.875, new StringHasher());
        }

        [Fact]
        public void Emplace_AddsNewStringKeyValuePairs()
        {
            _map.Emplace("key1", "value1");
            _map.Emplace("key2", "value2");

            Assert.Equal(2, _map.Count);
            Assert.Equal("value1", _map["key1"]);
            Assert.Equal("value2", _map["key2"]);
        }

        [Fact]
        public void Emplace_UpdatesValueForExistingStringKey()
        {       
            _map.Emplace("key1", "value1");
            _map.Emplace("key1", "updatedValue1");

            Assert.Equal(1, _map.Count);
            Assert.Equal("updatedValue1", _map["key1"]);
        }

        [Fact]
        public void Get_ReturnsValueIfStringKeyExists()
        {       
            _map.Emplace("key1", "value1");

            Assert.True(_map.Get("key1", out var value));
            Assert.Equal("value1", value);
        }

        [Fact]
        public void Get_ReturnsFalseIfStringKeyDoesNotExist()
        {
       

            Assert.False(_map.Get("nonexistent", out var value));
            Assert.Null(value);
        }

        [Fact]
        public void Update_ChangesValueIfStringKeyExists()
        {
       
            _map.Emplace("key1", "value1");

            Assert.True(_map.Update("key1", "updatedValue1"));
            Assert.Equal("updatedValue1", _map["key1"]);
        }

        [Fact]
        public void Update_ReturnsFalseIfStringKeyDoesNotExist()
        {      
            Assert.False(_map.Update("nonexistent", "shouldNotBeAdded"));
        }

        [Fact]
        public void Remove_DeletesExistingStringKey()
        {       
            _map.Emplace("key1", "value1");

            Assert.True(_map.Remove("key1"));
            Assert.False(_map.Contains("key1"));
        }

        [Fact]
        public void Remove_ReturnsFalseIfStringKeyDoesNotExist()
        {      
            Assert.False(_map.Remove("nonexistent"));
        }

        [Fact]
        public void Contains_ReturnsTrueForExistingStringKey()
        {       
            _map.Emplace("key1", "value1");

            Assert.True(_map.Contains("key1"));
        }

        [Fact]
        public void Contains_ReturnsFalseForNonExistingStringKey()
        {
            Assert.False(_map.Contains("nonexistent"));
        }

        [Fact]
        public void Clear_RemovesAllStringEntries()
        {       
            _map.Emplace("key1", "value1");
            _map.Emplace("key2", "value2");

            _map.Clear();

            Assert.Equal(0, _map.Count);
            Assert.False(_map.Contains("key1"));
            Assert.False(_map.Contains("key2"));
        }

        [Fact]
        public void Resize_IncreasesCapacityCorrectlyForStringKeys()
        {    
            for (int i = 0; i < 20; i++)
            {
                _map.Emplace($"key{i}", $"value{i}");
            }

            Assert.Equal(20, _map.Count);

            for (int i = 0; i < 20; i++)
            {
                Assert.True(_map.Contains($"key{i}"));
                Assert.Equal($"value{i}", _map[$"key{i}"]);
            }
        }

        [Fact]
        public void Enumerator_ReturnsAllStringEntries()
        {       
            _map.Emplace("key1", "value1");
            _map.Emplace("key2", "value2");

            var entries = new List<KeyValuePair<string, string>>(_map.Entries);

            Assert.Equal(2, entries.Count);
            Assert.Contains(new KeyValuePair<string, string>("key1", "value1"), entries);
            Assert.Contains(new KeyValuePair<string, string>("key2", "value2"), entries);
        }

        [Fact]
        public void Indexer_ThrowsKeyNotFoundExceptionForNonExistingStringKey()
        {      
            Assert.Throws<KeyNotFoundException>(() => _map["nonexistent"]);
        }

        [Fact]
        public void Indexer_Get_ReturnsValueForExistingStringKey()
        {       
            _map.Emplace("key1", "value1");

            Assert.Equal("value1", _map["key1"]);
        }

        [Fact]
        public void Indexer_Set_UpdatesValueForExistingStringKey()
        {       
            _map.Emplace("key1", "value1");

            _map["key1"] = "updatedValue1";

            Assert.Equal("updatedValue1", _map["key1"]);
        }

        [Fact]
        public void LoadFactorLimit_DoesNotExceedForStringKeys()
        {
            var result = new DenseMap<string, string>(4, 0.75, new StringHasher());

            result.Emplace("key1", "value1");
            result.Emplace("key2", "value2");
            result.Emplace("key3", "value3");

            Assert.True(result.Count <= 4 * 0.75);
        }
    }

}
