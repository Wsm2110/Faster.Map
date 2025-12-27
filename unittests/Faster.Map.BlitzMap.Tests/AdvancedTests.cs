using Faster.Map.Contracts;
using Faster.Map.Core;
using Xunit;

namespace Faster.Map.BlitzMap.Tests;

/// <summary>
/// Comprehensive test suite for BlitzMap.
/// Focuses on correctness, invariants, resize safety, collision handling,
/// and real-world usage patterns.
/// </summary>
public class AdvancedTests
{
    #region Basic CRUD

    [Fact]
    public void Insert_And_Get_ReturnsValue()
    {
        var map = new BlitzMap<int, string>();

        Assert.True(map.Insert(42, "answer"));
        Assert.True(map.Get(42, out var value));
        Assert.Equal("answer", value);
        Assert.Equal(1, map.Count);
    }

    [Fact]
    public void Get_MissingKey_ReturnsFalse()
    {
        var map = new BlitzMap<int, string>();

        Assert.False(map.Get(99, out _));
        Assert.False(map.Contains(99));
        Assert.Equal(0, map.Count);
    }

    #endregion

    #region Duplicate Handling

    [Fact]
    public void Insert_DuplicateKey_ReturnsFalse_AndKeepsOriginalValue()
    {
        var map = new BlitzMap<int, int>();

        Assert.True(map.Insert(1, 10));
        Assert.False(map.Insert(1, 20));

        Assert.True(map.Get(1, out var value));
        Assert.Equal(10, value);
        Assert.Equal(1, map.Count);
    }

    #endregion

    #region Update / InsertOrUpdate

    [Fact]
    public void Update_ExistingKey_ReplacesValue()
    {
        var map = new BlitzMap<int, int>();

        map.Insert(5, 100);
        Assert.True(map.Update(5, 200));

        map.Get(5, out var value);
        Assert.Equal(200, value);
    }

    [Fact]
    public void Update_MissingKey_ReturnsFalse()
    {
        var map = new BlitzMap<int, int>();
        Assert.False(map.Update(5, 123));
    }

    [Fact]
    public void InsertOrUpdate_Inserts_WhenMissing()
    {
        var map = new BlitzMap<int, int>();

        Assert.True(map.InsertOrUpdate(7, 77));
        map.Get(7, out var value);

        Assert.Equal(77, value);
        Assert.Equal(1, map.Count);
    }

    [Fact]
    public void InsertOrUpdate_Updates_WhenExists()
    {
        var map = new BlitzMap<int, int>();

        map.Insert(7, 77);
        map.InsertOrUpdate(7, 88);

        map.Get(7, out var value);
        Assert.Equal(88, value);
        Assert.Equal(1, map.Count);
    }

    #endregion

    #region Remove / Compaction

    [Fact]
    public void Remove_ExistingKey_RemovesEntry_AndCompacts()
    {
        var map = new BlitzMap<int, int>();

        map.Insert(1, 10);
        map.Insert(2, 20);
        map.Insert(3, 30);

        Assert.True(map.Remove(2));
        Assert.False(map.Contains(2));
        Assert.Equal(2, map.Count);

        Assert.True(map.Get(1, out _));
        Assert.True(map.Get(3, out _));
    }

    [Fact]
    public void Remove_MissingKey_ReturnsFalse()
    {
        var map = new BlitzMap<int, int>();
        Assert.False(map.Remove(123));
    }

    #endregion

    #region Collision Handling (Worst-Case Buckets)

    [Fact]
    public void Handles_HeavyCollisions_Correctly()
    {
        var map = new BlitzMap<int, int>(length: 4, loadfactor: 0.9);

        // Force collisions by mapping keys to same bucket
        for (int i = 0; i < 1_000; i++)
        {
            map.Insert(i * 4, i);
        }

        for (int i = 0; i < 1_000; i++)
        {
            Assert.True(map.Get(i * 4, out var value));
            Assert.Equal(i, value);
        }

        Assert.Equal(1_000, map.Count);
    }

    #endregion

    #region Resize Safety (Most Important)

    [Fact]
    public void Resize_Preserves_AllEntries()
    {
        var map = new BlitzMap<int, int>(length: 2, loadfactor: 0.75);

        const int count = 10_000;

        for (int i = 0; i < count; i++)
        {
            map.Insert(i, i * 2);
        }

        for (int i = 0; i < count; i++)
        {
            Assert.True(map.Get(i, out var value));
            Assert.Equal(i * 2, value);
        }

        Assert.Equal(count, map.Count);
    }

    #endregion

    #region Enumerator (SpanEnumerator)

    [Fact]
    public void Enumerator_Iterates_AllEntries_ExactlyOnce()
    {
        var map = new BlitzMap<int, int>();

        for (int i = 0; i < 100; i++)
            map.Insert(i, i * 10);

        var seen = new HashSet<int>();

        foreach (ref readonly var entry in map)
        {
            Assert.True(seen.Add(entry.Key));
            Assert.Equal(entry.Key * 10, entry.Value);
        }

        Assert.Equal(100, seen.Count);
    }

    #endregion

    #region Real-World Scenarios

    private sealed record User(int Id, string Name);

    [Fact]
    public void Acts_As_ObjectCache()
    {
        var cache = new BlitzMap<int, User>();

        cache.Insert(1, new User(1, "Alice"));
        cache.Insert(2, new User(2, "Bob"));

        Assert.True(cache.Get(2, out var user));
        Assert.Equal("Bob", user.Name);
    }

    [Fact]
    public void Acts_As_SessionStore()
    {
        var sessions = new BlitzMap<Guid, DateTime>();

        var id = Guid.NewGuid();
        var expires = DateTime.UtcNow.AddMinutes(30);

        sessions.Insert(id, expires);

        Assert.True(sessions.Get(id, out var stored));
        Assert.Equal(expires, stored);
    }

    #endregion

    #region Clear / Reset

    [Fact]
    public void Clear_RemovesEverything()
    {
        var map = new BlitzMap<int, int>();

        for (int i = 0; i < 100; i++)
            map.Insert(i, i);

        map.Clear();

        Assert.Equal(0, map.Count);
        Assert.False(map.Get(1, out _));
        Assert.False(map.Contains(50));
    }

    #endregion

    #region 🔥 Randomized Fuzz Tests

    [Fact]
    public void Fuzz_RandomizedOperations_MatchDictionary()
    {
        const int ops = 50_000;
        var rnd = new Random(12345);

        var map = new BlitzMap<int, int>();
        var dict = new Dictionary<int, int>();

        for (int i = 0; i < ops; i++)
        {
            int key = rnd.Next(0, 10_000);
            int value = rnd.Next();

            switch (rnd.Next(4))
            {
                case 0: // Insert
                    bool mapInserted = map.Insert(key, value);
                    bool dictInserted = dict.TryAdd(key, value);
                    Assert.Equal(dictInserted, mapInserted);
                    break;

                case 1: // Update
                    bool mapUpdated = map.Update(key, value);
                    bool dictUpdated = dict.ContainsKey(key);
                    if (dictUpdated) dict[key] = value;
                    Assert.Equal(dictUpdated, mapUpdated);
                    break;

                case 2: // Remove
                    bool mapRemoved = map.Remove(key);
                    bool dictRemoved = dict.Remove(key);
                    Assert.Equal(dictRemoved, mapRemoved);
                    break;

                case 3: // Get
                    bool mapFound = map.Get(key, out var mv);
                    bool dictFound = dict.TryGetValue(key, out var dv);
                    Assert.Equal(dictFound, mapFound);
                    if (mapFound) Assert.Equal(dv, mv);
                    break;
            }
        }

        Assert.Equal(dict.Count, map.Count);
    }

    #endregion

    #region 🔥 Dictionary Parity (Exhaustive)

    [Fact]
    public void DictionaryParity_AllOperationsMatch()
    {
        var map = new BlitzMap<int, int>();
        var dict = new Dictionary<int, int>();

        for (int i = 0; i < 10_000; i++)
        {
            map.Insert(i, i * 3);
            dict.Add(i, i * 3);
        }

        for (int i = 0; i < 10_000; i++)
        {
            Assert.True(map.Get(i, out var mv));
            Assert.Equal(dict[i], mv);
        }

        for (int i = 0; i < 5_000; i++)
        {
            Assert.True(map.Remove(i));
            Assert.True(dict.Remove(i));
        }

        for (int i = 0; i < 10_000; i++)
        {
            bool m = map.Contains(i);
            bool d = dict.ContainsKey(i);
            Assert.Equal(d, m);
        }
    }

    #endregion

    #region 🔥 Stress Tests (Millions of Ops)

    [Fact]
    public void Stress_Insert_And_Get_OneMillion()
    {
        const int count = 1_000_000;
        var map = new BlitzMap<int, int>(count, 0.9);

        for (int i = 0; i < count; i++)
            map.Insert(i, i);

        Assert.Equal(count, map.Count);

        for (int i = 0; i < count; i++)
        {
            Assert.True(map.Get(i, out var v));
            Assert.Equal(i, v);
        }
    }

    #endregion

    #region 🔥 API Misuse Tests (Defensive)

    [Fact]
    public void Remove_Twice_DoesNotCorrupt()
    {
        var map = new BlitzMap<int, int>();

        map.Insert(1, 1);
        Assert.True(map.Remove(1));
        Assert.False(map.Remove(1));

        Assert.Equal(0, map.Count);
    }

    [Fact]
    public void Update_OnEmptyMap_IsSafe()
    {
        var map = new BlitzMap<int, int>();
        Assert.False(map.Update(42, 100));
    }

    [Fact]
    public void InsertOrUpdate_OnEmptyMap_Works()
    {
        var map = new BlitzMap<int, int>();
        Assert.True(map.InsertOrUpdate(1, 10));
        Assert.True(map.Get(1, out var v));
        Assert.Equal(10, v);
    }

    #endregion

    #region 🔥 Unsafe Memory Invariants

    [Fact]
    public void Enumerator_DoesNotReturnDefaultEntries()
    {
        var map = new BlitzMap<int, int>();

        // Start at 1 so default(int)=0 can never appear as a real key
        for (int i = 1; i <= 1000; i++)
            map.Insert(i, i);

        int count = 0;
        foreach (ref readonly var e in map)
        {
            Assert.NotEqual(default(int), e.Key);
            count++;
        }

        Assert.Equal(map.Count, count);
    }


    [Fact]
    public void Remove_DoesNotBreak_OtherBuckets()
    {
        var map = new BlitzMap<int, int>(4, 0.9);

        // Force chains
        for (int i = 0; i < 100; i++)
            map.Insert(i * 4, i);

        // Remove middle entries
        for (int i = 20; i < 40; i++)
            Assert.True(map.Remove(i * 4));

        // Remaining entries must still resolve correctly
        for (int i = 0; i < 100; i++)
        {
            bool shouldExist = i < 20 || i >= 40;
            bool exists = map.Get(i * 4, out var v);

            Assert.Equal(shouldExist, exists);
            if (exists) Assert.Equal(i, v);
        }
    }

    [Fact]
    public void IdCache_InsertGetUpdateRemove_Works()
    {
        var map = new BlitzMap<int, string>();

        map.Insert(1, "A");
        map.Insert(2, "B");

        Assert.True(map.Get(1, out var v1));
        Assert.Equal("A", v1);

        Assert.True(map.Update(1, "A2"));
        Assert.Equal("A2", map[1]);

        Assert.True(map.Remove(1));
        Assert.False(map.Contains(1));

        Assert.Equal(1, map.Count);
    }

    [Fact]
    public void InsertOrUpdate_ReplacesValueCorrectly()
    {
        var map = new BlitzMap<int, int>();

        map.InsertOrUpdate(10, 1);
        map.InsertOrUpdate(10, 2);
        map.InsertOrUpdate(10, 3);

        Assert.Equal(3, map[10]);
        Assert.Equal(1, map.Count);
    }

    [Fact]
    public void TelemetryCounterPattern()
    {
        var map = new BlitzMap<int, int>();

        for (int i = 0; i < 1_000; i++)
        {
            if (map.Get(i, out var v))
                map.Update(i, v + 1);
            else
                map.Insert(i, 1);
        }

        for (int i = 0; i < 1_000; i++)
            Assert.Equal(1, map[i]);
    }

    [Fact]
    public void Parity_With_Dictionary_RandomOps()
    {
        var rnd = new Random(123);
        var map = new BlitzMap<int, int>();
        var dict = new Dictionary<int, int>();

        for (int i = 0; i < 100_000; i++)
        {
            int key = rnd.Next(512);

            switch (rnd.Next(4))
            {
                case 0:
                    map.InsertOrUpdate(key, key);
                    dict[key] = key;
                    break;

                case 1:

                    map.Remove(key);
                    dict.Remove(key);
                    break;

                case 2:

                    var result = map.Get(key, out var mv);

                    Assert.Equal(dict.TryGetValue(key, out var dv), result);

                    if (dict.ContainsKey(key))
                        Assert.Equal(dv, mv);
                    break;

                case 3:
                    Assert.Equal(dict.ContainsKey(key), map.Contains(key));
                    break;
            }
        }

        Assert.Equal(dict.Count, map.Count);
    }

    [Fact]
    public void Stress_MillionEntries()
    {
        var map = new BlitzMap<int, int>(16);

        for (int i = 0; i < 1_000_000; i++)
            map.Insert(i, i);

        for (int i = 0; i < 1_000_000; i++)
            Assert.Equal(i, map[i]);

        Assert.Equal(1_000_000, map.Count);
    }

    [Fact]
    public void Remove_AfterResize_DoesNotCorrupt()
    {
        var map = new BlitzMap<int, int>();

        for (int i = 0; i < 100_000; i++)
            map.Insert(i, i);

        for (int i = 0; i < 100_000; i += 2)
            map.Remove(i);

        for (int i = 0; i < 100_000; i++)
        {
            if ((i & 1) == 0)
                Assert.False(map.Contains(i));
            else
                Assert.Equal(i, map[i]);
        }
    }

    [Fact]
    public void Enumerator_AllEntriesAreReachable()
    {
        var map = new BlitzMap<int, int>();

        for (int i = 0; i < 10_000; i++)
            map.Insert(i, i * 2);

        int count = 0;
        foreach (ref readonly var e in map)
        {
            Assert.True(map.Get(e.Key, out var v));
            Assert.Equal(e.Value, v);
            count++;
        }

        Assert.Equal(map.Count, count);
    }

    [Fact]
    public void Enumerator_SkipsRemovedEntries()
    {
        var map = new BlitzMap<int, int>();

        for (int i = 0; i < 1000; i++)
            map.Insert(i, i);

        for (int i = 0; i < 1000; i += 3)
            map.Remove(i);

        foreach (ref readonly var e in map)
            Assert.True(e.Key % 3 != 0);
    }

    [Fact]
    public void Remove_NonExistentKey_ReturnsFalse()
    {
        var map = new BlitzMap<int, int>();
        Assert.False(map.Remove(42));
    }

    [Fact]
    public void Assert_Kickouts_WorkCorrectly()
    {
        var map = new BlitzMap<int, int>(length: 16, loadfactor: 0.75);
        
        // assign all to bucket 5
        map.Insert(5, 5);
        map.Insert(21, 21);
        map.Insert(37, 37);
        map.Insert(53, 53);
        map.Insert(69, 69);
        map.Insert(85, 85);

        map.Insert(8, 8);

        Assert.True(map.Get(8, out var value));
        Assert.True(map.Get(5, out value));
        Assert.True(map.Get(21, out value));
        Assert.True(map.Get(37, out value));
        Assert.True(map.Get(53, out value));
        Assert.True(map.Get(69, out value));
        Assert.True(map.Get(85, out value));

    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var map = new BlitzMap<int, int>();

        for (int i = 0; i < 1000; i++)
            map.Insert(i, i);

        map.Clear();

        Assert.Equal(0, map.Count);
        Assert.False(map.Contains(1));
    }

    [Fact]
    public void NoDuplicateKeys_Internally()
    {
        var map = new BlitzMap<int, int>();

        for (int i = 0; i < 5000; i++)
            map.InsertOrUpdate(i % 100, i);

        var seen = new HashSet<int>();

        foreach (ref readonly var e in map)
            Assert.True(seen.Add(e.Key));
    }

    [Fact]
    public void Count_Equals_EnumeratorCount()
    {
        var map = new BlitzMap<int, int>();

        for (int i = 0; i < 1000; i++)
            map.Insert(i, i);

        int count = 0;
        foreach (ref readonly var _ in map)
            count++;

        Assert.Equal(map.Count, count);
    }

    [Fact]
    public void Remove_FromLongChain_AllPositions()
    {
        var map = new BlitzMap<int, int>();

        for (int i = 0; i < 1000; i++)
            map.Insert(i, i);

        // remove head repeatedly
        for (int i = 0; i < 500; i++)
            Assert.True(map.Remove(i));

        // remove middle
        for (int i = 500; i < 750; i++)
            Assert.True(map.Remove(i));

        // remove tail
        for (int i = 750; i < 1000; i++)
            Assert.True(map.Remove(i));

        Assert.Equal(0, map.Count);
    }

    [Fact]
    public void Resize_WithLongCollisionChains()
    {
        var map = new BlitzMap<int, int>(2);

        for (int i = 0; i < 50_000; i++)
            map.Insert(i, i);

        for (int i = 0; i < 50_000; i++)
            Assert.Equal(i, map[i]);
    }


    [Fact]
    public void NoInfiniteLoops_UnderHeavyChurn()
    {
        var map = new BlitzMap<int, int>();

        for (int i = 0; i < 100_000; i++)
        {
            map.InsertOrUpdate(i % 1000, i);
            map.Remove((i + 1) % 1000);
        }

        // If we got here, no infinite loops
        Assert.True(true);
    }

    [Fact]
    public void Enumerator_DoesNotCrash_AfterMutation()
    {
        var map = new BlitzMap<int, int>();

        for (int i = 0; i < 1000; i++)
            map.Insert(i, i);

        foreach (ref readonly var e in map)
        {
            map.InsertOrUpdate(e.Key + 1000, e.Value);
            break;
        }

        Assert.True(true);
    }

    [Fact]
    public void ExtremeIntegerKeys()
    {
        var map = new BlitzMap<int, int>();

        map.Insert(int.MinValue, 1);
        map.Insert(int.MaxValue, 2);
        map.Insert(0, 3);

        Assert.Equal(1, map[int.MinValue]);
        Assert.Equal(2, map[int.MaxValue]);
        Assert.Equal(3, map[0]);
    }

    [Fact]
    public void Clear_ThenReuse_MultipleTimes()
    {
        var map = new BlitzMap<int, int>();

        for (int round = 0; round < 10; round++)
        {
            for (int i = 0; i < 10_000; i++)
                map.Insert(i, i);

            map.Clear();

            Assert.Equal(0, map.Count);
        }
    }

    [Fact]
    public void LongRunningFuzz()
    {
        var rnd = new Random();
        var map = new BlitzMap<int, int>();
        var dict = new Dictionary<int, int>();

        for (int i = 0; i < 100000; i++)
        {
            int k = rnd.Next(1000);
            int v = rnd.Next();

            switch (rnd.Next(4))
            {
                case 0:
                    map.InsertOrUpdate(k, v); dict[k] = v; break;
                case 1:
                    if (k == 58)
                    {

                    }
                    map.Remove(k); dict.Remove(k); break;
                case 2:
                    Assert.Equal(
                        dict.TryGetValue(k, out var dv),
                        map.Get(k, out var mv));
                    if (dict.ContainsKey(k)) Assert.Equal(dv, mv);
                    break;
                case 3:

                    Assert.Equal(dict.ContainsKey(k), map.Contains(k));
                    break;
            }
        }
    }

    [Fact]
    public void Remove_MiddleOfLongChain_DoesNotBreakLookup()
    {
        var map = new BlitzMap<int, int, AllToBucket3Hasher>(4);

        int[] keys =
        {
          100, 200, 300, 400,
          567, 600, 700, 800      };

        // Insert 8 colliding keys
        foreach (var k in keys)
        {
            map.InsertOrUpdate(k, k * 10);
        }

        // Remove the 4th element in the chain (400)
        Assert.True(map.Remove(100));

        // These must all still exist
        // Assert.True(map.Get(100, out _));
        Assert.True(map.Get(200, out _));
        Assert.True(map.Get(300, out _));

        Assert.True(map.Remove(600));
        Assert.True(map.Get(567, out var v));
        Assert.Equal(5670, v);

        Assert.True(map.Get(700, out _));
        Assert.True(map.Get(800, out _));

        Assert.False(map.Contains(100));

        Assert.True(map.InsertOrUpdate(100, 1));
        Assert.True(map.InsertOrUpdate(900, 1));
        Assert.True(map.Contains(400));
    }

    public struct AllToBucket3Hasher : IHasher<int>
    {
        public uint ComputeHash(int key)
        {
            // mask assumed 0b111 → bucket 3
            return 3u;
        }

        public bool Equals(int x, int y) => x == y;
    }


    #endregion

}
