// Copyright (c) 2024, Wiljan Ruizendaal. All rights reserved. <wruizendaal@gmail.com>
// Distributed under the MIT Software License, Version 1.0.

using Faster.Map.Contracts;
using Faster.Map.Hashing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#nullable enable

namespace Faster.Map.Core;

/// <summary>
/// A specialized implementation of <see cref="RobinhoodMap{TKey, TValue, THasher}"/> that
/// simplifies usage by defaulting the hasher to <see cref="DefaultHasher{TKey}"/>.
/// This avoids requiring three generic type parameters when the user doesn't need
/// a custom hashing function.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the map. Must be non-nullable.</typeparam>
/// <typeparam name="TValue">The type of the values in the map.</typeparam>
public sealed class RobinhoodMap<TKey, TValue> : RobinhoodMap<TKey, TValue, DefaultHasher.Generic<TKey>>
    where TKey : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RobinhoodMap{TKey, TValue}"/> class
    /// with a default initial length of 8 and a load factor of 0.5.
    /// </summary>
    public RobinhoodMap() : base(8, 0.5d) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RobinhoodMap{TKey, TValue}"/> class
    /// with the specified initial length and a default load factor of 0.5.
    /// </summary>
    /// <param name="length">The initial length of the hashmap. Will be rounded up to a power of two.</param>
    public RobinhoodMap(uint length) : base(length, 0.5d) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RobinhoodMap{TKey, TValue}"/> class
    /// with the specified initial length and load factor.
    /// </summary>
    /// <param name="length">The initial length of the hashmap. Will be rounded up to a power of two.</param>
    /// <param name="loadFactor">The load factor determines when the hashmap will resize (default is 0.5).</param>
    public RobinhoodMap(uint length, double loadFactor) : base(length, loadFactor) { }
}

/// <summary>
/// RobinhoodMap is a high-performance hashmap implementation that uses Robin Hood hashing with linear probing.
/// This map is designed for scenarios where you need efficient key-value storage with quick lookups, inserts, and deletions.
///
/// Key features:
/// - Open addressing with linear probing for collision resolution.
/// - Robin Hood hashing, which ensures that entries are placed in positions that minimize probe length disparity.
/// - Fibonacci hashing for better hash distribution.
/// - Automatic resizing when a specified load factor is exceeded.
/// - Upper limit on the probe sequence length (psl) which is Log2(size)
/// - Keeps track of the currentProbeCount which ensures early exit even if the maxProbeCount exceeds the currentProbeCount
///
/// Example usage:
/// <code>
/// var map = new RobinhoodMap<int, string>();
/// map.Emplace(1, "One");
/// map.Emplace(2, "Two");
/// map.Emplace(3, "Three");
///
/// if (map.Get(2, out var value))
/// {
///     Console.WriteLine($"Key 2 has value: {value}");
/// }
///
/// map.Update(3, "Three Updated");
/// map.Remove(1);
/// </code>
/// </summary>
/// <typeparam name="TKey">The type of keys in the map. Must be non-nullable.</typeparam>
/// <typeparam name="TValue">The type of values in the map.</typeparam>
public class RobinhoodMap<TKey, TValue, THasher> where THasher : struct, IHasher<TKey>
{
    #region Properties

    /// <summary>
    /// Gets or sets the number of elements stored in the map.
    /// Example:
    /// <code>
    /// var map = new RobinhoodMap<int, string>();
    /// int count = map.Count; // count should be 0 initially
    /// </code>
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Gets the size of the map.
    /// Example:
    /// <code>
    /// var map = new RobinhoodMap<int, string>();
    /// uint size = map.Size; // size will reflect the internal array size
    /// </code>
    /// </summary>
    public uint Size => (uint)_entries.Length;

    /// <summary>
    /// Returns all the entries as KeyValuePair objects.
    /// Example:
    /// <code>
    /// var map = new RobinhoodMap<int, string>();
    /// map.Emplace(1, "One");
    /// foreach (var entry in map.Entries)
    /// {
    ///     Console.WriteLine($"{entry.Key}: {entry.Value}");
    /// }
    /// </code>
    /// </summary>
    public IEnumerable<KeyValuePair<TKey, TValue>> Entries
    {
        get
        {
            for (int i = _meta.Length - 1; i >= 0; --i)
            {
                var meta = _meta[i];
                if (meta is not 0)
                {
                    yield return new KeyValuePair<TKey, TValue>(_entries[i].Key, _entries[i].Value);
                }
            }
        }
    }

    /// <summary>
    /// Returns all keys in the map.
    /// Example:
    /// <code>
    /// var map = new RobinhoodMap<int, string>();
    /// map.Emplace(1, "One");
    /// foreach (var key in map.Keys)
    /// {
    ///     Console.WriteLine(key);
    /// }
    /// </code>
    /// </summary>
    public IEnumerable<TKey> Keys
    {
        get
        {
            for (int i = _meta.Length - 1; i >= 0; --i)
            {
                var meta = _meta[i];
                if (meta > 0)
                {
                    yield return _entries[i].Key;
                }
            }
        }
    }

    /// <summary>
    /// Returns all values in the map.
    /// Example:
    /// <code>
    /// var map = new RobinhoodMap<int, string>();
    /// map.Emplace(1, "One");
    /// foreach (var value in map.Values)
    /// {
    ///     Console.WriteLine(value);
    /// }
    /// </code>
    /// </summary>
    public IEnumerable<TValue> Values
    {
        get
        {
            for (int i = _meta.Length - 1; i >= 0; --i)
            {
                var meta = _meta[i];
                if (meta is not 0)
                {
                    yield return _entries[i].Value;
                }
            }
        }
    }

    #endregion

    #region Fields

    private byte[] _meta;
    private THasher _hasher;
    private Entry[] _entries;
    private uint _length;
    private readonly double _loadFactor;
    private byte _maxProbeSequenceLength;
    private int _maxLookupsBeforeResize;
    private uint _mask;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RobinhoodMap{TKey,TValue}"/> class with default parameters.  
    /// </summary>
    public RobinhoodMap() : this(8, 0.5d, EqualityComparer<TKey>.Default) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RobinhoodMap{TKey,TValue}"/> class with the specified length.   
    /// </summary>
    /// <param name="length">The length of the hashmap. Will always take the closest power of two.</param>
    public RobinhoodMap(uint length) : this(length, 0.5d, EqualityComparer<TKey>.Default) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RobinhoodMap{TKey,TValue}"/> class with the specified length and load factor.
    /// </summary>
    /// <param name="length">The length of the hashmap. Will always take the closest power of two.</param>
    /// <param name="loadFactor">The load factor determines when the hashmap will resize (default is 0.5d).</param>
    public RobinhoodMap(uint length, double loadFactor) : this(length, loadFactor, EqualityComparer<TKey>.Default) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RobinhoodMap{TKey,TValue}"/> class with the specified parameters.   
    /// </summary>
    /// <param name="length">The length of the hashmap. Will always take the closest power of two.</param>
    /// <param name="loadFactor">The load factor determines when the hashmap will resize.</param>
    /// <param name="keyComparer">Used to compare keys to resolve hash collisions.</param>
    public RobinhoodMap(uint length, double loadFactor, IEqualityComparer<TKey> keyComparer)
    {
        _length = BitOperations.RoundUpToPowerOf2(length);
        _loadFactor = loadFactor;

        if (length < 4)
        {
            _length = 4;
        }

        _maxProbeSequenceLength = (byte)BitOperations.Log2(_length);
        _maxLookupsBeforeResize = (int)((_length + _maxProbeSequenceLength) * loadFactor);      
     
        var size = (int)_length + _maxProbeSequenceLength;
        _mask = _length - 1;
        _entries = GC.AllocateUninitializedArray<Entry>(size);
        _meta = GC.AllocateArray<byte>(size);
        _hasher = default;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Inserts the specified key-value pair into the map.
    /// Example:
    /// <code>
    /// var map = new RobinhoodMap<int, string>();
    /// map.Emplace(1, "One");
    /// </code>
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>True if the insertion was successful, otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Emplace(TKey key, TValue value)
    {
        if (Count > _maxLookupsBeforeResize)
        {
            Resize();
        }

        var index = _hasher.ComputeHash(key) & _mask;

        byte distance = 1;
        var entry = new Entry(key, value);

        do
        {
            ref var meta = ref Find(_meta, index);

            if (meta == 0)
            {
                meta = distance;
                Find(_entries, index) = entry;
                ++Count;
                return true;
            }

            if (distance > meta)
            {
                Swap(ref distance, ref meta);
                Swap(ref entry, ref Find(_entries, index));
                ++index;
                continue;
            }

            if (_hasher.Equals(key, Find(_entries, index).Key))
            {
                return false;
            }

            ++distance;
            ++index;
        } while (true);
    }

    /// <summary>
    /// Retrieves the value associated with the specified key.
    /// Example:
    /// <code>
    /// var map = new RobinhoodMap<int, string>();
    /// map.Emplace(1, "One");
    /// if (map.Get(1, out var value))
    /// {
    ///     Console.WriteLine($"Key 1 has value: {value}");
    /// }
    /// </code>
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value if the key is found.</param>
    /// <returns>True if the key was found, otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Get(TKey key, out TValue value)
    {
        var index = _hasher.ComputeHash(key) & _mask;
        var maxDistance = index + _maxProbeSequenceLength;

        do
        {
            ref var entry = ref Find(_entries, index);

            if (_hasher.Equals(entry.Key, key))
            {
                value = entry.Value;
                return true;
            }

        } while (++index < maxDistance);

        value = default;
        return false;
    }

    /// <summary>
    /// Gets the value for the specified key, or if the key is not present, adds an entry and returns the value by reference.
    /// Example:
    /// <code>
    /// var map = new RobinhoodMap<int, int>();
    /// ref var value = ref map.GetOrUpdate(1);
    /// value++;
    /// Console.WriteLine(value); // Should print 1
    /// </code>
    /// </summary>
    /// <param name="key">The key to look for.</param>
    /// <returns>Reference to the existing value or newly added value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TValue GetOrUpdate(TKey key)
    {
        if (Count >= _maxLookupsBeforeResize)
        {
            Resize();
        }

        var index = _hasher.ComputeHash(key) & _mask;
        var entry = new Entry(key, default);
        byte distance = 1;

        do
        {
            ref var meta = ref Find(_meta, index);

            if (meta == 0)
            {
                meta = distance;
                ref var x = ref Find(_entries, index);
                x = entry;

                ++Count;
                return ref x.Value;
            }

            if (distance > meta)
            {
                Swap(ref distance, ref meta);
                Swap(ref entry, ref Find(_entries, index));
                goto next;
            }

            if (_hasher.Equals(key, Find(_entries, index).Key))
            {
                return ref Find(_entries, index).Value;
            }

            next:

            ++distance;
            ++index;
        } while (true);
    }

    /// <summary>
    /// Updates the value associated with a specific key.
    /// Example:
    /// <code>
    /// var map = new RobinhoodMap<int, string>();
    /// map.Emplace(1, "One");
    /// map.Update(1, "One Updated");
    /// </code>
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The new value.</param>
    /// <returns>True if the update was successful, otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Update(TKey key, TValue value)
    {
        var index = _hasher.ComputeHash(key) & _mask;
        var maxDistance = index + _maxProbeSequenceLength;

        do
        {
            ref var entry = ref Find(_entries, index);

            if (_hasher.Equals(entry.Key, key))
            {
                entry.Value = value;
                return true;
            }

        } while (++index < maxDistance);

        return false;
    }

    /// <summary>
    /// Removes the entry with the specified key from the map.
    /// Example:
    /// <code>
    /// var map = new RobinhoodMap<int, string>();
    /// map.Emplace(1, "One");
    /// map.Remove(1);
    /// </code>
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns>True if the entry was removed, otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey key)
    {
        var index = _hasher.ComputeHash(key) & _mask;
        var maxDistance = index + _maxProbeSequenceLength;

        do
        {
            if (_hasher.Equals(key, Find(_entries, index).Key))
            {
                uint nextIndex = index + 1;
                var nextMeta = Find(_meta, nextIndex);

                while (nextMeta > 1)
                {
                    nextMeta--;

                    Find(_meta, index) = nextMeta;
                    Find(_entries, index) = Find(_entries, nextIndex);

                    index++;
                    nextIndex++;

                    nextMeta = Find(_meta, nextIndex);
                }

                Find(_meta, index) = default;
                Find(_entries, index) = default;

                --Count;
                return true;
            }

            ++index;

        } while (index < maxDistance);

        return false;
    }

    /// <summary>
    /// Determines whether the specified key exists in the map.
    /// Example:
    /// <code>
    /// var map = new RobinhoodMap<int, string>();
    /// map.Emplace(1, "One");
    /// bool exists = map.Contains(1); // true
    /// </code>
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>True if the key exists, otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(TKey key)
    {
        var index = _hasher.ComputeHash(key) & _mask;
        var maxDistance = index + _maxProbeSequenceLength;

        do
        {
            var entry = Find(_entries, index);
            if (_hasher.Equals(entry.Key, key))
            {
                return true;
            }

        } while (++index < maxDistance);

        return false;
    }

    /// <summary>
    /// Copies all entries from another RobinhoodMap into this one.
    /// Example:
    /// <code>
    /// var sourceMap = new RobinhoodMap<int, string>();
    /// sourceMap.Emplace(1, "One");
    /// var destMap = new RobinhoodMap<int, string>();
    /// destMap.Copy(sourceMap);
    /// </code>
    /// </summary>
    /// <param name="denseMap">The source RobinhoodMap to copy from.</param>
    public void Copy(RobinhoodMap<TKey, TValue> denseMap)
    {
        for (var i = 0; i < denseMap._entries.Length; ++i)
        {
            var meta = denseMap._meta[i];
            if (meta is 0)
            {
                continue;
            }

            Emplace(denseMap._entries[i].Key, denseMap._entries[i].Value);
        }
    }

    /// <summary>
    /// Clears all entries from the map.
    /// Example:
    /// <code>
    /// var map = new RobinhoodMap<int, string>();
    /// map.Clear();
    /// </code>
    /// </summary>
    public void Clear()
    {
        Array.Clear(_entries);
        Array.Clear(_meta);
        Count = 0;
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// Example:
    /// <code>
    /// var map = new RobinhoodMap<int, string>();
    /// map.Emplace(1, "One");
    /// string value = map[1]; // "One"
    /// map[1] = "One Updated";
    /// </code>
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The value associated with the key.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the key is not found in the map.</exception>
    public TValue this[TKey key]
    {
        get
        {
            if (Get(key, out var result))
            {
                return result;
            }

            throw new KeyNotFoundException($"Unable to find Entry - {key.GetType().FullName} key - {key.GetHashCode()}");
        }
        set
        {
            if (!Update(key, value))
            {
                throw new KeyNotFoundException($"Unable to find Entry - {key.GetType().FullName} key - {key.GetHashCode()}");
            }
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Finds the entry in the array at the specified index.
    /// </summary>
    /// <param name="array">The array to search.</param>
    /// <param name="index">The index to look up.</param>
    /// <returns>A reference to the found entry.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref Entry Find(Entry[] array, uint index)
    {
        return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
    }

    /// <summary>
    /// Finds the metadata byte in the array at the specified index.
    /// </summary>
    /// <param name="array">The array to search.</param>
    /// <param name="index">The index to look up.</param>
    /// <returns>A reference to the found metadata byte.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref byte Find(byte[] array, uint index)
    {
        return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
    }

    /// <summary>
    /// Inserts an entry into the map internally without triggering a resize.  
    /// </summary>
    /// <param name="entry">The entry to insert.</param>
    private void EmplaceInternal(ref Entry entry)
    {
        var index = _hasher.ComputeHash(entry.Key) & _mask;
        byte distance = 1;

        do
        {
            ref var meta = ref Find(_meta, index);

            if (meta == 0)
            {
                meta = distance;
                Find(_entries, index) = entry;

                return;
            }

            if (distance > meta)
            {
                Swap(ref distance, ref meta);
                Swap(ref entry, ref Find(_entries, index));
            }

            ++distance;
            ++index;
        } while (true);
    }

    /// <summary>
    /// Swaps the values of two variables.
    /// Example:
    /// <code>
    /// Swap(ref x, ref y);
    /// </code>
    /// </summary>
    /// <typeparam name="T">The type of the variables to swap.</typeparam>
    /// <param name="x">The first variable.</param>
    /// <param name="y">The second variable.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Swap<T>(ref T x, ref T y) => (x, y) = (y, x);

    /// <summary>
    /// Resizes the map by doubling its size and rehashing all entries.     
    /// </summary>
    private void Resize()
    {
        _length <<= 1;
      
        _mask = _length - 1;
        _maxProbeSequenceLength = (byte)BitOperations.Log2(_length);
        _maxLookupsBeforeResize = (int)((_length + _maxProbeSequenceLength) * _loadFactor);

        var size = Unsafe.As<uint, int>(ref _length) + _maxProbeSequenceLength;

        var oldEntries = _entries;
        var oldMeta = _meta;

        _entries = GC.AllocateUninitializedArray<Entry>(size);
        _meta = GC.AllocateArray<byte>(size);

        for (uint i = 0; i < oldMeta.Length; ++i)
        {
            if (oldMeta[i] == 0)
            {
                continue;
            }

            EmplaceInternal(ref Find(oldEntries, i));
        }
    }

    [DebuggerDisplay("{Key} {Value}")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct Entry
    {
        public TKey Key;
        public TValue Value;

        public Entry(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }

    #endregion
}
