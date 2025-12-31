using Faster.Map.Core;
using Xunit;

namespace Faster.Map.BlitzMap.Tests;

public class EnumerableTests
{

    [Fact]
    public void Enumerate_Empty_YieldsNothing()
    {
        var map = new BlitzMap<int, int>();

        int seen = 0;
        foreach (ref readonly var e in map)
        {
            _ = e;
            seen++;
        }

        Assert.Equal(0, seen);
        Assert.Equal(0, map.Count);
    }

    [Fact]
    public void Enumerate_YieldsAllPairs_NoDuplicates()
    {
        var rnd = new Random(123);
        var map = new BlitzMap<int, int>(2);
        var dict = new Dictionary<int, int>();

        for (int i = 0; i < 5000; i++)
        {
            int k = rnd.Next(0, 8000);
            int v = rnd.Next();
            map.InsertOrUpdate(k, v);
            dict[k] = v;
        }

        var keys = new HashSet<int>();
        int seen = 0;

        foreach (ref readonly var e in map)
        {
            Assert.True(keys.Add(e.Key)); // no duplicate keys from enumeration
            Assert.True(dict.TryGetValue(e.Key, out var dv));
            Assert.Equal(dv, e.Value);

            Assert.True(map.Get(e.Key, out var mv));
            Assert.Equal(dv, mv);

            seen++;
        }

        Assert.Equal(dict.Count, seen);
        Assert.Equal(dict.Count, keys.Count);
        Assert.Equal(dict.Count, map.Count);
    }

    [Fact]
    public void Enumerate_ReflectsUpdates()
    {
        var map = new BlitzMap<int, int>(2);
        var dict = new Dictionary<int, int>();

        for (int i = 0; i < 2000; i++)
        {
            int k = i * 7 + 3;
            int v = i;
            map.InsertOrUpdate(k, v);
            dict[k] = v;
        }

        for (int i = 0; i < 2000; i += 2)
        {
            int k = i * 7 + 3;
            int v = i ^ 0x5a5a5a5a;
            map.InsertOrUpdate(k, v);
            dict[k] = v;
        }

        foreach (ref readonly var e in map)
        {
            Assert.True(dict.TryGetValue(e.Key, out var dv));
            Assert.Equal(dv, e.Value);
        }

        Assert.Equal(dict.Count, map.Count);
    }

    [Fact]
    public void Enumerate_AfterRemovals_MatchesDictionary()
    {
        var rnd = new Random(42);
        var map = new BlitzMap<int, int>(2);
        var dict = new Dictionary<int, int>();

        for (int i = 0; i < 6000; i++)
        {
            int k = rnd.Next(0, 10000);
            int v = rnd.Next();
            map.InsertOrUpdate(k, v);
            dict[k] = v;
        }

        for (int i = 0; i < 3000; i++)
        {
            int k = rnd.Next(0, 10000);
            Assert.Equal(dict.Remove(k), map.Remove(k));
        }

        var keys = new HashSet<int>();
        int seen = 0;

        foreach (ref readonly var e in map)
        {
            Assert.True(keys.Add(e.Key));
            Assert.True(dict.TryGetValue(e.Key, out var dv));
            Assert.Equal(dv, e.Value);
            seen++;
        }

        Assert.Equal(dict.Count, seen);
        Assert.Equal(dict.Count, map.Count);
    }

    [Fact]
    public void Enumerate_AfterClear_YieldsNothing()
    {
        var map = new BlitzMap<int, int>(2);

        for (int i = 0; i < 1000; i++)
        {
            map.InsertOrUpdate(i, i * 10);
        }

        map.Clear();

        int seen = 0;
        foreach (ref readonly var e in map)
        {
            _ = e;
            seen++;
        }

        Assert.Equal(0, seen);
        Assert.Equal(0, map.Count);
    }

    [Fact]
    public void Enumerate_AfterResize_YieldsAllPairs()
    {
        var map = new BlitzMap<int, int>(2, 0.80);
        var dict = new Dictionary<int, int>();

        // Force multiple resizes.
        for (int i = 0; i < 50_000; i++)
        {
            int k = i;
            int v = i ^ 0x1234567;
            map.InsertOrUpdate(k, v);
            dict[k] = v;
        }

        var keys = new HashSet<int>();
        int seen = 0;

        foreach (ref readonly var e in map)
        {
            Assert.True(keys.Add(e.Key));
            Assert.True(dict.TryGetValue(e.Key, out var dv));
            Assert.Equal(dv, e.Value);
            seen++;
        }

        Assert.Equal(dict.Count, seen);
        Assert.Equal(dict.Count, map.Count);
    }

    [Fact]
    public void Enumerate_ThenCopy_PreservesAllPairs()
    {
        var src = new BlitzMap<int, int>(2);
        var dst = new BlitzMap<int, int>(2);
        var dict = new Dictionary<int, int>();

        for (int i = 0; i < 5000; i++)
        {
            int k = i * 11 + 1;
            int v = i ^ 0x7f7f7f7f;
            src.InsertOrUpdate(k, v);
            dict[k] = v;
        }

        dst.Copy(src);

        foreach (var kv in dict)
        {
            Assert.True(dst.Get(kv.Key, out var v));
            Assert.Equal(kv.Value, v);
        }

        Assert.Equal(dict.Count, dst.Count);
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