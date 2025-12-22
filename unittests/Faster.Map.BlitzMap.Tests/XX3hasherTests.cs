using Faster.Map.Core;
using Faster.Map.Hashing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Faster.Map.BlitzMap.Tests;

public class XX3hasherIntTests
{
    private BlitzMap<int, string, XxHash3Hasher<int>> _map;

    public XX3hasherIntTests()
    {
        // Initialize with a small capacity to ensure resizing is tested
        _map = new BlitzMap<int, string, XxHash3Hasher<int>>(4);
    }

    [Fact]
    public void Insert_AddsNewKeyValuePair_IncreasesCount()
    {
        Assert.True(_map.Insert(1, "Value1"));
        Assert.Equal(1, _map.Count);
        Assert.True(_map.Insert(2, "Value2"));
        Assert.Equal(2, _map.Count);
    }

    [Fact]
    public void Insert_ReturnsFalseForDuplicateKey()
    {
        _map.Insert(1, "Value1");
        Assert.False(_map.Insert(1, "NewValue1")); // Attempt to insert duplicate
        Assert.Equal(1, _map.Count); // Count should not change
    }

    [Fact]
    public void Get_RetrievesExistingValue()
    {
        _map.Insert(10, "Ten");
        _map.Insert(20, "Twenty");

        Assert.True(_map.Get(10, out var value1));
        Assert.Equal("Ten", value1);

        Assert.True(_map.Get(20, out var value2));
        Assert.Equal("Twenty", value2);
    }

    [Fact]
    public void Get_ReturnsFalseForNonExistingKey()
    {
        _map.Insert(1, "Value1");
        Assert.False(_map.Get(99, out var value));
        Assert.Null(value); // Default for string is null
    }

    [Fact]
    public void Update_ModifiesExistingValue()
    {
        _map.Insert(5, "Original");
        Assert.True(_map.Update(5, "Updated"));

        Assert.True(_map.Get(5, out var value));
        Assert.Equal("Updated", value);
        Assert.Equal(1, _map.Count); // Count should remain the same
    }

    [Fact]
    public void Update_ReturnsFalseForNonExistingKey()
    {
        Assert.False(_map.Update(7, "NonExistent"));
        Assert.Equal(0, _map.Count);
    }

    [Fact]
    public void Remove_RemovesExistingKey_DecreasesCount()
    {
        _map.Insert(1, "A");
        _map.Insert(2, "B");
        _map.Insert(3, "C");
        Assert.Equal(3, _map.Count);

        Assert.True(_map.Remove(2));
        Assert.Equal(2, _map.Count);
        Assert.False(_map.Get(2, out _)); // Verify it's gone

        Assert.True(_map.Get(1, out var val1));
        Assert.Equal("A", val1);
        Assert.True(_map.Get(3, out var val3));
        Assert.Equal("C", val3);
    }

    [Fact]
    public void Remove_ReturnsFalseForNonExistingKey()
    {
        _map.Insert(1, "A");
        Assert.False(_map.Remove(99));
        Assert.Equal(1, _map.Count); // Count should not change
    }

    [Fact]
    public void InsertOrUpdate_InsertsNewKey()
    {
        Assert.True(_map.InsertOrUpdate(1, "Initial"));
        Assert.Equal(1, _map.Count);
        Assert.True(_map.Get(1, out var value));
        Assert.Equal("Initial", value);
    }

    [Fact]
    public void InsertOrUpdate_UpdatesExistingKey()
    {
        _map.Insert(1, "Original");
        Assert.True(_map.InsertOrUpdate(1, "Modified"));
        Assert.Equal(1, _map.Count); // Count should not change
        Assert.True(_map.Get(1, out var value));
        Assert.Equal("Modified", value);
    }

    [Fact]
    public void BlitzMap_ResizesCorrectly()
    {
        // Initial capacity 4, load factor 0.8 => resize at 4 * 0.8 = 3.2, so after 3 inserts
        // Actual _maxCountBeforeResize will be 3 (uint cast of 3.2)
        _map = new BlitzMap<int, string, XxHash3Hasher<int>>(4, 0.8);
        int initialSize = _map.Size; // Should be 4

        _map.Insert(1, "One");
        _map.Insert(2, "Two");
        _map.Insert(3, "Three");
        Assert.Equal(3, _map.Count);
        Assert.Equal(initialSize, _map.Size); // Should still be initial size

        // This insert should trigger a resize
        _map.Insert(4, "Four");
        Assert.Equal(4, _map.Count);
        Assert.True(_map.Size > initialSize); // Size should have doubled (8)

        // Verify all elements are still accessible after resize
        Assert.True(_map.Get(1, out var v1)); Assert.Equal("One", v1);
        Assert.True(_map.Get(2, out var v2)); Assert.Equal("Two", v2);
        Assert.True(_map.Get(3, out var v3)); Assert.Equal("Three", v3);
        Assert.True(_map.Get(4, out var v4)); Assert.Equal("Four", v4);
    }

    [Fact]
    public void Clear_ResetsMap()
    {
        _map.Insert(1, "A");
        _map.Insert(2, "B");
        Assert.Equal(2, _map.Count);

        _map.Clear();
        Assert.Equal(0, _map.Count);
        Assert.False(_map.Get(1, out _));
        Assert.False(_map.Get(2, out _));
        // Ensure buckets are reset to INACTIVE
        Assert.False(_map.Contains(1));
        Assert.False(_map.Contains(2));
    }

    [Fact]
    public void Indexer_Get_RetrievesValue()
    {
        _map.Insert(100, "Hundred");
        Assert.Equal("Hundred", _map[100]);
    }

    [Fact]
    public void Indexer_Get_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => _map[200]);
    }

    [Fact]
    public void Indexer_Set_InsertsValue()
    {
        _map[300] = "ThreeHundred";
        Assert.Equal(1, _map.Count);
        Assert.Equal("ThreeHundred", _map[300]);
    }

    [Fact]
    public void Indexer_Set_UpdatesValue()
    {
        _map[300] = "First";
        _map[300] = "Second";
        Assert.Equal(1, _map.Count); // Count should not change
        Assert.Equal("Second", _map[300]);
    }

    [Fact]
    public void Enumerate_AllEntries()
    {
        _map.Insert(1, "One");
        _map.Insert(2, "Two");
        _map.Insert(3, "Three");

        var entries = new List<string>();
        foreach (var entry in _map)
        {
            entries.Add(entry.Value);
        }

        Assert.Equal(3, entries.Count);
        Assert.Contains("One", entries);
        Assert.Contains("Two", entries);
        Assert.Contains("Three", entries);
    }
}

public class XX3hasherStringTests
{
    private BlitzMap<string, int, XxHash3StringHasher> _map;

    public XX3hasherStringTests()
    {
        _map = new BlitzMap<string, int, XxHash3StringHasher>(4);
    }

    [Fact]
    public void Insert_AddsNewKeyValuePair_IncreasesCount()
    {
        Assert.True(_map.Insert("KeyA", 1));
        Assert.Equal(1, _map.Count);
        Assert.True(_map.Insert("KeyB", 2));
        Assert.Equal(2, _map.Count);
    }

    [Fact]
    public void Insert_ReturnsFalseForDuplicateKey()
    {
        _map.Insert("KeyA", 1);
        Assert.False(_map.Insert("KeyA", 100));
        Assert.Equal(1, _map.Count);
    }

    [Fact]
    public void Get_RetrievesExistingValue()
    {
        _map.Insert("Apple", 10);
        _map.Insert("Banana", 20);

        Assert.True(_map.Get("Apple", out var value1));
        Assert.Equal(10, value1);

        Assert.True(_map.Get("Banana", out var value2));
        Assert.Equal(20, value2);
    }

    [Fact]
    public void Get_ReturnsFalseForNonExistingKey()
    {
        _map.Insert("Orange", 5);
        Assert.False(_map.Get("Grape", out var value));
        Assert.Equal(0, value); // Default for int is 0
    }

    [Fact]
    public void Update_ModifiesExistingValue()
    {
        _map.Insert("Fruit", 100);
        Assert.True(_map.Update("Fruit", 200));

        Assert.True(_map.Get("Fruit", out var value));
        Assert.Equal(200, value);
        Assert.Equal(1, _map.Count);
    }

    [Fact]
    public void Update_ReturnsFalseForNonExistingKey()
    {
        Assert.False(_map.Update("Vegetable", 50));
        Assert.Equal(0, _map.Count);
    }

    [Fact]
    public void Remove_RemovesExistingKey_DecreasesCount()
    {
        _map.Insert("One", 1);
        _map.Insert("Two", 2);
        _map.Insert("Three", 3);
        Assert.Equal(3, _map.Count);

        Assert.True(_map.Remove("Two"));
        Assert.Equal(2, _map.Count);
        Assert.False(_map.Get("Two", out _));

        Assert.True(_map.Get("One", out var val1)); Assert.Equal(1, val1);
        Assert.True(_map.Get("Three", out var val3)); Assert.Equal(3, val3);
    }

    [Fact]
    public void Remove_ReturnsFalseForNonExistingKey()
    {
        _map.Insert("Test", 123);
        Assert.False(_map.Remove("NonExistentTest"));
        Assert.Equal(1, _map.Count);
    }

    [Fact]
    public void InsertOrUpdate_InsertsNewKey()
    {
        Assert.True(_map.InsertOrUpdate("NewKey", 10));
        Assert.Equal(1, _map.Count);
        Assert.True(_map.Get("NewKey", out var value));
        Assert.Equal(10, value);
    }

    [Fact]
    public void InsertOrUpdate_UpdatesExistingKey()
    {
        _map.Insert("ExistingKey", 50);
        Assert.True(_map.InsertOrUpdate("ExistingKey", 150));
        Assert.Equal(1, _map.Count);
        Assert.True(_map.Get("ExistingKey", out var value));
        Assert.Equal(150, value);
    }

    [Fact]
    public void BlitzMap_ResizesCorrectly_StringKeys()
    {
        _map = new BlitzMap<string, int, XxHash3StringHasher>(4, 0.8);
        int initialSize = _map.Size;

        _map.Insert("A", 1);
        _map.Insert("B", 2);
        _map.Insert("C", 3);
        Assert.Equal(3, _map.Count);
        Assert.Equal(initialSize, _map.Size);

        _map.Insert("D", 4); // Should trigger resize
        Assert.Equal(4, _map.Count);
        Assert.True(_map.Size > initialSize);

        Assert.True(_map.Get("A", out var vA)); Assert.Equal(1, vA);
        Assert.True(_map.Get("B", out var vB)); Assert.Equal(2, vB);
        Assert.True(_map.Get("C", out var vC)); Assert.Equal(3, vC);
        Assert.True(_map.Get("D", out var vD)); Assert.Equal(4, vD);
    }

    [Fact]
    public void Clear_ResetsMap_StringKeys()
    {
        _map.Insert("K1", 1);
        _map.Insert("K2", 2);
        Assert.Equal(2, _map.Count);

        _map.Clear();
        Assert.Equal(0, _map.Count);
        Assert.False(_map.Get("K1", out _));
        Assert.False(_map.Get("K2", out _));
    }

    [Fact]
    public void Indexer_Get_RetrievesValue_StringKeys()
    {
        _map.Insert("Hello", 100);
        Assert.Equal(100, _map["Hello"]);
    }

    [Fact]
    public void Indexer_Get_ThrowsKeyNotFoundException_StringKeys()
    {
        Assert.Throws<KeyNotFoundException>(() => _map["World"]);
    }

    [Fact]
    public void Indexer_Set_InsertsValue_StringKeys()
    {
        _map["NewString"] = 300;
        Assert.Equal(1, _map.Count);
        Assert.Equal(300, _map["NewString"]);
    }

    [Fact]
    public void Indexer_Set_UpdatesValue_StringKeys()
    {
        _map["UpdateMe"] = 1;
        _map["UpdateMe"] = 2;
        Assert.Equal(1, _map.Count);
        Assert.Equal(2, _map["UpdateMe"]);
    }

    [Fact]
    public void Enumerate_AllEntries_StringKeys()
    {
        _map.Insert("One", 1);
        _map.Insert("Two", 2);
        _map.Insert("Three", 3);

        var entries = new List<int>();
        foreach (var entry in _map)
        {
            entries.Add(entry.Value);
        }

        Assert.Equal(3, entries.Count);
        Assert.Contains(1, entries);
        Assert.Contains(2, entries);
        Assert.Contains(3, entries);
    }
}