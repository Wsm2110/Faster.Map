using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Faster.Map.BlitzMap.Tests;

public class EnumerableTests
{

    [Fact]
    public void Entries_ShouldReturnEmpty_WhenMapIsEmpty()
    {
        var map = new BlitzMap<int, string>();
        Assert.Empty(map.Entries);
    }

    [Fact]
    public void Entries_ShouldReturnSingleEntry_WhenOneEntryExists()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "One");

        var result = Assert.Single(map.Entries);
        Assert.Equal(1, result.Key);
        Assert.Equal("One", result.Value);
    }

    [Fact]
    public void Entries_ShouldNotIncludeDeletedEntries()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "One");
        map.Insert(2, "Two");
        map.Insert(3, "Three");

        map.Remove(2);

        var result = map.Entries;

        Assert.Equal(2, result.Count());
    }


    [Fact]
    public void Entries_ShouldNotBeEmpty()
    {
        var Length = 134_217_728;
        var rnd = new FastRandom(6);
        var uni = new HashSet<uint>((int)Length);
        while (uni.Count < (uint)(Length * 0.1))
        {
            uni.Add((uint)rnd.Next());
        }
        var keys = uni.ToArray();
        var map = new BlitzMap<uint, uint>();
        foreach (var item in keys)
        {
            map.Insert(item, item);
        }

        foreach (var item in map)
        {
            Assert.True(item.Key != default);
            Assert.True(item.Value != default);
        }

        Assert.True(map.Count == map.Entries.Count()); 
    }

    [Fact]
    public void Entries_ShouldNotBreakWithConcurrentModifications()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "One");
        map.Insert(2, "Two");

        var enumerator = map.Entries.GetEnumerator();
        enumerator.MoveNext();
        enumerator.MoveNext();

        map.Insert(3, "Three"); // Modify while iterating

        Assert.Equal(2, enumerator.Current.Key); // Iteration should still work
    }
}