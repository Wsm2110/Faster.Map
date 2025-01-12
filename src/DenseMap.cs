// Copyright (c) 2024, Wiljan Ruizendaal. All rights reserved. <wruizendaal@gmail.com> 
// Distributed under the MIT Software License, Version 1.0.

using Faster.Map.Contracts;
using Faster.Map.Hasher;
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

    private int _groupWidth = 16;

    #endregion

    #region Fields
    private ulong _groupMask;
    private const uint ElementsInGroupMinusOne = 15;
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
    private uint _lengthMinusOne;
    private readonly double _loadFactor;
    private readonly IHasher<TKey> _hasher;
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
    public DenseMap(IHasher<TKey> hasher) : this(16, 0.875, hasher) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class with default parameters.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>();
    /// </code>
    /// </summary>
    public DenseMap() : this(16, 0.875, new GoldenRatioHasher<TKey>()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class with the specified length and default load factor.
    /// Example:
    /// <code>
    /// var map = new DenseMap<int, string>(32);
    /// </code>
    /// </summary>
    /// <param name="length">The length of the hashmap. Will always take the closest power of two.</param>
    public DenseMap(uint length) : this(length, 0.875, new GoldenRatioHasher<TKey>()) { }

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
    public DenseMap(uint length, double loadFactor, IHasher<TKey> hasher = null)
    {
        if (!Vector128.IsHardwareAccelerated)
        {
            throw new NotSupportedException("Your hardware does not support acceleration for 128-bit vectors.");
        }

        _length = length;
        _loadFactor = loadFactor;
        _hasher = hasher ?? new GoldenRatioHasher<TKey>();

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
        _comparer = EqualityComparer<TKey>.Default;

        _controlBytes = GC.AllocateArray<sbyte>((int)_length, true);
        _entries = GC.AllocateArray<Entry>((int)_length);
        Array.Fill(_controlBytes, _emptyBucket);
        
        _maxTombstoneBeforeRehash = _length * 0.125;
        _lengthMinusOne = _length - 1;
        _groupMask = _lengthMinusOne & ~ElementsInGroupMinusOne;
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
    public unsafe void Emplace(TKey key, TValue value)
    {
        // Check if the table is full based on the maximum lookups before needing to resize.
        // Resize if the count exceeds the threshold.
        if (Count >= _maxLookupsBeforeResize)
        {
            Resize();
        }

        // Calculate the hash code for the key and cast it to uint for non-negative indexing.
        var hashcode = _hasher.ComputeHash(key);
        // Generate a secondary hash value 'h2' for probing, used to avoid clustering.
        var h2 = H2(hashcode);
        // Create a SIMD vector with the value of 'h2' for quick equality checks.
        var target = Vector128.Create(h2);
        // This operation ensures that `index` is in the range [0, capacity - 1] by using only the lower bits of `hashcode`,
        // which helps in efficient and quick indexing.
        var index = hashcode & _groupMask;
        // Initialize the probing jump distance to zero, which will increase with each probe iteration.
        byte jumpDistance = 0;

        while (true)
        {
            // Load a vector from the control bytes starting at the computed index.
            // Control bytes hold metadata about the entries in the map.
            var source = ReadVector128(_controlBytes, index);
            // Compare `source` and `target` vectors to find any positions with a matching control byte.
            var resultMask = Vector128.Equals(source, target).ExtractMostSignificantBits();
            // Loop over each set bit in `mask` (indicating matching positions).
            while (resultMask != 0)
            {
                // Find the lowest set bit in `mask` (first matching position). 
                ref var entry = ref Find(_entries, index + (uint)BitOperations.TrailingZeroCount(resultMask));
                // If a matching key is found, update the entry's value and return the old value.
                if (_comparer.Equals(entry.Key, key))
                {
                    entry.Value = value;
                    return;
                }

                // Clear the lowest set bit in `mask` to continue with the next matching bit.
                resultMask = ResetLowestSetBit(resultMask);
            }

            // Note: we arent using tombstones, we just leave them for dead.
            // This means we are increasing the probe chain, but then again we dont expect there to be alot of tombstones
            // Probe sequences terminate at the first empty slot they encounter, having an empty slot in the group means that "removing" the current entry without placing a tombstone won’t disrupt probe chains.
            // More information at "Remove()"

            var emptyMask = Vector128.Equals(source, _emptyBucketVector).ExtractMostSignificantBits();
            // Check for empty buckets in the current vector.
            if (emptyMask != 0)
            {
                // Find the lowest set bit in `mask`, which represents the first matching position in the current probe group.
                // This identifies the first available slot or match within the group.
                var i = index + (uint)BitOperations.TrailingZeroCount(emptyMask);
                // Update the control byte at position `i` in `_controlBytes` to `h2`, marking it as occupied.
                // The control byte typically indicates the status of the slot (occupied, empty, or tombstone).
                Find(_controlBytes, i) = h2;
                // Access the entry in `_entries` at position `i` for insertion or update.
                ref var entry = ref Find(_entries, i);
                // Assign the specified `key` to the `Key` field of the located entry.
                entry.Key = key;
                // Assign the specified `value` to the `Value` field of the located entry.
                entry.Value = value;
                // Increment the total count of entries in the hash table, reflecting the new insertion.
                Count++;
                // Return immediately to indicate the insertion is complete.
                return;
            }

            // Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
            // So first we jump by 1 group (meaning we just continue our linear scan), then 2 groups (skipping over 1 group), then 3 groups (skipping over 2 groups), and so on.
            // Interestingly, this pattern perfectly lines up with our power-of-two size such that we will visit every single bucket exactly once without any repeats(searching is therefore guaranteed to terminate as we always have at least one EMPTY bucket).
            // Note: that our non-linear probing strategy makes us fairly robust against weird degenerate collision chains that can make us accidentally quadratic(Hash DoS).
            // Note: that we expect to almost never actually probe, since that’s WIDTH(16) non-EMPTY buckets we need to fail to find our key in.

            index = (index + (jumpDistance += 16)) & _lengthMinusOne;
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
    public unsafe bool Get(TKey key, out TValue value)
    {
        // Compute the hash code for the given key and cast it to an unsigned integer for bitwise operations.
        var hashcode = _hasher.ComputeHash(key);
        // Apply a secondary hash function to further spread the bits of the hashcode to reduce clustering.
        var h2 = H2(hashcode);
        // Create a 128-bit vector that holds the transformed hash code, which is used for comparisons.
        var target = Vector128.Create(h2);
        // This operation ensures that `index` is in the range [0, capacity - 1] by using only the lower bits of `hashcode`,
        // which helps in efficient and quick indexing.
        var index = hashcode & _lengthMinusOne;
        // Initialize a variable to keep track of the distance to jump when probing the map.

        byte jumpDistance = 0;

        // Loop until we find either a match or an empty slot.
        while (true)
        {
            // Load a vector from the control bytes starting at the computed index.
            // Control bytes hold metadata about the entries in the map.
            var source = ReadVector128(_controlBytes, index);
            // Compare the target vector (hashed key) with the loaded source vector to find matches.
            // `ExtractMostSignificantBits()` returns a mask where each bit set indicates a match.
            var mask = Vector128.Equals(target, source).ExtractMostSignificantBits();
            // Process any matches indicated by the mask.
            while (mask != 0)
            {
                // Get the position of the first set bit in the mask (indicating a match).
                var bitPos = BitOperations.TrailingZeroCount(mask);
                // Retrieve the entry corresponding to the matched bit position within the map's entries.
                var entry = Find(_entries, index + (uint)bitPos);
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
        // Note: GetValueRefOrAddDefault is mirrored from Emplace

        // Check if the table is full based on the maximum lookups before needing to resize.
        // Resize if the count exceeds the threshold.
        if (Count >= _maxLookupsBeforeResize)
        {
            Resize();
        }

        // Calculate the hash code for the key and cast it to uint for non-negative indexing.
        var hashcode = _hasher.ComputeHash(key);
        // Generate a secondary hash value 'h2' for probing, used to avoid clustering.
        var h2 = H2(hashcode);
        // Create a SIMD vector with the value of 'h2' for quick equality checks.
        var target = Vector128.Create(h2);
        // This operation ensures that `index` is in the range [0, capacity - 1] by using only the lower bits of `hashcode`,
        // which helps in efficient and quick indexing.
        var index = hashcode & _lengthMinusOne;
        // Initialize the probing jump distance to zero, which will increase with each probe iteration.
        byte jumpDistance = 0;

        while (true)
        {
            // Load a vector from the control bytes starting at the computed index.
            // Control bytes hold metadata about the entries in the map.
            var source = ReadVector128(_controlBytes, index);
            // Compare `source` and `target` vectors to find any positions with a matching control byte.
            var mask = Vector128.Equals(source, target).ExtractMostSignificantBits();
            // Loop over each set bit in `mask` (indicating matching positions).
            while (mask != 0)
            {
                // Find the lowest set bit in `mask` (first matching position).
                var bitPos = BitOperations.TrailingZeroCount(mask);
                // Use `bitPos` to access the corresponding entry in `_entries`.
                ref var entry = ref Find(_entries, index + (uint)bitPos);
                // If a matching key is found, update the entry's value and return the old value.
                if (_comparer.Equals(entry.Key, key))
                {
                    return ref entry.Value;
                }

                // Clear the lowest set bit in `mask` to continue with the next matching bit.
                mask = ResetLowestSetBit(mask);
            }

            // Note: we arent using tombstones, we just leave them for dead.
            // This means we are increasing the probe chain, but then again we dont expect there to be alot of tombstones
            // Probe sequences terminate at the first empty slot they encounter, having an empty slot in the group means that "removing" the current entry without placing a tombstone won’t disrupt probe chains.
            // More information at "Remove()"

            // Check for empty buckets in the current vector.
            mask = Vector128.Equals(source, _emptyBucketVector).ExtractMostSignificantBits();
            if (mask != 0)
            {
                // Find the lowest set bit in `mask`, which represents the first matching position.
                var bitPos = BitOperations.TrailingZeroCount(mask);
                // Convert `bitPos` to an unsigned integer and add it to `index` to get the absolute position `i`.
                // This calculates the offset within the group where the match or available slot is located.
                var i = index + (uint)bitPos;
                // Access the entry in `_entries` at the calculated position `i`.
                ref var entry = ref Find(_entries, i);
                // Set the key for the located entry to the specified `key`.
                entry.Key = key;
                // Set the control byte for the entry at position `i` to `h2` to mark it as occupied.
                Find(_controlBytes, i) = h2;
                // Increment the total count of entries in the hash table.
                Count++;

                // Return a reference to the `Value` field of the entry, allowing for direct access or assignment.
                return ref entry.Value;
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
        var hashcode = _hasher.ComputeHash(key);
        // Apply a secondary hash function to further spread out hashcode bits.
        var h2 = H2(hashcode);
        // Create a 128-bit vector from the transformed hash code to use as the target for comparison.
        var target = Vector128.Create(h2);
        // This operation ensures that `index` is in the range [0, capacity - 1] by using only the lower bits of `hashcode`,
        // which helps in efficient and quick indexing.
        var index = hashcode & _lengthMinusOne;
        // Initialize `jumpDistance` to control the distance between probes, starting at zero.
        byte jumpDistance = 0;

        // Loop until we either find the key to update or confirm it's absent.
        while (true)
        {
            // Load a vector from the control bytes starting at the computed index.
            // Control bytes hold metadata about the entries in the map.
            var source = ReadVector128(_controlBytes, index);

            // Compare the `source` vector with the `target` vector. `ExtractMostSignificantBits` produces a bit mask
            // where each set bit corresponds to a position that matches the target hash.
            var mask = Vector128.Equals(source, target).ExtractMostSignificantBits();

            // Process any matching entries indicated by bits set in `mask`.
            while (mask != 0)
            {
                // Get the position of the first set bit in `mask`, which indicates a possible match.
                var bitPos = BitOperations.TrailingZeroCount(mask);

                // Find the corresponding entry in `_entries` by calculating the exact position from `index` and `bitPos`.
                ref var entry = ref Find(_entries, index + (uint)bitPos);

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
        // We dont expect to trigger this. Use a resize sinds this will only happen when the table is nearly full
        if (_tombstoneCounter >= _maxTombstoneBeforeRehash)
        {
            Resize();
        }
        // Compute the hash code for the given key and cast it to an unsigned integer for bitwise operations.
        var hashcode = _hasher.ComputeHash(key);
        // Apply a secondary hash function to further spread the hash bits.
        var h2 = H2(hashcode);
        // Create a 128-bit vector from the transformed hash code to use as the target for comparison.
        var target = Vector128.Create(h2);
        // This operation ensures that `index` is in the range [0, capacity - 1] by using only the lower bits of `hashcode`,
        // which helps in efficient and quick indexing.
        var index = hashcode & _lengthMinusOne;
        // Initialize `jumpDistance` to control the distance between probes, starting at zero.
        byte jumpDistance = 0;

        // Begin probing until either the key is found and removed, or it's confirmed as absent.
        while (true)
        {
            // Load a vector from the control bytes starting at the computed index.
            // Control bytes hold metadata about the entries in the map.
            var source = ReadVector128(_controlBytes, index);

            // Compare `source` with `target`. `ExtractMostSignificantBits` returns a bitmask
            // where each set bit represents a potential match with `target`.
            var resultMask = Vector128.Equals(source, target).ExtractMostSignificantBits();
            var emptyMask = Vector128.Equals(_emptyBucketVector, source).ExtractMostSignificantBits();

            // Process each matching bit in `mask`.
            while (resultMask != 0)
            {
                // Get the position of the first set bit in `mask`, indicating a potential key match.
                var bitPos = BitOperations.TrailingZeroCount(resultMask);

                var i = index + (uint)bitPos;

                // Check if the entry at the matched position has a key that equals the specified key.
                // Use `_comparer` to ensure accurate key comparison.              
                if (_comparer.Equals(Find(_entries, i).Key, key))
                {
                    // If the group that contains the entry to be removed has an empty slot (an unoccupied slot that is marked empty rather than as a tombstone), this indicates that any probe sequence for a key would terminate upon reaching that empty slot.
                    // Since probe sequences terminate at the first empty slot they encounter, having an empty slot in the group means that removing the current entry without placing a tombstone won’t disrupt probe chains.
                    // Any probe sequence for another key that reaches this group would terminate on the existing empty slot before it could reach the removed slot.Therefore, there is no need to place a tombstone to preserve probe chain integrity.

                    // By skipping tombstone placement in this case, densemap reduces the need for tombstone management and avoids increasing the tombstone count, which would otherwise contribute to the rehash threshold.
                    // This keeps the table cleaner and minimizes the chance of triggering rehashes due to tombstone accumulation.

                    // Example:
                    // Imagine a group of 16 slots where a slot in the group is already empty. When removing an entry from this group, Densemap can remove the key without marking it as a tombstone because:
                    // 1. Any probe sequence from a different key that reaches this group would stop at the empty slot before hitting the removed slot.
                    // 2. This effectively preserves probe chain termination without needing the removed slot to act as a tombstone.

                    if (emptyMask > 0)
                    {
                        Find(_controlBytes, i) = _emptyBucket;
                    }
                    else
                    {
                        Find(_controlBytes, i) = _tombstone;
                        _tombstoneCounter++;
                    }

                    // Reset to default
                    Find(_entries, i) = default;
                    // Decrement the `Count` to reflect the removal of an entry.
                    --Count;

                    // Return `true` to indicate that the key was successfully removed.
                    return true;
                }

                // Clear the lowest set bit in `mask` to continue checking any remaining matches in the vector.
                resultMask = ResetLowestSetBit(resultMask);
            }

            // Check if `source` contains any empty buckets, which would indicate the end of the probe chain.
            // If an empty bucket is found, the key is not in the map.
            if (emptyMask != 0)
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
        var hashcode = _hasher.ComputeHash(key);
        // Apply a secondary hash function to further spread the bits of the hashcode.
        var h2 = H2(hashcode);
        // Create a 128-bit vector from the transformed hash code to use as the target for comparison.
        var target = Vector128.Create(h2);
        // This operation ensures that `index` is in the range [0, capacity - 1] by using only the lower bits of `hashcode`,
        // which helps in efficient and quick indexing.
        var index = hashcode & _lengthMinusOne;
        // Initialize `jumpDistance` to control the distance between probes, starting at zero.
        byte jumpDistance = 0;

        // Begin probing the hash map until the key is found or confirmed absent.
        while (true)
        {
            // Load a vector from the control bytes starting at the computed index.
            // Control bytes hold metadata about the entries in the map.
            var source = ReadVector128(_controlBytes, index);
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
                if (_comparer.Equals(Find(_entries, index + (uint)bitPos).Key, key))
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
        _controlBytes.AsSpan().Fill(_emptyBucket);
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
        _length <<= 1;
        _lengthMinusOne = _length - 1;
        _maxLookupsBeforeResize = _length * _loadFactor;

        _tombstoneCounter = 0;
        _maxTombstoneBeforeRehash = _length * 0.125;
        _groupMask = _lengthMinusOne & ~ElementsInGroupMinusOne;

        var oldEntries = _entries;
        var oldMetadata = _controlBytes;

        var size = Unsafe.As<uint, int>(ref _length);

        _controlBytes = GC.AllocateArray<sbyte>(size, true);
        _entries = GC.AllocateArray<Entry>(size);
        Array.Fill(_controlBytes, _emptyBucket);

        for (uint i = 0; i < oldEntries.Length; ++i)
        {
            var h2 = Find(oldMetadata, i);
            if (h2 < 0)
            {
                continue;
            }

            var entry = Find(oldEntries, i);

            var hashcode = _hasher.ComputeHash(entry.Key);
            var index = (hashcode & _lengthMinusOne) & ~ElementsInGroupMinusOne;
            byte jumpDistance = 0;

            while (true)
            {
                var mask = ReadVector128(_controlBytes, index).ExtractMostSignificantBits();
                if (mask != 0)
                {
                    var bitPos = BitOperations.TrailingZeroCount(mask);
                    index += (uint)bitPos;

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
    private static ref T Find<T>(T[] array, ulong index)
    {
        ref var arr0 = ref MemoryMarshal.GetArrayDataReference(array);
        return ref Unsafe.Add(ref arr0, Unsafe.As<ulong, nuint>(ref index));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref T Find<T>(T[] array, int index)
    {
        ref var arr0 = ref MemoryMarshal.GetArrayDataReference(array);
        return ref Unsafe.Add(ref arr0, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static Vector128<sbyte> ReadVector128(sbyte[] array, ulong index)
    {
        // Use `Unsafe.AsPointer` and pointer arithmetic for direct memory access
        // Calculate the pointer offset and load the vector directly
        ref var arr0 = ref MemoryMarshal.GetArrayDataReference(array);
        return Vector128.LoadAligned((sbyte*)Unsafe.AsPointer(ref arr0) + index);
    }

    /// <summary>
    /// Retrieves the 7 lowest bits from a hashcode.
    /// </summary>
    /// <param name="hashcode">The hashcode.</param>
    /// <returns>The 7 lowest bits of the hashcode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static sbyte H2(ulong hashcode) => (sbyte)(hashcode >> 57);

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
