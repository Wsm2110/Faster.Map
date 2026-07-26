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
    /// Initializes a new instance of the <see cref="DenseMap{TKey, TValue}"/> class
    /// with a default initial capacity of 16 and a load factor of 0.875.
    /// </summary>
    public DenseMap() : base(16, 0.875) { }
}

/// <summary>
/// DenseMap is a high-performance hashmap implementation that uses open-addressing with linear group probing and SIMD (Single Instruction, Multiple Data) for parallel searches.
/// This map is designed for scenarios requiring extreme key-value storage throughput with fast lookups, inserts, and deletions.
/// 
/// Key features:
/// - Array of Structs (AoS) layout for maximized spatial locality and L1 cache hits on the happy path.
/// - Asymmetric Metadata Sign-Bit mapping to eliminate SIMD equality checks during standard lookups.
/// - Open addressing with linear group probing to exploit CPU hardware prefetching.
/// - Branchless IL execution pipelines.
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
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Gets the size of the map.
    /// </summary>
    public uint Size => (uint)_entries.Length;

    /// <summary>
    /// Returns all the entries as KeyValuePair objects.
    /// </summary>
    public IEnumerable<KeyValuePair<TKey, TValue>> Entries
    {
        get
        {
            for (int i = 0; i < _length; ++i)
            {
                // Live entries are strictly > 0 (1 to 127). Tombstone is 0, Empty is -128.
                if (_controlBytes[i] > 0)
                {
                    var entry = _entries[i];
                    yield return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
                }
            }
        }
    }

    /// <summary>
    /// Returns all keys in the map.
    /// </summary>
    public IEnumerable<TKey> Keys
    {
        get
        {
            for (int i = 0; i < _length; ++i)
            {
                if (_controlBytes[i] > 0)
                {
                    yield return _entries[i].Key;
                }
            }
        }
    }

    /// <summary>
    /// Returns all values in the map.
    /// </summary>
    public IEnumerable<TValue> Values
    {
        get
        {
            for (int i = 0; i < _length; ++i)
            {
                if (_controlBytes[i] > 0)
                {
                    yield return _entries[i].Value;
                }
            }
        }
    }

    /// <summary>
    /// Vector256&lt;sbyte&gt; (AVX2) covers 32 control bytes per SIMD load/compare.
    /// </summary>
    private const uint _groupWidth = 32;

    #endregion

    #region Fields

    // WONDROUS OPTIMIZATION: Asymmetric Sign-Bit Layout
    // Empty is the ONLY state with the MSB set to 1.
    // Tombstone is 0 (MSB 0). Live hashes are 1..127 (MSB 0).
    private const sbyte _emptyBucket = -128; // Binary: 1000 0000
    private const sbyte _tombstone = 0;      // Binary: 0000 0000

    /// <summary>
    /// The array of control bytes utilized for SIMD probing.
    /// </summary>
    private sbyte[] _controlBytes;

    /// <summary>
    /// The array storing the actual key-value entries. Utilizing AoS for optimal spatial locality.
    /// </summary>
    private Entry[] _entries;

    /// <summary>
    /// The maximum number of tombstones allowed before forcing a map rehash.
    /// </summary>
    private double _maxTombstoneBeforeRehash;

    /// <summary>
    /// The active count of tombstones currently in the map.
    /// </summary>
    private uint _tombstoneCounter;

    /// <summary>
    /// The capacity of the map (must be a power of two).
    /// </summary>
    private uint _length;

    /// <summary>
    /// The upper limit of lookups (valid entries and tombstones) allowed prior to a structural resize.
    /// </summary>
    private double _maxLookupsBeforeResize;

    /// <summary>
    /// The bitmask applied to securely wrap indices within the boundaries of the map array.
    /// </summary>
    private uint _mask;

    /// <summary>
    /// The maximum load factor permitted before initiating a map resize.
    /// </summary>
    private readonly double _loadFactor;

    /// <summary>
    /// The hasher implementation used to compute hash codes for keys.
    /// </summary>
    private readonly THasher _hasher;

    /// <summary>
    /// A flag designating whether standard automated rebuild mechanisms are suppressed, typically used during bulk operations.
    /// </summary>
    private bool _suppressRebuild;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class with the specified length and default load factor.
    /// </summary>
    /// <param name="length">The length of the hashmap. Will always take the closest power of two.</param>
    public DenseMap(uint length) : this(length, 0.875) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class with the specified length and default load factor.
    /// </summary>
    public DenseMap() : this(16, 0.875) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class with the specified parameters.
    /// </summary>
    /// <param name="length">The length of the hashmap. Will always take the closest power of two.</param>
    /// <param name="loadFactor">The load factor determines when the hashmap will resize.</param>
    public DenseMap(uint length, double loadFactor)
    {
        if (!Vector256.IsHardwareAccelerated)
        {
            throw new NotSupportedException("Your hardware does not support acceleration for 256-bit vectors (AVX2 or equivalent is required).");
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

        _controlBytes = GC.AllocateArray<sbyte>((int)_length + (int)_groupWidth);
        _entries = GC.AllocateArray<Entry>((int)_length);

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
        if (Count + _tombstoneCounter >= _maxLookupsBeforeResize)
        {
            Resize();
        }

        var hashcode = _hasher.ComputeHash(key);
        var h2 = H2(hashcode);

        uint index = (uint)hashcode & _mask;
        uint firstAvailableSlot = uint.MaxValue;

        ref sbyte ctrl = ref MemoryMarshal.GetArrayDataReference(_controlBytes);
        ref Entry entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        while (true)
        {
            var source = Vector256.LoadUnsafe(ref ctrl, index);

            // Empty is the only negative byte. Tombstone is strictly zero.
            uint emptyMask = source.ExtractMostSignificantBits();
            uint tombstoneMask = Vector256.Equals(source, Vector256<sbyte>.Zero).ExtractMostSignificantBits();
            uint combinedMask = emptyMask | tombstoneMask;

            // Track the first available slot (Tombstone or Empty)
            if (firstAvailableSlot == uint.MaxValue && combinedMask != 0)
            {
                firstAvailableSlot = (index + (uint)BitOperations.TrailingZeroCount(combinedMask)) & _mask;
            }

            // Identify Empty slots (terminates the chain)
            if (emptyMask != 0)
            {
                uint slot = firstAvailableSlot != uint.MaxValue
                    ? firstAvailableSlot
                    : (index + (uint)BitOperations.TrailingZeroCount(emptyMask)) & _mask;

                // If we are replacing a tombstone, decrement the counter.
                if (Unsafe.Add(ref ctrl, slot) == _tombstone)
                {
                    _tombstoneCounter--;
                }

                SetCtrl(ref ctrl, slot, h2);

                ref var entry = ref Unsafe.Add(ref entryBase, slot);
                entry.Key = key;
                entry.Value = value;

                ++Count;
                return;
            }

            // Linear probing activates hardware prefetcher
            index = (index + _groupWidth) & _mask;
        }
    }

    /// <summary>
    /// Inserts or updates a key-value pair in the map.
    /// If the key is not already present, the specified value is inserted.
    /// If the key is present, the existing value is updated in place.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InsertOrUpdate(TKey key, TValue value)
    {
        if (Count + _tombstoneCounter >= _maxLookupsBeforeResize) Resize();

        var hash = _hasher.ComputeHash(key);
        var h2 = H2(hash);
        var target = Vector256.Create(h2);
        uint index = hash & _mask;
        uint firstAvailableSlot = uint.MaxValue;

        ref sbyte ctrl = ref MemoryMarshal.GetArrayDataReference(_controlBytes);
        ref Entry entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        while (true)
        {
            var source = Vector256.LoadUnsafe(ref ctrl, index);

            // 1. Check for Matches
            var matchMask = Vector256.Equals(source, target).ExtractMostSignificantBits();
            while (matchMask != 0)
            {
                uint slot = (index + (uint)BitOperations.TrailingZeroCount(matchMask)) & _mask;
                ref var matchEntry = ref Unsafe.Add(ref entryBase, slot);

                if (_hasher.Equals(matchEntry.Key, key))
                {
                    matchEntry.Value = value;
                    return;
                }
                matchMask &= matchMask - 1;
            }

            // 2. Track first available slot
            uint emptyMask = source.ExtractMostSignificantBits();
            uint tombstoneMask = Vector256.Equals(source, Vector256<sbyte>.Zero).ExtractMostSignificantBits();
            uint combinedMask = emptyMask | tombstoneMask;

            if (firstAvailableSlot == uint.MaxValue && combinedMask != 0)
            {
                firstAvailableSlot = (index + (uint)BitOperations.TrailingZeroCount(combinedMask)) & _mask;
            }

            // 3. Terminate on Empty Bucket
            if (emptyMask != 0)
            {
                uint slot = firstAvailableSlot != uint.MaxValue
                    ? firstAvailableSlot
                    : (index + (uint)BitOperations.TrailingZeroCount(emptyMask)) & _mask;

                if (Unsafe.Add(ref ctrl, slot) == _tombstone) _tombstoneCounter--;

                SetCtrl(ref ctrl, slot, h2);

                ref var newEntry = ref Unsafe.Add(ref entryBase, slot);
                newEntry.Key = key;
                newEntry.Value = value;
                Count++;
                return;
            }

            index = (index + _groupWidth) & _mask;
        }
    }

    /// <summary>
    /// Tries to find the key in the map and returns the associated value.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>Returns false if the key is not found.</returns>       
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Get(TKey key, out TValue value)
    {
        var hashcode = _hasher.ComputeHash(key);
        var h2 = H2(hashcode);
        var target = Vector256.Create(h2);

        uint index = hashcode & _mask;

        ref sbyte ctrl = ref MemoryMarshal.GetArrayDataReference(_controlBytes);
        ref Entry entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        while (true)
        {
            var source = Vector256.LoadUnsafe(ref ctrl, index);
            var matchMask = Vector256.Equals(source, target).ExtractMostSignificantBits();

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

            // WONDROUS OPTIMIZATION: A single vpmovmskb terminates the loop. No vector equality check.
            if (source.ExtractMostSignificantBits() != 0)
            {
                value = default;
                return false;
            }

            index = (index + _groupWidth) & _mask;
        }
    }

    /// <summary>
    /// Gets the value for the specified key, or, if the key is not present, adds an entry and returns the value by reference.
    /// </summary>
    /// <param name="key">The key to look for.</param>
    /// <returns>Reference to the new or existing value.</returns>  
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TValue GetValueRefOrAddDefault(TKey key)
    {
        if (Count + _tombstoneCounter >= _maxLookupsBeforeResize)
            Resize();

        var hashcode = _hasher.ComputeHash(key);
        var h2 = H2(hashcode);
        var target = Vector256.Create(h2);

        uint index = hashcode & _mask;
        uint firstAvailableSlot = uint.MaxValue;

        ref sbyte ctrl = ref MemoryMarshal.GetArrayDataReference(_controlBytes);
        ref Entry entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        while (true)
        {
            var source = Vector256.LoadUnsafe(ref ctrl, index);
            var matchMask = Vector256.Equals(source, target).ExtractMostSignificantBits();

            while (matchMask != 0)
            {
                uint bit = (uint)BitOperations.TrailingZeroCount(matchMask);
                uint slot = (index + bit) & _mask;

                ref var matchEntry = ref Unsafe.Add(ref entryBase, slot);
                if (_hasher.Equals(matchEntry.Key, key))
                    return ref matchEntry.Value;

                matchMask &= matchMask - 1;
            }

            uint emptyMask = source.ExtractMostSignificantBits();
            uint tombstoneMask = Vector256.Equals(source, Vector256<sbyte>.Zero).ExtractMostSignificantBits();
            uint combinedMask = emptyMask | tombstoneMask;

            if (firstAvailableSlot == uint.MaxValue && combinedMask != 0)
            {
                firstAvailableSlot = (index + (uint)BitOperations.TrailingZeroCount(combinedMask)) & _mask;
            }

            if (emptyMask != 0)
            {
                uint slot = firstAvailableSlot != uint.MaxValue
                    ? firstAvailableSlot
                    : (index + (uint)BitOperations.TrailingZeroCount(emptyMask)) & _mask;

                if (Unsafe.Add(ref ctrl, slot) == _tombstone) _tombstoneCounter--;

                SetCtrl(ref ctrl, slot, h2);

                ref var newEntry = ref Unsafe.Add(ref entryBase, slot);
                newEntry.Key = key;
                Count++;
                return ref newEntry.Value;
            }

            index = (index + _groupWidth) & _mask;
        }
    }

    /// <summary>
    /// Tries to find the key in the map and updates the associated value.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The new value.</param>
    /// <returns>Returns true if the update succeeded, otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Update(TKey key, TValue value)
    {
        var hashcode = _hasher.ComputeHash(key);
        var h2 = H2(hashcode);
        var target = Vector256.Create(h2);

        uint index = hashcode & _mask;

        ref sbyte ctrl = ref MemoryMarshal.GetArrayDataReference(_controlBytes);
        ref Entry entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        while (true)
        {
            var source = Vector256.LoadUnsafe(ref ctrl, index);
            var matchMask = Vector256.Equals(source, target).ExtractMostSignificantBits();

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

            if (source.ExtractMostSignificantBits() != 0) return false;

            index = (index + _groupWidth) & _mask;
        }
    }

    /// <summary>
    /// Determines whether it is safe to mark a soon-to-be-removed slot as fully Empty.
    /// </summary>
    /// <param name="ctrl">Reference to the start of the control-byte array.</param>
    /// <param name="index">The absolute index of the slot being removed.</param>
    /// <returns>True if it is safe to skip placing a tombstone.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool WasNeverFull(ref sbyte ctrl, uint index)
    {
        // Extracting MSB natively finds Empty buckets (-128) instantly.
        uint emptyAfterMask = Vector256.LoadUnsafe(ref ctrl, index).ExtractMostSignificantBits();
        uint emptyBeforeMask = Vector256.LoadUnsafe(ref ctrl, (index - _groupWidth) & _mask).ExtractMostSignificantBits();

        if (emptyAfterMask == 0 || emptyBeforeMask == 0) return false;

        int runRight = BitOperations.TrailingZeroCount(emptyAfterMask);
        int runLeft = BitOperations.LeadingZeroCount(emptyBeforeMask);

        return (runRight + runLeft) < _groupWidth;
    }

    /// <summary>
    /// Removes a key and value from the map.
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
        var target = Vector256.Create(h2);

        uint index = hashcode & _mask;

        ref sbyte ctrl = ref MemoryMarshal.GetArrayDataReference(_controlBytes);
        ref Entry entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        while (true)
        {
            var source = Vector256.LoadUnsafe(ref ctrl, index);
            var matchMask = Vector256.Equals(source, target).ExtractMostSignificantBits();

            while (matchMask != 0)
            {
                uint bit = (uint)BitOperations.TrailingZeroCount(matchMask);
                uint slot = (index + bit) & _mask;

                ref var entry = ref Unsafe.Add(ref entryBase, slot);
                if (_hasher.Equals(entry.Key, key))
                {
                    if (WasNeverFull(ref ctrl, slot))
                    {
                        SetCtrl(ref ctrl, slot, _emptyBucket);
                    }
                    else
                    {
                        SetCtrl(ref ctrl, slot, _tombstone);
                        _tombstoneCounter++;
                    }

                    entry = default;
                    --Count;
                    return true;
                }
                matchMask &= matchMask - 1;
            }

            if (source.ExtractMostSignificantBits() != 0) return false;

            index = (index + _groupWidth) & _mask;
        }
    }

    /// <summary>
    /// Rebuilds the internal data structures to optimize storage and access, typically after a resize or rehash
    /// operation.
    /// </summary>
    private void Rebuild()
    {
        var oldEntries = _entries;
        var oldControlBytes = _controlBytes;
        var length = _length;

        var newEntries = GC.AllocateArray<Entry>((int)length);
        var newControlBytes = GC.AllocateArray<sbyte>((int)length + (int)_groupWidth);

        newControlBytes.AsSpan().Fill(_emptyBucket);

        ref sbyte newCtrl = ref MemoryMarshal.GetArrayDataReference(newControlBytes);
        ref Entry newEnt = ref MemoryMarshal.GetArrayDataReference(newEntries);

        ref sbyte oldCtrl = ref MemoryMarshal.GetArrayDataReference(oldControlBytes);
        ref Entry oldEnt = ref MemoryMarshal.GetArrayDataReference(oldEntries);

        for (uint i = 0; i < length; ++i)
        {
            // Tombstones (0) and Empties (-128) are <= 0. Fast, single skip.
            var ctrl = Unsafe.Add(ref oldCtrl, i);
            if (ctrl <= 0) continue;

            var entry = Unsafe.Add(ref oldEnt, i);

            var hashcode = _hasher.ComputeHash(entry.Key);
            uint index = hashcode & _mask;

            while (true)
            {
                var source = Vector256.LoadUnsafe(ref newCtrl, index);
                uint emptyMask = source.ExtractMostSignificantBits();

                if (emptyMask != 0)
                {
                    uint bit = (uint)BitOperations.TrailingZeroCount(emptyMask);
                    uint slot = (index + bit) & _mask;

                    Unsafe.Add(ref newEnt, slot) = entry;
                    SetCtrl(ref newCtrl, slot, ctrl);
                    break;
                }

                index = (index + _groupWidth) & _mask;
            }
        }

        _controlBytes = newControlBytes;
        _entries = newEntries;
        _tombstoneCounter = 0;
    }

    /// <summary>
    /// Determines if the hashmap contains the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>Returns true if the key is found, otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(TKey key)
    {
        var hashcode = _hasher.ComputeHash(key);
        var h2 = H2(hashcode);
        var target = Vector256.Create(h2);
        var index = hashcode & _mask;

        ref sbyte start = ref MemoryMarshal.GetArrayDataReference(_controlBytes);
        ref Entry entryStart = ref MemoryMarshal.GetArrayDataReference(_entries);

        while (true)
        {
            var source = Vector256.LoadUnsafe(ref start, index);
            var mask = Vector256.Equals(source, target).ExtractMostSignificantBits();

            while (mask != 0)
            {
                var bitPos = BitOperations.TrailingZeroCount(mask);
                uint slot = (index + (uint)bitPos) & _mask;

                if (_hasher.Equals(Unsafe.Add(ref entryStart, slot).Key, key))
                {
                    return true;
                }

                mask = ResetLowestSetBit(mask);
            }

            // WONDROUS OPTIMIZATION: Terminate via pure sign-bit read.
            if (source.ExtractMostSignificantBits() != 0) return false;

            index = (index + _groupWidth) & _mask;
        }
    }

    /// <summary>
    /// Copies all entries from another DenseMap that uses the same hasher type.
    /// </summary>
    /// <param name="other">The map to copy from.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Copy(DenseMap<TKey, TValue, THasher> other)
    {
        var otherControlBytes = other._controlBytes;
        var otherEntries = other._entries;
        var otherLength = other._length;

        for (int i = 0; i < otherLength; ++i)
        {
            if (otherControlBytes[i] > 0)
            {
                ref var entry = ref otherEntries[i];
                InsertOrUpdate(entry.Key, entry.Value);
            }
        }
    }

    /// <summary>
    /// Removes all entries from the map and sets the count to 0.
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
    /// </summary>
    public void BeginBulkRemove()
    {
        _suppressRebuild = true;
    }

    /// <summary>
    /// Ends a bulk update session and performs a single rebuild if tombstones exist.
    /// </summary>
    public void EndBulkRemove()
    {
        _suppressRebuild = false;

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
        var newControlBytes = GC.AllocateArray<sbyte>((int)_length + (int)_groupWidth);

        newControlBytes.AsSpan().Fill(_emptyBucket);

        ref sbyte newCtrl = ref MemoryMarshal.GetArrayDataReference(newControlBytes);
        ref Entry newEnt = ref MemoryMarshal.GetArrayDataReference(newEntries);

        ref sbyte oldCtrl = ref MemoryMarshal.GetArrayDataReference(oldControlBytes);
        ref Entry oldEnt = ref MemoryMarshal.GetArrayDataReference(oldEntries);

        for (uint i = 0; i < oldLength; ++i)
        {
            var ctrl = Unsafe.Add(ref oldCtrl, i);
            if (ctrl <= 0)
                continue;

            var entry = Unsafe.Add(ref oldEnt, i);

            var hashcode = _hasher.ComputeHash(entry.Key);
            uint index = hashcode & _mask;

            while (true)
            {
                var source = Vector256.LoadUnsafe(ref newCtrl, index);

                // Fast path: In a newly allocated map, only empty buckets exist. No vector comparison needed.
                uint emptyMask = source.ExtractMostSignificantBits();

                if (emptyMask != 0)
                {
                    uint bit = (uint)BitOperations.TrailingZeroCount(emptyMask);
                    uint slot = (index + bit) & _mask;

                    Unsafe.Add(ref newEnt, slot) = entry;
                    SetCtrl(ref newCtrl, slot, ctrl);
                    break;
                }

                index = (index + _groupWidth) & _mask;
            }
        }

        _controlBytes = newControlBytes;
        _entries = newEntries;
    }


    /// <summary>
    /// Retrieves the 7 lowest bits from a hashcode and securely bounds them.
    /// </summary>
    /// <param name="hashcode">The hashcode.</param>
    /// <returns>The 7 lowest bits of the hashcode clamped strictly between 1 and 127.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static sbyte H2(uint hashcode)
    {
        uint h = hashcode >> 25;
        // Branchless clamp to 1..127. 
        // If h == 0, (h-1) >> 31 logical shifts to 1. If h > 0, evaluates to 0.
        return (sbyte)(h + ((h - 1) >> 31));
    }

    /// <summary>
    /// Sets the control byte for a designated slot, mirroring it when necessary to ensure 
    /// SIMD loads wrapping around the end of the array read the correct control bytes.
    /// </summary>
    /// <param name="ctrl">Reference to the control bytes array.</param>
    /// <param name="slot">The slot index to write to.</param>
    /// <param name="value">The control byte value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetCtrl(ref sbyte ctrl, uint slot, sbyte value)
    {
        Unsafe.Add(ref ctrl, slot) = value;

        // Pure arithmetic IL for mirroring to tail padding: sub -> sar -> and. 
        // No branching or conditional move (cmov) required.
        uint mirrorOffset = (uint)(((int)slot - 32) >> 31) & _length;
        Unsafe.Add(ref ctrl, slot + mirrorOffset) = value;
    }

    /// <summary>
    /// Resets the lowest significant bit in the given value.
    /// </summary>
    /// <param name="value">The input value.</param>
    /// <returns>The value with the lowest significant bit reset.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ResetLowestSetBit(uint value)
    {
        return value & (value - 1);
    }

    #endregion

    /// <summary>
    /// Represents an internal storage structure for mapping a key directly to its value.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct Entry
    {
        /// <summary>
        /// The stored key.
        /// </summary>
        public TKey Key;

        /// <summary>
        /// The associated value for the key.
        /// </summary>
        public TValue Value;
    };
}