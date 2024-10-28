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
            for (int i = _controlBytes.Length - 1; i >= 0; --i)
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
            for (int i = _controlBytes.Length - 1; i >= 0; --i)
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
            for (int i = _controlBytes.Length - 1; i >= 0; --i)
            {
                if (_controlBytes[i] >= 0)
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
    private static readonly Vector128<sbyte> _tombstoneVector = Vector128.Create(_tombstone);
    private sbyte[] _controlBytes;
    private double _maxTombstoneBeforeRehash;
    private uint _tombstoneCounter;
    private Entry[] _entries;
    private const uint _goldenRatio = 0x9E3779B9; // 2654435769;
    private uint _length;
    private byte _shift = 32;
    private double _maxLookupsBeforeResize;
    private uint _lengthMinusOne;
    private readonly double _loadFactor;
    private readonly IEqualityComparer<TKey> _comparer;
    private const double baseThreshold = 0.125; // 12.5% of entries as tombstones
 
    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class with default parameters.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>();
    /// </code>
    /// </summary>
    public DenseMap() : this(16, 0.90) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class with the specified length and default load factor.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>(32);
    /// </code>
    /// </summary>
    /// <param name="length">The length of the hashmap. Will always take the closest power of two.</param>
    public DenseMap(uint length) : this(length, 0.90) { }

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
        _comparer = EqualityComparer<TKey>.Default;
        _shift = (byte)(_shift - BitOperations.Log2(_length));
        _entries = new Entry[_length + 16];
        _controlBytes = new sbyte[_length + 16];

        // Calculate the weight factor based on the table size
        double weightFactor = CalculateWeightFactor(_length);

        // Calculate max tombstones before rehash based on the dynamic weight factor
        _maxTombstoneBeforeRehash = (uint)(baseThreshold * _length * weightFactor * (1 - (loadFactor / _length)));

        Array.Fill(_controlBytes, _emptyBucket);
        _lengthMinusOne = _length - 1;
    }

    #endregion

    #region Public Methods

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
    public bool Emplace(TKey key, TValue value)
    {
        // Check if the table is full based on the maximum lookups before needing to resize.
        // Resize if the count exceeds the threshold.
        if (Count >= _maxLookupsBeforeResize)
        {
            Resize();
        }

        // Calculate the hash code for the key and cast it to uint for non-negative indexing.
        var hashcode = (uint)key.GetHashCode();
        // Generate a secondary hash value 'h2' for probing, used to avoid clustering.
        var h2 = H2(hashcode);
        // Create a SIMD vector with the value of 'h2' for quick equality checks.
        var target = Vector128.Create(h2);
        // Calculate the initial index by hashing the hashcode with a multiplicative hash function.
        uint index = _goldenRatio * hashcode >> _shift;
        // Initialize the probing jump distance to zero, which will increase with each probe iteration.
        uint jumpDistance = 0;

        // Fast path: if the control byte at 'index' indicates an empty bucket, insert directly.
        // Note: No need to use an expensive equalitycomparer
        ref var controlbyte = ref Find(_controlBytes, index);
        if (controlbyte == _emptyBucket)
        {
            // Claim the bucket by setting its control byte to 'h2'.
            controlbyte = h2;
            // Get a reference to the entry at 'index' and assign the key-value pair.
            ref var entry = ref Find(_entries, index);
            entry.Value = value;
            entry.Key = key;

            // Increment the count and return the newly inserted value.
            Count++;
            return true;
        }

        // `indexSimd` is used to track a potential position to place the new entry.
        int indexSimd = -1;

        while (true)
        {
            // Load a 128-bit vector from `_controlBytes` at 'index' to check for matching control bytes.
            var source = Vector128.LoadUnsafe(ref Find(_controlBytes, index));
            // Compare `source` and `target` vectors to find any positions with a matching control byte.
            var mask = Vector128.Equals(source, target).ExtractMostSignificantBits();
            // Loop over each set bit in `mask` (indicating matching positions).
            while (mask != 0)
            {
                // Find the lowest set bit in `mask` (first matching position).
                var bitPos = BitOperations.TrailingZeroCount(mask);
                // Use `bitPos` to access the corresponding entry in `_entries`.
                ref var entry = ref Find(_entries, index + Unsafe.As<int, uint>(ref bitPos));
                // If a matching key is found, update the entry's value and return the old value.
                if (_comparer.Equals(entry.Key, key))
                {
                    entry.Value = value;
                    return true;
                }

                // Clear the lowest set bit in `mask` to continue with the next matching bit.
                mask = ResetLowestSetBit(mask);
            }

            // If `entrySimd` is still null, check for tombstone markers in `source`.
            if (indexSimd == -1)
            {
                // Check for tombstones (deleted entries) in the vector.
                mask = Vector128.Equals(source, _tombstoneVector).ExtractMostSignificantBits();
                if (mask != 0)
                {
                    indexSimd = (int)index + BitOperations.TrailingZeroCount(mask);
                }
            }

            // Check for empty buckets in the current vector.
            mask = Vector128.Equals(source, _emptyBucketVector).ExtractMostSignificantBits();
            if (mask != 0)
            {
                // If an empty bucket is found and `entrySimd` is not set, use this as the insertion point.
                // The empty bucket marks the end of a probeChain.
                if (indexSimd == -1)
                {
                    indexSimd = (int)index + BitOperations.TrailingZeroCount(mask);
                }

                ref var entrySimd = ref Find(_entries, indexSimd);

                entrySimd.Key = key;
                entrySimd.Value = value;

                Find(_controlBytes, indexSimd) = h2;

                ++Count;
                return true;
            }

            // Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
            // So first we jump by 1 group (meaning we just continue our linear scan), then 2 groups (skipping over 1 group), then 3 groups (skipping over 2 groups), and so on.
            // Interestingly, this pattern perfectly lines up with our power-of-two size such that we will visit every single bucket exactly once without any repeats(searching is therefore guaranteed to terminate as we always have at least one EMPTY bucket).
            // Note: that our non-linear probing strategy makes us fairly robust against weird degenerate collision chains that can make us accidentally quadratic(Hash DoS).
            // Note: that we expect to almost never actually probe, since that’s WIDTH(16) non-EMPTY buckets we need to fail to find our key in.

            jumpDistance += 16; // Increase the jump distance by 16 to probe the next cluster.
            index += jumpDistance; // Move the index forward by the jump distance.           
            index &= _lengthMinusOne; // Use bitwise AND to ensure the index wraps around within the bounds of the map. Thus preventing out of bounds exceptions
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
        // Compute the hash code for the given key and cast it to an unsigned integer for bitwise operations.
        var hashcode = (uint)key.GetHashCode();
        // Apply a secondary hash function to further spread the bits of the hashcode to reduce clustering.
        var h2 = H2(hashcode);
        // Create a 128-bit vector that holds the transformed hash code, which is used for comparisons.
        var target = Vector128.Create(h2);
        // Calculate the initial index position in the map based on a multiplication with a constant (_goldenRatio)
        // and a bitwise right shift. This helps spread the entries across the map's range.
        uint index = _goldenRatio * hashcode >> _shift;
        // Initialize a variable to keep track of the distance to jump when probing the map.
        uint jumpDistance = 0;

        // Loop until we find either a match or an empty slot.
        while (true)
        {
            // Load a vector from the control bytes starting at the computed index.
            // Control bytes hold metadata about the entries in the map.
            var source = Vector128.LoadUnsafe(ref Find(_controlBytes, index));
            // Compare the target vector (hashed key) with the loaded source vector to find matches.
            // `ExtractMostSignificantBits()` returns a mask where each bit set indicates a match.
            var mask = Vector128.Equals(target, source).ExtractMostSignificantBits();

            // Process any matches indicated by the mask.
            while (mask != 0)
            {
                // Get the position of the first set bit in the mask (indicating a match).
                var bitPos = BitOperations.TrailingZeroCount(mask);

                // Retrieve the entry corresponding to the matched bit position within the map's entries.
                var entry = Find(_entries, index + Unsafe.As<int, byte>(ref bitPos));

                // Check if the entry's key matches the specified key using the equality comparer.
                if (_comparer.Equals(entry.Key, key))
                {
                    // If a match is found, set the output value and return true.
                    value = entry.Value;
                    return true;
                }

                // Clear the lowest set bit in the mask to check for additional matches in this iteration.
                mask = ResetLowestSetBit(mask);
            }

            // Detecting _empty allows the function to exit early without unnecessary checks, improving lookup performance.
            // Continuing beyond an empty entry would waste processing time, as it’s certain that no entries following it could match the key(in open addressing, keys are always clustered together without gaps until an empty slot).
            if (Vector128.Equals(source, _emptyBucketVector).ExtractMostSignificantBits() > 0)
            {
                // Set the output value to the default and return false, as no match was found.
                value = default;
                return false;
            }

            // Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
            // So first we jump by 1 group (meaning we just continue our linear scan), then 2 groups (skipping over 1 group), then 3 groups (skipping over 2 groups), and so on.
            // Interestingly, this pattern perfectly lines up with our power-of-two size such that we will visit every single bucket exactly once without any repeats(searching is therefore guaranteed to terminate as we always have at least one EMPTY bucket).
            // Note: that our non-linear probing strategy makes us fairly robust against weird degenerate collision chains that can make us accidentally quadratic(Hash DoS).
            // Note: that we expect to almost never actually probe, since that’s WIDTH(16) non-EMPTY buckets we need to fail to find our key in.

            jumpDistance += 16; // Increase the jump distance by 16 to probe the next cluster.
            index += jumpDistance; // Move the index forward by the jump distance.           
            index &= _lengthMinusOne; // Use bitwise AND to ensure the index wraps around within the bounds of the map. Thus preventing out of bounds exceptions
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
        // Check if the table is full based on the maximum lookups before needing to resize.
        // Resize if the count exceeds the threshold.
        if (Count >= _maxLookupsBeforeResize)
        {
            Resize();
        }

        // Calculate the hash code for the key and cast it to uint for non-negative indexing.
        var hashcode = (uint)key.GetHashCode();
        // Generate a secondary hash value 'h2' for probing, used to avoid clustering.
        var h2 = H2(hashcode);
        // Create a SIMD vector with the value of 'h2' for quick equality checks.
        var target = Vector128.Create(h2);
        // Calculate the initial index by hashing the hashcode with a multiplicative hash function.
        uint index = _goldenRatio * hashcode >> _shift;
        // Initialize the probing jump distance to zero, which will increase with each probe iteration.
        uint jumpDistance = 0;

        // Fast path: if the control byte at 'index' indicates an empty bucket, insert directly.
        // Note: No need to use an expensive equalitycomparer
        ref var controlbyte = ref Find(_controlBytes, index);
        if (controlbyte == _emptyBucket)
        {
            // Claim the bucket by setting its control byte to 'h2'.
            controlbyte = h2;
            // Get a reference to the entry at 'index' and assign the key-value pair.
            ref var entry = ref Find(_entries, index);
            entry.Key = key;

            // Increment the count and return the newly inserted value.
            Count++;
            return ref entry.Value;
        }

        // `indexSimd` is used to track a potential position to place the new entry.
        int indexSimd = -1;

        while (true)
        {
            // Load a 128-bit vector from `_controlBytes` at 'index' to check for matching control bytes.
            var source = Vector128.LoadUnsafe(ref Find(_controlBytes, index));
            // Compare `source` and `target` vectors to find any positions with a matching control byte.
            var mask = Vector128.Equals(source, target).ExtractMostSignificantBits();
            // Loop over each set bit in `mask` (indicating matching positions).
            while (mask != 0)
            {
                // Find the lowest set bit in `mask` (first matching position).
                var bitPos = BitOperations.TrailingZeroCount(mask);
                // Use `bitPos` to access the corresponding entry in `_entries`.
                ref var entry = ref Find(_entries, index + Unsafe.As<int, uint>(ref bitPos));
                // If a matching key is found, update the entry's value and return the old value.
                if (_comparer.Equals(entry.Key, key))
                {
                    return ref entry.Value;
                }

                // Clear the lowest set bit in `mask` to continue with the next matching bit.
                mask = ResetLowestSetBit(mask);
            }

            // If `entrySimd` is still null, check for tombstone markers in `source`.
            if (indexSimd == -1)
            {
                // Check for tombstones (deleted entries) in the vector.
                mask = Vector128.Equals(source, _tombstoneVector).ExtractMostSignificantBits();
                if (mask != 0)
                {
                    indexSimd = (int)index + BitOperations.TrailingZeroCount(mask);
                }
            }

            // Check for empty buckets in the current vector.
            mask = Vector128.Equals(source, _emptyBucketVector).ExtractMostSignificantBits();
            if (mask != 0)
            {
                // If an empty bucket is found and `entrySimd` is not set, use this as the insertion point.
                // The empty bucket marks the end of a probeChain.
                if (indexSimd == -1)
                {
                    indexSimd = (int)index + BitOperations.TrailingZeroCount(mask);
                }

                ref var entrySimd = ref Find(_entries, indexSimd);

                entrySimd.Key = key;

                Find(_controlBytes, indexSimd) = h2;

                ++Count;

                return ref entrySimd.Value;
            }

            // Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
            // So first we jump by 1 group (meaning we just continue our linear scan), then 2 groups (skipping over 1 group), then 3 groups (skipping over 2 groups), and so on.
            // Interestingly, this pattern perfectly lines up with our power-of-two size such that we will visit every single bucket exactly once without any repeats(searching is therefore guaranteed to terminate as we always have at least one EMPTY bucket).
            // Note: that our non-linear probing strategy makes us fairly robust against weird degenerate collision chains that can make us accidentally quadratic(Hash DoS).
            // Note: that we expect to almost never actually probe, since that’s WIDTH(16) non-EMPTY buckets we need to fail to find our key in.

            jumpDistance += 16; // Increase the jump distance by 16 to probe the next cluster.
            index += jumpDistance; // Move the index forward by the jump distance.           
            index &= _lengthMinusOne; // Use bitwise AND to ensure the index wraps around within the bounds of the map. Thus preventing out of bounds exceptions
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
        // Compute the hash code for the given key and cast to an unsigned integer for bitwise operations.
        var hashcode = (uint)key.GetHashCode();
        // Apply a secondary hash function to further spread out hashcode bits.
        var h2 = H2(hashcode);
        // Create a 128-bit vector from the transformed hash code to use as the target for comparison.
        var target = Vector128.Create(h2);
        // Calculate the initial index position in the hash map, using `_goldenRatio` and a right shift.
        uint index = (_goldenRatio * hashcode) >> _shift;
        // Initialize `jumpDistance` to control the distance between probes, starting at zero.
        uint jumpDistance = 0;

        // Loop until we either find the key to update or confirm it's absent.
        while (true)
        {
            // Load a vector from `_controlBytes` at the calculated index.
            // `_controlBytes` holds control bytes, indicating metadata about each map entry.
            var source = Vector128.LoadUnsafe(ref Find(_controlBytes, index));

            // Compare the `source` vector with the `target` vector. `ExtractMostSignificantBits` produces a bit mask
            // where each set bit corresponds to a position that matches the target hash.
            var mask = Vector128.Equals(source, target).ExtractMostSignificantBits();

            // Process any matching entries indicated by bits set in `mask`.
            while (mask != 0)
            {
                // Get the position of the first set bit in `mask`, which indicates a possible match.
                var bitPos = BitOperations.TrailingZeroCount(mask);

                // Find the corresponding entry in `_entries` by calculating the exact position from `index` and `bitPos`.
                ref var entry = ref Find(_entries, index + Unsafe.As<int, uint>(ref bitPos));

                // Check if the current entry's key matches the specified key using the equality comparer.
                if (_comparer.Equals(entry.Key, key))
                {
                    // If a match is found, update the entry's value and return `true` to indicate success.
                    entry.Value = value;
                    return true;
                }

                // Clear the lowest set bit in `mask` to continue checking any remaining matches in this vector.
                mask = ResetLowestSetBit(mask);
            }

            // Check if the `source` vector contains any empty buckets.
            // If an empty bucket is found, it indicates the end of the probe chain, and the key is not present.
            if (Vector128.Equals(_emptyBucketVector, source).ExtractMostSignificantBits() > 0)
            {
                // Return `false` to indicate the key does not exist in the map, so no update was made.
                return false;
            }

            // Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
            // So first we jump by 1 group (meaning we just continue our linear scan), then 2 groups (skipping over 1 group), then 3 groups (skipping over 2 groups), and so on.
            // Interestingly, this pattern perfectly lines up with our power-of-two size such that we will visit every single bucket exactly once without any repeats(searching is therefore guaranteed to terminate as we always have at least one EMPTY bucket).
            // Note: that our non-linear probing strategy makes us fairly robust against weird degenerate collision chains that can make us accidentally quadratic(Hash DoS).
            // Note: that we expect to almost never actually probe, since that’s WIDTH(16) non-EMPTY buckets we need to fail to find our key in.

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
        if (_tombstoneCounter >= _maxTombstoneBeforeRehash)
        {
            Rehash();
        }
        // Compute the hash code for the given key and cast it to an unsigned integer for bitwise operations.
        var hashcode = (uint)key.GetHashCode();
        // Apply a secondary hash function to further spread the hash bits.
        var h2 = H2(hashcode);
        // Create a 128-bit vector from the transformed hash code to use as the target for comparison.
        var target = Vector128.Create(h2);
        // Calculate the initial index position in the hash map, based on `_goldenRatio` and bit-shifting.
        uint index = (_goldenRatio * hashcode) >> _shift;
        // Initialize `jumpDistance` to control the distance between probes, starting at zero.
        uint jumpDistance = 0;

        // Begin probing until either the key is found and removed, or it's confirmed as absent.
        while (true)
        {
            // Load a vector from `_controlBytes` at the calculated index.
            // `_controlBytes` contains metadata for each slot in the map.
            var source = Vector128.LoadUnsafe(ref Find(_controlBytes, index));

            // Compare `source` with `target`. `ExtractMostSignificantBits` returns a bitmask
            // where each set bit represents a potential match with `target`.
            var mask = Vector128.Equals(source, target).ExtractMostSignificantBits();

            // Process each matching bit in `mask`.
            while (mask != 0)
            {
                // Get the position of the first set bit in `mask`, indicating a potential key match.
                var bitPos = BitOperations.TrailingZeroCount(mask);

                // Check if the entry at the matched position has a key that equals the specified key.
                // Use `_comparer` to ensure accurate key comparison.
                if (_comparer.Equals(Find(_entries, index + Unsafe.As<int, uint>(ref bitPos)).Key, key))
                {
                    // If a match is found, mark the control byte as a tombstone to indicate a deleted entry.
                    Find(_controlBytes, index + Unsafe.As<int, uint>(ref bitPos)) = _tombstone;

                    // Reset to default
                    Find(_entries, index + Unsafe.As<int, uint>(ref bitPos)) = default;

                    // Decrement the `Count` to reflect the removal of an entry.
                    --Count;

                    _tombstoneCounter++;

                    // Return `true` to indicate that the key was successfully removed.
                    return true;
                }

                // Clear the lowest set bit in `mask` to continue checking any remaining matches in the vector.
                mask = ResetLowestSetBit(mask);
            }

            // Check if `source` contains any empty buckets, which would indicate the end of the probe chain.
            // If an empty bucket is found, the key is not in the map.
            if (Vector128.Equals(_emptyBucketVector, source).ExtractMostSignificantBits() != 0)
            {
                // Return `false` since the key was not found and no removal was performed.
                return false;
            }

            // Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
            // So first we jump by 1 group (meaning we just continue our linear scan), then 2 groups (skipping over 1 group), then 3 groups (skipping over 2 groups), and so on.
            // Interestingly, this pattern perfectly lines up with our power-of-two size such that we will visit every single bucket exactly once without any repeats(searching is therefore guaranteed to terminate as we always have at least one EMPTY bucket).
            // Note: that our non-linear probing strategy makes us fairly robust against weird degenerate collision chains that can make us accidentally quadratic(Hash DoS).
            // Note: that we expect to almost never actually probe, since that’s WIDTH(16) non-EMPTY buckets we need to fail to find our key in.
            jumpDistance += 16;
            index += jumpDistance;
            index &= _lengthMinusOne; // Ensures `index` stays within valid map indices.
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
        // Compute the hash code for the given key and cast it to an unsigned integer for bitwise operations.
        var hashcode = (uint)key.GetHashCode();
        // Apply a secondary hash function to further spread the bits of the hashcode.
        var h2 = H2(hashcode);
        // Create a 128-bit vector from the transformed hash code to use as the target for comparison.
        var target = Vector128.Create(h2);
        // Calculate the initial index position in the hash map, using `_goldenRatio` and a right shift.
        uint index = _goldenRatio * hashcode >> _shift;
        // Initialize `jumpDistance` to control the distance between probes, starting at zero.
        uint jumpDistance = 0;

        // Begin probing the hash map until the key is found or confirmed absent.
        while (true)
        {
            // Load a vector from `_controlBytes` at the calculated index.
            // `_controlBytes` holds metadata about each slot in the map.
            var source = Vector128.LoadUnsafe(ref Find(_controlBytes, index));

            // Compare `source` with `target`, and `ExtractMostSignificantBits` returns a bitmask
            // where each set bit indicates a position in `source` that matches `target`.
            var mask = Vector128.Equals(source, target).ExtractMostSignificantBits();

            // Process each match indicated by the bits set in `mask`.
            while (mask != 0)
            {
                // Get the position of the first set bit in `mask`, indicating a potential key match.
                var bitPos = BitOperations.TrailingZeroCount(mask);

                // Check if the entry at this position has a key that matches the specified key.
                // Use `_comparer` to ensure accurate key comparison.
                if (_comparer.Equals(Find(_entries, index + Unsafe.As<int, uint>(ref bitPos)).Key, key))
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

            jumpDistance += 16;
            // Move the index forward by the updated `jumpDistance`, wrapping within map bounds.
            index += jumpDistance;
            index &= _lengthMinusOne; // Ensures the index remains within the valid range of the map.
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
            if (denseMap._controlBytes[i] < 0)
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
        Array.Fill(_controlBytes, _emptyBucket);
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
    /// Calculates a weight factor based on the table size.
    /// The weight factor decreases as the table size grows.
    /// </summary>
    /// <param name="tableSize">The current size of the table.</param>
    /// <returns>The weight factor to apply to the tombstone limit.</returns>
    private double CalculateWeightFactor(uint tableSize)
    {
        // Start with a higher weight factor for small tables and reduce gradually
        if (tableSize <= 16) return 3;
        if (tableSize <= 32) return 2.75;
        if (tableSize <= 64) return 2.5;
        if (tableSize <= 128) return 2.25;
        if (tableSize <= 256) return 2;
        if (tableSize <= 512) return 1.75;
        if (tableSize <= 1024) return 1.5;
        if (tableSize <= 2048) return 1.25;

        // For larger tables, return the base threshold weight factor
        return 1.0;
    }

    private void Rehash()
    {
        var oldEntries = _entries;
        var oldMetadata = _controlBytes;
        _tombstoneCounter = 0;

        var size = Unsafe.As<uint, int>(ref _length) + 16;

        _controlBytes = GC.AllocateArray<sbyte>(size);
        _entries = GC.AllocateArray<Entry>(size);

        _controlBytes.AsSpan().Fill(_emptyBucket);

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
                var mask = Vector128.LoadUnsafe(ref Find(_controlBytes, index)).ExtractMostSignificantBits();
                if (mask != 0)
                {
                    var bitPos = BitOperations.TrailingZeroCount(mask);
                    index += Unsafe.As<int, uint>(ref bitPos);

                    Find(_controlBytes, index) = h2;
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
    /// Resizes the map by doubling its size and rehashing all entries.
    /// </summary>     
    private void Resize()
    {
        _shift--;
        _length <<= 1;
        _lengthMinusOne = _length - 1;
        _maxLookupsBeforeResize = _length * _loadFactor;

        _tombstoneCounter = 0;

        // Calculate the weight factor based on the table size
        double weightFactor = CalculateWeightFactor(_length);

        // Calculate max tombstones before rehash based on the dynamic weight factor
        _maxTombstoneBeforeRehash = (uint)(baseThreshold * _length * weightFactor * (1 - (_loadFactor / _length)));

        var oldEntries = _entries;
        var oldMetadata = _controlBytes;

        var size = Unsafe.As<uint, int>(ref _length) + 16;

        _controlBytes = GC.AllocateArray<sbyte>(size);
        _entries = GC.AllocateArray<Entry>(size);

        _controlBytes.AsSpan().Fill(_emptyBucket);

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
                var mask = Vector128.LoadUnsafe(ref Find(_controlBytes, index)).ExtractMostSignificantBits();
                if (mask != 0)
                {
                    var bitPos = BitOperations.TrailingZeroCount(mask);
                    index += Unsafe.As<int, uint>(ref bitPos);

                    Find(_controlBytes, index) = h2;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref T Find<T>(T[] array, int index)
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
    private static sbyte H2(uint hashcode) => (sbyte)(hashcode & 0b01111111);

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

#endif