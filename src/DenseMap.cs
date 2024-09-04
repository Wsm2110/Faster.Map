// Copyright (c) 2024, Wiljan Ruizendaal. All rights reserved. <wruizendaal@gmail.com> 
// Distributed under the MIT Software License, Version 1.0.

#if NET7_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Faster.Map;

/// <summary>
/// DenseMap is a high-performance hashmap implementation that uses open-addressing with quadratic probing and SIMD (Single Instruction, Multiple Data) for parallel searches.
/// This map is designed for scenarios requiring efficient key-value storage with fast lookups, inserts, and deletions.
/// 
/// Key features:
/// - Open addressing with quadratic probing for collision resolution.
/// - SIMD-based parallel searches for performance optimization.
/// - High load factor (default is 0.9) while maintaining speed.
/// - Fibonacci hashing for better hash distribution.
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
public class DenseMap<TKey, TValue>
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
            for (int i = _metadata.Length - 1; i >= 0; --i)
            {
                if (_metadata[i] >= 0)
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
            for (int i = _metadata.Length - 1; i >= 0; --i)
            {
                if (_metadata[i] >= 0)
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
            for (int i = _metadata.Length - 1; i >= 0; --i)
            {
                if (_metadata[i] >= 0)
                {
                    yield return _entries[i].Value;
                }
            }
        }
    }

    #endregion

    #region Fields

    private const sbyte _emptyBucket = -127;
    private const sbyte _tombstone = -126;
    private static readonly Vector128<sbyte> _emptyBucketVector = Vector128.Create(_emptyBucket);
    private sbyte[] _metadata;
    private Entry[] _entries;
    private const uint _goldenRatio = 0x9E3779B9; // 2654435769;
    private uint _length;
    private byte _shift = 32;
    private double _maxLookupsBeforeResize;
    private uint _lengthMinusOne;
    private readonly double _loadFactor;
    private readonly IEqualityComparer<TKey> _comparer;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class with default parameters.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>();
    /// </code>
    /// </summary>
    public DenseMap() : this(16, 0.90, EqualityComparer<TKey>.Default) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class with the specified length and default load factor.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>(32);
    /// </code>
    /// </summary>
    /// <param name="length">The length of the hashmap. Will always take the closest power of two.</param>
    public DenseMap(uint length) : this(length, 0.90, EqualityComparer<TKey>.Default) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class with the specified length and load factor.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>(32, 0.8);
    /// </code>
    /// </summary>
    /// <param name="length">The length of the hashmap. Will always take the closest power of two.</param>
    /// <param name="loadFactor">The load factor determines when the hashmap will resize (default is 0.9).</param>
    public DenseMap(uint length, double loadFactor) : this(length, loadFactor, EqualityComparer<TKey>.Default) { }

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
    public DenseMap(uint length, double loadFactor, IEqualityComparer<TKey> keyComparer)
    {
        if (!Vector128.IsHardwareAccelerated)
        {
            throw new NotSupportedException("Your hardware does not support acceleration for 128-bit vectors.");
        }

        _length = length;
        _loadFactor = loadFactor;

        if (loadFactor > 0.9)
        {
            _loadFactor = 0.9;
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
        _comparer = keyComparer ?? EqualityComparer<TKey>.Default;
        _shift = (byte)(_shift - BitOperations.Log2(_length));
        _entries = new Entry[_length + 16];
        _metadata = new sbyte[_length + 16];

        Array.Fill(_metadata, _emptyBucket);
        _lengthMinusOne = _length - 1;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Inserts a key and value into the hashmap.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>();
    /// bool success = map.Emplace(1, "One");
    /// </code>
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>Returns false if the key already exists.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Emplace(TKey key, TValue value)
    {
        if (Count >= _maxLookupsBeforeResize)
        {
            Resize();
        }

        var hashcode = (uint)key.GetHashCode();
        var h2 = H2(hashcode);
        var target = Vector128.Create(Unsafe.As<uint, sbyte>(ref h2));
        uint index = (_goldenRatio * hashcode) >> _shift;
        uint jumpDistance = 0;

        while (true)
        {
            var source = Vector128.LoadUnsafe(ref Find(_metadata, index));
            var mask = Vector128.Equals(source, target).ExtractMostSignificantBits();

            while (mask != 0)
            {
                var bitPos = BitOperations.TrailingZeroCount(mask);
                var entry = Find(_entries, index + Unsafe.As<int, uint>(ref bitPos));

                if (_comparer.Equals(entry.Key, key))
                {
                    return false;
                }

                mask = ResetLowestSetBit(mask);
            }

            mask = source.ExtractMostSignificantBits();

            if (mask != 0)
            {
                var bitPos = BitOperations.TrailingZeroCount(mask);
                index += Unsafe.As<int, uint>(ref bitPos);

                Find(_metadata, index) = Unsafe.As<uint, sbyte>(ref h2);

                ref var currentEntry = ref Find(_entries, index);
                currentEntry.Key = key;
                currentEntry.Value = value;

                ++Count;
                return true;
            }

            jumpDistance += 16;
            index += jumpDistance;
            index &= _length - 1;
        }
    }

    /// <summary>
    /// Tries to emplace a key-value pair into the map. If the map already contains this key, updates the existing KeyValuePair.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>();
    /// map.AddOrUpdate(1, "One");
    /// map.AddOrUpdate(1, "One Updated");
    /// </code>
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddOrUpdate(TKey key, TValue value)
    {
        if (Count > _maxLookupsBeforeResize)
        {
            Resize();
        }

        var hashcode = (uint)key.GetHashCode();
        var h2 = H2(hashcode);
        var target = Vector128.Create(Unsafe.As<uint, sbyte>(ref h2));
        uint index = (_goldenRatio * hashcode) >> _shift;
        uint jumpDistance = 0;

        while (true)
        {
            var source = Vector128.LoadUnsafe(ref Find(_metadata, index));
            var mask = Vector128.Equals(target, source).ExtractMostSignificantBits();

            while (mask != 0)
            {
                var bitPos = BitOperations.TrailingZeroCount(mask);
                ref var entry = ref Find(_entries, index + Unsafe.As<int, uint>(ref bitPos));

                if (_comparer.Equals(entry.Key, key))
                {
                    entry.Value = value;
                    return;
                }

                mask = ResetLowestSetBit(mask);
            }

            mask = source.ExtractMostSignificantBits();

            if (mask > 0)
            {
                var bitPos = BitOperations.TrailingZeroCount(mask);
                index += Unsafe.As<int, uint>(ref bitPos);

                ref var currentEntry = ref Find(_entries, index);
                currentEntry.Key = key;
                currentEntry.Value = value;

                ref var metadata = ref Find(_metadata, index);
                metadata = Unsafe.As<uint, sbyte>(ref h2);

                ++Count;
                return;
            }

            jumpDistance += 16;
            index += jumpDistance;
            index &= _length - 1;
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
        var hashcode = (uint)key.GetHashCode();
        var h2 = H2(hashcode);
        var target = Vector128.Create(Unsafe.As<uint, sbyte>(ref h2));
        uint index = (_goldenRatio * hashcode) >> _shift;
        uint jumpDistance = 0;

        while (true)
        {
            var source = Vector128.LoadUnsafe(ref Find(_metadata, index));
            var mask = Vector128.Equals(target, source).ExtractMostSignificantBits();

            while (mask != 0)
            {
                var bitPos = BitOperations.TrailingZeroCount(mask);
                var entry = Find(_entries, index + Unsafe.As<int, byte>(ref bitPos));

                if (_comparer.Equals(entry.Key, key))
                {
                    value = entry.Value;
                    return true;
                }

                mask = ResetLowestSetBit(mask);
            }

            if (Vector128.Equals(source, _emptyBucketVector).ExtractMostSignificantBits() > 0)
            {
                value = default;
                return false;
            }

            jumpDistance += 16;
            index += jumpDistance;
            index &= _lengthMinusOne;
        }
    }

    /// <summary>
    /// Gets the value for the specified key, or, if the key is not present, adds an entry and returns the value by reference.
    /// This allows you to add or update a value in a single lookup operation.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, int>();
    /// ref var value = ref map.GetOrUpdate(1);
    /// value++;
    /// Console.WriteLine(value); // Output: 1
    /// </code>
    /// </summary>
    /// <param name="key">The key to look for.</param>
    /// <returns>Reference to the new or existing value.</returns>    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TValue GetOrUpdate(TKey key)
    {
        if (Count >= _maxLookupsBeforeResize)
        {
            Resize();
        }

        var hashcode = (uint)key.GetHashCode();
        var h2 = H2(hashcode);
        var target = Vector128.Create(Unsafe.As<uint, sbyte>(ref h2));
        uint index = (_goldenRatio * hashcode) >> _shift;
        uint jumpDistance = 0;

        while (true)
        {
            var source = Vector128.LoadUnsafe(ref Find(_metadata, index));
            var mask = Vector128.Equals(target, source).ExtractMostSignificantBits();

            while (mask != 0)
            {
                var bitPos = BitOperations.TrailingZeroCount(mask);
                ref var entry = ref Find(_entries, index + Unsafe.As<int, uint>(ref bitPos));

                if (_comparer.Equals(entry.Key, key))
                {
                    return ref entry.Value;
                }

                mask = ResetLowestSetBit(mask);
            }

            mask = source.ExtractMostSignificantBits();

            if (mask > 0)
            {
                var bitPos = BitOperations.TrailingZeroCount(mask);
                index += Unsafe.As<int, uint>(ref bitPos);

                ref var currentEntry = ref Find(_entries, index);
                currentEntry.Key = key;
                currentEntry.Value = default;

                ref var metadata = ref Find(_metadata, index);
                metadata = Unsafe.As<uint, sbyte>(ref h2);

                ++Count;
                return ref currentEntry.Value;
            }

            jumpDistance += 16;
            index += jumpDistance;
            index &= _lengthMinusOne;
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
        var hashcode = (uint)key.GetHashCode();
        var h2 = H2(hashcode);
        var target = Vector128.Create(Unsafe.As<uint, sbyte>(ref h2));
        uint index = (_goldenRatio * hashcode) >> _shift;
        uint jumpDistance = 0;

        while (true)
        {
            var source = Vector128.LoadUnsafe(ref Find(_metadata, index));
            var mask = Vector128.Equals(source, target).ExtractMostSignificantBits();

            while (mask != 0)
            {
                var bitPos = BitOperations.TrailingZeroCount(mask);
                ref var entry = ref Find(_entries, index + Unsafe.As<int, uint>(ref bitPos));

                if (_comparer.Equals(entry.Key, key))
                {
                    entry.Value = value;
                    return true;
                }

                mask = ResetLowestSetBit(mask);
            }

            if (Vector128.Equals(_emptyBucketVector, source).ExtractMostSignificantBits() > 0)
            {
                return false;
            }

            jumpDistance += 16;
            index += jumpDistance;
            index &= _length - 1;
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
        var hashcode = (uint)key.GetHashCode();
        var h2 = H2(hashcode);
        var target = Vector128.Create(Unsafe.As<uint, sbyte>(ref h2));
        uint index = (_goldenRatio * hashcode) >> _shift;
        uint jumpDistance = 0;

        while (true)
        {
            var source = Vector128.LoadUnsafe(ref Find(_metadata, index));
            var mask = Vector128.Equals(target, source).ExtractMostSignificantBits();

            while (mask != 0)
            {
                var bitPos = BitOperations.TrailingZeroCount(mask);

                if (_comparer.Equals(Find(_entries, index + Unsafe.As<int, uint>(ref bitPos)).Key, key))
                {
                    Find(_metadata, index + Unsafe.As<int, uint>(ref bitPos)) = _tombstone;
                    --Count;
                    return true;
                }

                mask = ResetLowestSetBit(mask);
            }

            if (Vector128.Equals(_emptyBucketVector, source).ExtractMostSignificantBits() != 0)
            {
                return false;
            }

            jumpDistance += 16;
            index += jumpDistance;
            index &= _lengthMinusOne;
        }
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
        var hashcode = (uint)key.GetHashCode();
        var h2 = H2(hashcode);
        var target = Vector128.Create(Unsafe.As<uint, sbyte>(ref h2));
        uint index = (_goldenRatio * hashcode) >> _shift;
        uint jumpDistance = 0;

        while (true)
        {
            var source = Vector128.LoadUnsafe(ref Find(_metadata, index));
            var mask = Vector128.Equals(target, source).ExtractMostSignificantBits();

            while (mask != 0)
            {
                var bitPos = BitOperations.TrailingZeroCount(mask);
                if (_comparer.Equals(Find(_entries, index + Unsafe.As<int, uint>(ref bitPos)).Key, key))
                {
                    return true;
                }

                mask = ResetLowestSetBit(mask);
            }

            if (Vector128.Equals(_emptyBucketVector, source).ExtractMostSignificantBits() != 0)
            {
                return false;
            }

            jumpDistance += 16;
            index += jumpDistance;
            index &= _lengthMinusOne;
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
    public void Copy(DenseMap<TKey, TValue> denseMap)
    {
        for (var i = 0; i < denseMap._entries.Length; ++i)
        {
            if (denseMap._metadata[i] < 0)
            {
                continue;
            }

            var entry = denseMap._entries[i];
            Emplace(entry.Key, entry.Value);
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
        Array.Fill(_metadata, _emptyBucket);
        Count = 0;
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

    #endregion

    #region Private Methods

    /// <summary>
    /// Resizes the map by doubling its size and rehashing all entries.
    /// </summary>     
    private void Resize()
    {
        _shift--;
        _length <<= 1;
        _lengthMinusOne = _length - 1;
        _maxLookupsBeforeResize = _length * _loadFactor;

        var oldEntries = _entries;
        var oldMetadata = _metadata;

        var size = Unsafe.As<uint, int>(ref _length) + 16;

        _metadata = GC.AllocateArray<sbyte>(size);
        _entries = GC.AllocateArray<Entry>(size);

        _metadata.AsSpan().Fill(_emptyBucket);

        for (uint i = 0; i < oldEntries.Length; ++i)
        {
            var h2 = Find(oldMetadata, i);
            if (h2 < 0)
            {
                continue;
            }

            var entry = Find(oldEntries, i);

            var hashcode = (uint)entry.Key.GetHashCode();
            uint index = (_goldenRatio * hashcode) >> _shift;
            uint jumpDistance = 0;

            while (true)
            {
                var mask = Vector128.LoadUnsafe(ref Find(_metadata, index)).ExtractMostSignificantBits();
                if (mask != 0)
                {
                    var bitPos = BitOperations.TrailingZeroCount(mask);
                    index += Unsafe.As<int, uint>(ref bitPos);

                    Find(_metadata, index) = h2;
                    Find(_entries, index) = entry;
                    break;
                }

                jumpDistance += 16;
                index += jumpDistance;
                index &= _lengthMinusOne;
            }
        }
    }

    /// <summary>
    /// Finds the element in the array at the specified index.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="array">The array to search.</param>
    /// <param name="index">The index to look up.</param>
    /// <returns>A reference to the found element.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref T Find<T>(T[] array, uint index)
    {
        ref var arr0 = ref MemoryMarshal.GetArrayDataReference(array);
        return ref Unsafe.Add(ref arr0, index);
    }

    /// <summary>
    /// Retrieves the 7 lowest bits from a hashcode.
    /// </summary>
    /// <param name="hashcode">The hashcode.</param>
    /// <returns>The 7 lowest bits of the hashcode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint H2(uint hashcode) => hashcode & 0b01111111;

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
    public struct Entry
    {
        public TKey Key;
        public TValue Value;
    };
}

#endif