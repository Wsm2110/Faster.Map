// Copyright (c) 2026, Wiljan Ruizendaal. All rights reserved. <wruizendaal@gmail.com> 
// Distributed under the MIT Software License, Version 1.0.

using Faster.Map.Contracts;
using Faster.Map.Hashing;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Faster.Map.Core;

/// <summary>
/// A specialized implementation of <see cref="DenseMap{TKey, TValue, THasher}"/> that
/// defaults to using the <see cref="GoldenRatioHasher{TKey}"/> for efficient hashing.
/// This avoids requiring three generic parameters when a custom hasher is not needed.
/// </summary>
/// <typeparam name="TKey">The type of the keys stored in the map.</typeparam>
/// <typeparam name="TValue">The type of the values stored in the map.</typeparam>
/// <remarks>
/// The default hasher, <see cref="GoldenRatioHasher{TKey}"/>, is chosen for its strong 
/// distribution properties, ensuring minimal collisions and improved lookup performance.
/// </remarks>
public class DenseMap<TKey, TValue> : DenseMap<TKey, TValue, DefaultHasher.Generic<TKey>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DenseMap{TKey, TValue}"/> class 
    /// with the specified initial capacity and a default load factor of 0.875.
    /// </summary>
    /// <param name="length">The initial capacity (number of buckets) in the map.</param>
    /// <remarks>
    /// The default load factor (0.875) is chosen to balance memory usage and performance.
    /// Higher load factors reduce memory overhead while still maintaining efficient lookups.
    /// </remarks>
    public DenseMap(uint length) : base(length, 0.875) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DenseMap{TKey, TValue}"/> class 
    /// with the specified initial capacity and load factor.
    /// </summary>
    /// <param name="length">The initial capacity (number of buckets) in the map.</param>
    /// <param name="loadFactor">
    /// The maximum allowed load factor before resizing occurs. A higher load factor
    /// reduces memory usage at the cost of increased collision probability.
    /// </param>
    /// <remarks>
    /// This constructor allows fine-tuned control over performance trade-offs:
    /// - **Higher load factors (e.g., 0.9 - 0.95):** More memory-efficient but may cause more collisions.
    /// - **Lower load factors (e.g., 0.5 - 0.7):** Faster lookups but higher memory usage.
    /// </remarks>
    public DenseMap(uint length, double loadFactor) : base(length, loadFactor) { }

    /// <summary>
    /// 
    /// </summary>
    public DenseMap() : base(16, 0.875) { }
}
/// <summary>
/// DenseMap is a high-performance hashmap implementation that uses open-addressing with quadratic probing and SIMD (Single Instruction, Multiple Data) for parallel searches.
/// This map is designed for scenarios requiring efficient key-value storage with fast lookups, inserts, and deletions.
/// 
/// Key features:
/// - Open addressing with quadratic probing for collision resolution.
/// - SIMD-based parallel searches for performance optimization.
/// - High load factor (default is 0.875) while maintaining speed.
/// - Tombstones to avoid backshifts during deletions.
///
/// Example usage:
/// <code>
/// var map = new DenseMap<int, string>();
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
/// <typeparam name="THasher">
/// A struct implementing <see cref="Hasher.IHasher{TKey}"/> to provide an optimized hashing function.
/// Using a struct-based hasher avoids virtual method calls and allows aggressive inlining.</typeparam>
public class DenseMap<TKey, TValue, THasher> where THasher : struct, IHasher<TKey>
{
    #region Properties

    /// <summary>
    /// Gets or sets how many elements are stored in the map.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>();
    /// int count = map.Count; // count should be 0 initially
    /// </code>
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Gets the size of the map.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>();
    /// uint size = map.Size; // size will reflect the internal array size
    /// </code>
    /// </summary>
    public uint Size => (uint)_entries.Length;

    /// <summary>
    /// Returns all the entries as KeyValuePair objects.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>();
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
            for (int i = 0; i < _length; ++i)
            {
                if (_controlBytes[i] >= 0)
                {
                    var entry = _entries[i];
                    yield return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
                }
            }
        }
    }

    /// <summary>
    /// Returns all keys in the map.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>();
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
            for (int i = 0; i < _length; ++i)
            {
                if (_controlBytes[i] >= 0)
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
    /// var map = new DenseMap<int, string>();
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
            for (int i = 0; i < _length; ++i)
            {
                if (_controlBytes[i] >= 0)
                {
                    yield return _entries[i].Value;
                }
            }
        }
    }

    private uint _groupWidth = 16;

    #endregion

    #region Fields

    private const sbyte _emptyBucket = -127;
    private const sbyte _tombstone = -126;
    private static readonly Vector128<sbyte> _emptyBucketVector = Vector128.Create(_emptyBucket);
    private static readonly Vector128<sbyte> _tombstoneVector = Vector128.Create(_tombstone);
    private sbyte[] _controlBytes;
    private double _maxTombstoneBeforeRehash;
    private uint _tombstoneCounter;
    private Entry[] _entries;
    private uint _length;
    private double _maxLookupsBeforeResize;
    private uint _mask;
    private readonly double _loadFactor;
    private readonly THasher _hasher;
    private bool _suppressRebuild;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class with the specified length and default load factor.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>(32);
    /// </code>
    /// </summary>
    /// <param name="length">The length of the hashmap. Will always take the closest power of two.</param>
    public DenseMap(uint length) : this(length, 0.875) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class with the specified length and default load factor.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>(32);
    /// </code>
    /// </summary>
    public DenseMap() : this(16, 0.875) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class with the specified parameters.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>(32, 0.8, EqualityComparer<int>.Default);
    /// </code>
    /// </summary>
    /// <param name="length">The length of the hashmap. Will always take the closest power of two.</param>
    /// <param name="loadFactor">The load factor determines when the hashmap will resize.</param>
    /// <param name="keyComparer">Used to compare keys to resolve hash collisions.</param>
    public DenseMap(uint length, double loadFactor)
    {
        if (!Vector128.IsHardwareAccelerated)
        {
            throw new NotSupportedException("Your hardware does not support acceleration for 128-bit vectors.");
        }

        _length = length;
        _loadFactor = loadFactor;
        _hasher = default;

        if (loadFactor > 0.875)
        {
            _loadFactor = 0.875;
        }

        if (_length < 16)
        {
            _length = 16;
        }
        else if (BitOperations.IsPow2(_length))
        {
            _length = length;
        }
        else
        {
            _length = BitOperations.RoundUpToPowerOf2(_length);
        }

        _maxLookupsBeforeResize = (uint)(_length * _loadFactor);

        _controlBytes = GC.AllocateArray<sbyte>((int)_length + 16);
        _entries = GC.AllocateArray<Entry>((int)_length + 16);

        _controlBytes.AsSpan().Fill(_emptyBucket);

        _maxTombstoneBeforeRehash = _length * 0.125;
        _mask = _length - 1;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Inserts a new key-value pair into the map.
    /// If the key already exists, behavior is undefined (may overwrite or cause corruption).
    /// Use this only when you're certain the key doesn't exist for maximum performance.
    /// </summary>
    /// <param name="key">The key to insert.</param>
    /// <param name="value">The value to insert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Insert(TKey key, TValue value)
    {
        // Use both Count and Tombstones to determine if a Resize/Rebuild is needed
        // This prevents the P99 latency spikes caused by long probe chains
        if (Count + _tombstoneCounter >= _maxLookupsBeforeResize)
        {
            Resize();
        }

        var hashcode = _hasher.ComputeHash(key);
        var h2 = H2(hashcode);

        uint index = (uint)hashcode & _mask;
        uint jumpDistance = 0;
        uint firstAvailableSlot = uint.MaxValue;

        ref sbyte ctrl = ref MemoryMarshal.GetArrayDataReference(_controlBytes);
        ref Entry entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        while (true)
        {
            var source = Vector128.LoadUnsafe(ref ctrl, index);

            // 1. Identify Empty slots (terminates the chain)
            var emptyMask = Vector128.Equals(source, _emptyBucketVector).ExtractMostSignificantBits();

            // 2. Identify Tombstones (potential reuse points)
            // We only care about the very first tombstone we see in the entire probe sequence
            if (firstAvailableSlot == uint.MaxValue)
            {
                var tombstoneMask = Vector128.Equals(source, _tombstoneVector).ExtractMostSignificantBits();
                if (tombstoneMask != 0)
                {
                    uint bit = (uint)BitOperations.TrailingZeroCount(tombstoneMask);
                    firstAvailableSlot = (index + bit) & _mask;
                }
            }

            // 3. If we hit an empty bucket, the search for the end of the chain is over
            if (emptyMask != 0)
            {
                uint slot;
                if (firstAvailableSlot != uint.MaxValue)
                {
                    // Reuse the tombstone we found earlier to keep the map dense
                    slot = firstAvailableSlot;
                    _tombstoneCounter--;
                }
                else
                {
                    // No tombstones found, use the first empty bucket
                    uint bit = (uint)BitOperations.TrailingZeroCount(emptyMask);
                    slot = (index + bit) & _mask;
                }

                // Write the control byte (and mirror it for SIMD safety)
                SetCtrl(ref ctrl, slot, h2);

                // Write the data
                ref var entry = ref Unsafe.Add(ref entryBase, slot);
                entry.Key = key;
                entry.Value = value;

                ++Count;
                return;
            }

            // Triangular Probing: index + 16, then + 32, then + 48...
            index = (index + (jumpDistance += _groupWidth)) & _mask;
        }
    }
    /// <summary>
    /// Inserts or updates a key-value pair in the map.
    /// </summary>  
    ///
    /// If the key is not already present, the specified value is inserted, and <c>Default</c> is returned.
    /// If the key is present, the existing value is updated, and the old value is returned.
    /// 
    ///  This method always succeeds, performing an update for existing keys or an insertion for new keys.
    ///
    /// </remarks>
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>();
    /// 
    /// var val = map.Emplace(1, "One"); returns the default
    /// var valTwo = map.Emplace(1, "Two") // returns oldValue "One"
    ///  
    /// </code>
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>Returns the old value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InsertOrUpdate(TKey key, TValue value)
    {
        if (Count + _tombstoneCounter >= _maxLookupsBeforeResize) Resize();

        var hash = _hasher.ComputeHash(key);
        var h2 = H2(hash);
        var target = Vector128.Create(h2);
        uint index = hash & _mask;
        uint jump = 0;
        uint firstAvailableSlot = uint.MaxValue;

        ref sbyte ctrl = ref MemoryMarshal.GetArrayDataReference(_controlBytes);

        while (true)
        {
            var source = Vector128.LoadUnsafe(ref ctrl, index);

            // 1. Check for Matches
            var matchMask = Vector128.Equals(source, target).ExtractMostSignificantBits();
            while (matchMask != 0)
            {
                uint slot = (index + (uint)BitOperations.TrailingZeroCount(matchMask)) & _mask;
                if (_hasher.Equals(_entries[slot].Key, key))
                {
                    _entries[slot].Value = value;
                    return;
                }
                matchMask &= matchMask - 1;
            }

            // 2. Track first available slot (Tombstone or Empty)
            if (firstAvailableSlot == uint.MaxValue)
            {
                // We prioritize tombstones to keep the map "dense"
                var tombstoneMask = Vector128.Equals(source, _tombstoneVector).ExtractMostSignificantBits();
                var emptyMask = Vector128.Equals(source, _emptyBucketVector).ExtractMostSignificantBits();
                var combinedMask = tombstoneMask | emptyMask;

                if (combinedMask != 0)
                {
                    firstAvailableSlot = (index + (uint)BitOperations.TrailingZeroCount(combinedMask)) & _mask;
                }
            }

            // 3. Terminate on Empty Bucket
            if (Vector128.Equals(source, _emptyBucketVector).ExtractMostSignificantBits() != 0)
            {
                // Finalize insertion at the first slot we found during the probe
                SetCtrl(ref ctrl, firstAvailableSlot, h2);

                // If the slot we used was a tombstone, decrement the counter
                if (Unsafe.Add(ref ctrl, firstAvailableSlot) == _tombstone) _tombstoneCounter--;

                ref var entry = ref _entries[firstAvailableSlot];
                entry.Key = key;
                entry.Value = value;
                Count++;
                return;
            }

            index = (index + (jump += _groupWidth)) & _mask;
        }
    }

    /// <summary>
    /// Tries to find the key in the map and returns the associated value.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>();
    /// map.Emplace(1, "One");
    /// if (map.Get(1, out var value))
    /// {
    ///     Console.WriteLine(value); // Output: "One"
    /// }
    /// </code>
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>Returns false if the key is not found.</returns>       
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Get(TKey key, out TValue value)
    {
        var hashcode = _hasher.ComputeHash(key);
        var h2 = H2(hashcode);
        var target = Vector128.Create(h2);

        uint index = hashcode & _mask;
        uint jumpDistance = 0;

        ref sbyte ctrl = ref MemoryMarshal.GetArrayDataReference(_controlBytes);
        ref Entry entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        while (true)
        {
            var source = Vector128.LoadUnsafe(ref ctrl, index);
            var matchMask = Vector128.Equals(source, target).ExtractMostSignificantBits();

            while (matchMask != 0)
            {
                uint bit = (uint)BitOperations.TrailingZeroCount(matchMask);
                uint slot = (index + bit) & _mask;

                ref var entry = ref Unsafe.Add(ref entryBase, slot);
                if (_hasher.Equals(entry.Key, key))
                {
                    value = entry.Value;
                    return true;
                }

                matchMask &= matchMask - 1;
            }

            if (Vector128.Equals(source, _emptyBucketVector).ExtractMostSignificantBits() != 0)
            {
                value = default;
                return false;
            }

            index = (index + (jumpDistance += _groupWidth)) & _mask;
        }
    }

    /// <summary>
    /// Gets the value for the specified key, or, if the key is not present, adds an entry and returns the value by reference.
    /// This allows you to add or update a value in a single lookup operation.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, int>();
    /// ref var value = ref map.GetValueRefOrAddDefault(1);
    /// value++;
    /// Console.WriteLine(map.Get(1)); // Output: 1
    /// </code>
    /// </summary>
    /// <param name="key">The key to look for.</param>
    /// <returns>Reference to the new or existing value.</returns>    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TValue GetValueRefOrAddDefault(TKey key)
    {
        if (Count >= _maxLookupsBeforeResize)
            Resize();

        var hashcode = _hasher.ComputeHash(key);
        var h2 = H2(hashcode);
        var target = Vector128.Create(h2);

        uint index = hashcode & _mask;
        uint jumpDistance = 0;

        ref sbyte ctrl = ref MemoryMarshal.GetArrayDataReference(_controlBytes);
        ref Entry entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        while (true)
        {
            var source = Vector128.LoadUnsafe(ref ctrl, index);
            var matchMask = Vector128.Equals(source, target).ExtractMostSignificantBits();

            while (matchMask != 0)
            {
                uint bit = (uint)BitOperations.TrailingZeroCount(matchMask);
                uint slot = (index + bit) & _mask;

                ref var entry = ref Unsafe.Add(ref entryBase, slot);
                if (_hasher.Equals(entry.Key, key))
                    return ref entry.Value;

                matchMask &= matchMask - 1;
            }

            var emptyMask = Vector128.Equals(source, _emptyBucketVector).ExtractMostSignificantBits();
            if (emptyMask != 0)
            {
                uint bit = (uint)BitOperations.TrailingZeroCount(emptyMask);
                uint slot = (index + bit) & _mask;

                SetCtrl(ref ctrl, slot, h2);

                ref var entry = ref Unsafe.Add(ref entryBase, slot);
                entry.Key = key;
                Count++;
                return ref entry.Value;
            }

            index = (index + (jumpDistance += _groupWidth)) & _mask;
        }
    }


    /// <summary>
    /// Tries to find the key in the map and updates the associated value.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>();
    /// map.Emplace(1, "One");
    /// bool updated = map.Update(1, "One Updated");
    /// </code>
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The new value.</param>
    /// <returns>Returns true if the update succeeded, otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Update(TKey key, TValue value)
    {
        var hashcode = _hasher.ComputeHash(key);
        var h2 = H2(hashcode);
        var target = Vector128.Create(h2);

        uint index = hashcode & _mask;
        uint jumpDistance = 0;

        ref sbyte ctrl = ref MemoryMarshal.GetArrayDataReference(_controlBytes);
        ref Entry entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        while (true)
        {
            var source = Vector128.LoadUnsafe(ref ctrl, index);
            var matchMask = Vector128.Equals(source, target).ExtractMostSignificantBits();

            while (matchMask != 0)
            {
                uint bit = (uint)BitOperations.TrailingZeroCount(matchMask);
                uint slot = (index + bit) & _mask;

                ref var entry = ref Unsafe.Add(ref entryBase, slot);
                if (_hasher.Equals(entry.Key, key))
                {
                    entry.Value = value;
                    return true;
                }

                matchMask &= matchMask - 1;
            }

            if (Vector128.Equals(source, _emptyBucketVector).ExtractMostSignificantBits() != 0)
                return false;

            index = (index + (jumpDistance += _groupWidth)) & _mask;
        }
    }

    /// <summary>
    /// Removes a key and value from the map.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>();
    /// map.Emplace(1, "One");
    /// bool removed = map.Remove(1);
    /// </code>
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>Returns true if the removal succeeded, otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey key)
    {
        if (!_suppressRebuild && _tombstoneCounter >= _maxTombstoneBeforeRehash)
        {
            Rebuild();
        }

        var hashcode = _hasher.ComputeHash(key);
        var h2 = H2(hashcode);
        var target = Vector128.Create(h2);

        uint index = hashcode & _mask;
        uint jumpDistance = 0;

        ref sbyte ctrl = ref MemoryMarshal.GetArrayDataReference(_controlBytes);
        ref Entry entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        while (true)
        {
            var source = Vector128.LoadUnsafe(ref ctrl, index);

            var matchMask = Vector128.Equals(source, target).ExtractMostSignificantBits();

            while (matchMask != 0)
            {
                uint bit = (uint)BitOperations.TrailingZeroCount(matchMask);
                uint slot = (index + bit) & _mask;

                ref var entry = ref Unsafe.Add(ref entryBase, slot);
                if (_hasher.Equals(entry.Key, key))
                {
                    SetCtrl(ref ctrl, slot, _tombstone);
                    _tombstoneCounter++;

                    entry = default;
                    --Count;
                    return true;
                }

                matchMask &= matchMask - 1;
            }

            if (Vector128.Equals(source, _emptyBucketVector).ExtractMostSignificantBits() != 0)
            {
                return false;
            }

            index = (index + (jumpDistance += _groupWidth)) & _mask;
        }
    }

    /// <summary>
    /// Rebuilds the internal data structures to optimize storage and access, typically after a resize or rehash
    /// operation.
    /// </summary>
    /// <remarks>This method redistributes existing entries into new arrays, clearing any tombstones and
    /// ensuring that the hash table maintains optimal performance. It should be called when the underlying storage
    /// needs to be refreshed, such as after significant insertions or deletions. This operation resets the tombstone
    /// counter and may improve lookup and insertion efficiency.</remarks>
    private void Rebuild()
    {
        var oldEntries = _entries;
        var oldControlBytes = _controlBytes;
        var length = _length;

        var newEntries = GC.AllocateArray<Entry>((int)length);
        var newControlBytes = GC.AllocateArray<sbyte>((int)(length + 16));

        newControlBytes.AsSpan().Fill(_emptyBucket);

        ref sbyte newCtrl = ref MemoryMarshal.GetArrayDataReference(newControlBytes);
        ref Entry newEnt = ref MemoryMarshal.GetArrayDataReference(newEntries);

        ref sbyte oldCtrl = ref MemoryMarshal.GetArrayDataReference(oldControlBytes);
        ref Entry oldEnt = ref MemoryMarshal.GetArrayDataReference(oldEntries);

        for (uint i = 0; i < length; ++i)
        {
            var ctrl = Unsafe.Add(ref oldCtrl, i);
            if (ctrl < 0)
                continue;

            var entry = Unsafe.Add(ref oldEnt, i);

            var hashcode = _hasher.ComputeHash(entry.Key);
            uint index = hashcode & _mask;
            uint jumpDistance = 0;

            while (true)
            {
                var source = Vector128.LoadUnsafe(ref newCtrl, index);
                var emptyMask = Vector128.Equals(source, _emptyBucketVector).ExtractMostSignificantBits();

                if (emptyMask != 0)
                {
                    uint bit = (uint)BitOperations.TrailingZeroCount(emptyMask);
                    uint slot = (index + bit) & _mask;

                    Unsafe.Add(ref newEnt, slot) = entry;
                    SetCtrl(ref newCtrl, slot, ctrl);
                    break;
                }

                index = (index + (jumpDistance += _groupWidth)) & _mask;
            }
        }

        _controlBytes = newControlBytes;
        _entries = newEntries;
        _tombstoneCounter = 0;
    }

    /// <summary>
    /// Determines if the hashmap contains the specified key.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>();
    /// map.Emplace(1, "One");
    /// bool contains = map.Contains(1); // true
    /// </code>
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>Returns true if the key is found, otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(TKey key)
    {
        // Compute the hash code for the given key and cast it to an unsigned integer for bitwise operations.
        var hashcode = _hasher.ComputeHash(key);
        // Apply a secondary hash function to further spread the bits of the hashcode.
        var h2 = H2(hashcode);
        // Create a 128-bit vector from the transformed hash code to use as the target for comparison.
        var target = Vector128.Create(h2);
        // This operation ensures that `index` is in the range [0, capacity - 1] by using only the lower bits of `hashcode`,
        // which helps in efficient and quick indexing.
        var index = hashcode & _mask;
        // Initialize `jumpDistance` to control the distance between probes, starting at zero.
        uint jumpDistance = 0;

        ref sbyte start = ref MemoryMarshal.GetArrayDataReference(_controlBytes);
        ref Entry entryStart = ref MemoryMarshal.GetArrayDataReference(_entries);

        // Begin probing the hash map until the key is found or confirmed absent.
        while (true)
        {
            // Load a vector from the control bytes starting at the computed index.
            // Control bytes hold metadata about the entries in the map.
            var source = Vector128.LoadUnsafe(ref start, index);
            // Compare `source` with `target`, and `ExtractMostSignificantBits` returns a bitmask
            // where each set bit indicates a position in `source` that matches `target`.
            var mask = Vector128.Equals(source, target).ExtractMostSignificantBits();
            // Process each match indicated by the bits set in `mask`.
            while (mask != 0)
            {
                // Get the position of the first set bit in `mask`, indicating a potential key match.
                var bitPos = BitOperations.TrailingZeroCount(mask);

                // Normalize SIMD position to a logical slot using the power-of-two mask.
                uint slot = (index + (uint)bitPos) & _mask;

                // Check if the entry at this position has a key that matches the specified key.
                // Use `_hasher` to ensure accurate key comparison.
                if (_hasher.Equals(Unsafe.Add(ref entryStart, slot).Key, key))
                {
                    // If a match is found, return `true` to indicate the key exists in the map.
                    return true;
                }

                // Clear the lowest set bit in `mask` to continue checking any remaining matches in this vector.
                mask = ResetLowestSetBit(mask);
            }

            // Check if `source` contains any empty buckets, which indicates the end of the probe chain.
            // If an empty bucket is found, it confirms the key is not present in the map.
            if (Vector128.Equals(_emptyBucketVector, source).ExtractMostSignificantBits() != 0)
            {
                // Return `false` since the key was not found in the map.
                return false;
            }

            // Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
            // So first we jump by 1 group (meaning we just continue our linear scan), then 2 groups (skipping over 1 group), then 3 groups (skipping over 2 groups), and so on.
            // Interestingly, this pattern perfectly lines up with our power-of-two size such that we will visit every single bucket exactly once without any repeats(searching is therefore guaranteed to terminate as we always have at least one EMPTY bucket).
            // Note: that our non-linear probing strategy makes us fairly robust against weird degenerate collision chains that can make us accidentally quadratic(Hash DoS).
            // Note: that we expect to almost never actually probe, since that’s WIDTH(16) non-EMPTY buckets we need to fail to find our key in.

            index = (index + (jumpDistance += _groupWidth)) & _mask;
        }
    }

    /// <summary>
    /// Copies entries from one map to another.
    /// Example:
    /// <code>
    /// var sourceMap = new DenseMap<int, string>();
    /// sourceMap.Emplace(1, "One");
    /// var destMap = new DenseMap<int, string>();
    /// destMap.Copy(sourceMap);
    /// </code>
    /// </summary>
    /// <param name="denseMap">The map to copy from.</param>
    /// <summary>
    /// Copies all entries from another DenseMap that uses the same hasher type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Copy(DenseMap<TKey, TValue, THasher> other)
    {
        foreach (var kv in other.Entries)
        {
            InsertOrUpdate(kv.Key, kv.Value);
        }
    }

    /// <summary>
    /// Removes all entries from the map and sets the count to 0.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>();
    /// map.Clear();
    /// </code>
    /// </summary>
    public void Clear()
    {
        Array.Clear(_entries);
        _controlBytes.AsSpan().Fill(_emptyBucket);
        Count = 0;
        _tombstoneCounter = 0;
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>();
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

            throw new KeyNotFoundException($"Unable to find entry - {key.GetType().FullName} key - {key.GetHashCode()}");
        }
        set
        {
            if (!Update(key, value))
            {
                throw new KeyNotFoundException($"Unable to find entry - {key.GetType().FullName} key - {key.GetHashCode()}");
            }
        }
    }

    /// <summary>
    /// Suppresses automatic table rebuilds during bulk delete or update operations.
    /// 
    /// Call this method before performing many Remove() calls to prevent the table
    /// from rebuilding multiple times due to tombstone accumulation.
    /// 
    /// Example:
    /// <code>
    /// map.BeginBulkRemove();
    /// foreach (var key in keysToRemove)
    /// {
    ///     map.Remove(key);
    /// }
    /// map.EndBulkRemove();
    /// </code>
    /// </summary>
    public void BeginBulkRemove()
    {
        _suppressRebuild = true;
    }

    /// <summary>
    /// Ends a bulk update session and performs a single rebuild if tombstones exist.
    /// 
    /// This restores optimal probe performance after large-scale deletions by compacting
    /// the table exactly once instead of rebuilding repeatedly.
    /// 
    /// Example:
    /// <code>
    /// map.BeginBulkRemove();
    /// foreach (var key in keysToRemove)
    /// {
    ///     map.Remove(key);
    /// }
    /// map.EndBulkRemove();
    /// </code>
    /// </summary>
    public void EndBulkRemove()
    {
        _suppressRebuild = false;

        // Perform a single compaction pass if tombstones accumulated.
        if (_tombstoneCounter > _maxTombstoneBeforeRehash)
        {
            Rebuild();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Resizes the map by doubling its size and rehashing all entries.
    /// </summary>     
    private void Resize()
    {
        var oldEntries = _entries;
        var oldControlBytes = _controlBytes;
        var oldLength = _length;

        _length = oldLength << 1;
        _mask = _length - 1;
        _maxLookupsBeforeResize = (int)(_length * _loadFactor);
        _tombstoneCounter = 0;
        _maxTombstoneBeforeRehash = (int)(_length * 0.125);

        var newEntries = GC.AllocateArray<Entry>((int)_length);
        var newControlBytes = GC.AllocateArray<sbyte>((int)(_length + 16));

        newControlBytes.AsSpan().Fill(_emptyBucket);

        ref sbyte newCtrl = ref MemoryMarshal.GetArrayDataReference(newControlBytes);
        ref Entry newEnt = ref MemoryMarshal.GetArrayDataReference(newEntries);

        ref sbyte oldCtrl = ref MemoryMarshal.GetArrayDataReference(oldControlBytes);
        ref Entry oldEnt = ref MemoryMarshal.GetArrayDataReference(oldEntries);

        for (uint i = 0; i < oldLength; ++i)
        {
            var ctrl = Unsafe.Add(ref oldCtrl, i);
            if (ctrl < 0)
                continue;

            var entry = Unsafe.Add(ref oldEnt, i);

            var hashcode = _hasher.ComputeHash(entry.Key);
            uint index = hashcode & _mask;
            uint jumpDistance = 0;

            while (true)
            {
                var source = Vector128.LoadUnsafe(ref newCtrl, index);
                var emptyMask = Vector128.Equals(source, _emptyBucketVector).ExtractMostSignificantBits();

                if (emptyMask != 0)
                {
                    uint bit = (uint)BitOperations.TrailingZeroCount(emptyMask);
                    uint slot = (index + bit) & _mask;

                    Unsafe.Add(ref newEnt, slot) = entry;
                    SetCtrl(ref newCtrl, slot, ctrl);
                    break;
                }

                index = (index + (jumpDistance += 16)) & _mask;
            }
        }

        _controlBytes = newControlBytes;
        _entries = newEntries;
    }


    /// <summary>
    /// Retrieves the 7 lowest bits from a hashcode.
    /// </summary>
    /// <param name="hashcode">The hashcode.</param>
    /// <returns>The 7 lowest bits of the hashcode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static sbyte H2(uint hashcode)
    {
        // Mix the bits so the top 7 are influenced by the bottom
        hashcode ^= (hashcode >> 16);

        // Extract 7 bits from the very top of the 32-bit range
        // 32 - 7 = 25
        return (sbyte)((hashcode >> 25) & 0x7F);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetCtrl(ref sbyte ctrl, uint slot, sbyte value)
    {
        Unsafe.Add(ref ctrl, slot) = value;

        // mirror first group into padding
        uint mirrorMask = (uint)-(slot >> 4 == 0 ? 1 : 0);
        Unsafe.Add(ref ctrl, slot + (mirrorMask & _length)) = value;
    }

    /// <summary>
    /// Resets the lowest significant bit in the given value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ResetLowestSetBit(uint value)
    {
        return value & (value - 1);
    }

    #endregion

    [StructLayout(LayoutKind.Sequential)]
    internal struct Entry
    {
        public TKey Key;
        public TValue Value;
    };
}