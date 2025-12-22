using Faster.Map.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Faster.Map.BlitzMap.Tests;

public class EnumerableTests
{





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

        var count = 0;

        foreach (var item in map)
        {
            Assert.True(item.Key != default);
            Assert.True(item.Value != default);
            ++count;
        }
        

        Assert.True(map.Count == count); 
    }

    [Fact]
    public void Entries_ShouldNotBreakWithConcurrentModifications()
    {
        var map = new BlitzMap<int, string>();
        map.Insert(1, "One");
        map.Insert(2, "Two");

        var enumerator = map.GetEnumerator();
        enumerator.MoveNext();
        enumerator.MoveNext();

        map.Insert(3, "Three"); // Modify while iterating

        Assert.Equal(2, enumerator.Current.Key); // Iteration should still work
    }
}