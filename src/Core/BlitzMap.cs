// Copyright (c) 2026, Wiljan Ruizendaal. All rights reserved. <wruizendaal@gmail.com> 
// Distributed under the MIT Software License, Version 1.0.

using Faster.Map.Contracts;
using Faster.Map.Hashing;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;

namespace Faster.Map.Core;

/// <summary>
/// A specialized implementation of <see cref="BlitzMap{TKey, TValue, THasher}"/> that
/// simplifies usage by defaulting the hasher to <see cref="DefaultHasher{TKey}"/>.
/// This avoids requiring three generic type parameters when the user doesn't need 
/// a custom hashing function.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the map.</typeparam>
/// <typeparam name="TValue">The type of the values in the map.</typeparam>
public class BlitzMap<TKey, TValue> : BlitzMap<TKey, TValue, DefaultHasher<TKey>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlitzMap{TKey, TValue}"/> class
    /// with a default initial capacity of 2 and a load factor of 0.8.
    /// </summary>
    public BlitzMap() : base(2, 0.8) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlitzMap{TKey, TValue}"/> class
    /// with the specified initial capacity and a default load factor of 0.8.
    /// </summary>
    /// <param name="length">The initial capacity of the map.</param>
    public BlitzMap(int length) : base(length, 0.8) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlitzMap{TKey, TValue}"/> class
    /// with the specified initial capacity and load factor.
    /// </summary>
    /// <param name="length">The initial capacity of the map.</param>
    /// <param name="loadfactor">The maximum allowed load factor before resizing.</param>
    public BlitzMap(int length, double loadfactor) : base(length, loadfactor) { }
}

/// <summary>
/// A high-performance hash map implementation using a customizable hashing strategy.
/// By utilizing a struct-based hasher, this avoids interface dispatching and enables inlining,
/// resulting in reduced virtual call overhead and better CPU efficiency.
/// </summary>
/// <typeparam name="TKey">The type of the keys stored in the map.</typeparam>
/// <typeparam name="TValue">The type of the values stored in the map.</typeparam>
/// <typeparam name="THasher">
/// A struct implementing <see cref="IHasher{TKey}"/> to provide an optimized hashing function.
/// Using a struct-based hasher avoids virtual method calls and allows aggressive inlining.
/// </typeparam>
public class BlitzMap<TKey, TValue, THasher> where THasher : struct, IHasher<TKey>
{
    #region Properties

    /// <summary>
    /// Gets the number of key-value pairs currently stored in the map.
    /// </summary>
    /// <remarks>
    /// This returns the count as an <see cref="int"/> but internally stores
    /// it as a <see cref="uint"/> to optimize for performance.
    /// </remarks>
    public int Count => (int)_count;

    /// <summary>
    /// Gets the current capacity (number of buckets) allocated in the map.
    /// </summary>
    /// <remarks>
    /// The size represents the total number of slots available for key-value storage.
    /// This can be larger than <see cref="Count"/> due to empty or removed entries.
    /// </remarks>
    public int Size => _length;

    #endregion

    #region Enumerable

    /// <summary>
    /// Fallback for `foreach` compatibility.   
    /// Example:
    /// <code>
    /// foreach (ref readonly var entry in map)
    /// {
    /// 
    /// }
    /// </code>   
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanEnumerator GetEnumerator()
    {
        return new SpanEnumerator(
            MemoryMarshal.CreateReadOnlySpan(
                ref MemoryMarshal.GetArrayDataReference(_entries),
                (int)_count));
    }

    #endregion

    #region Fields

    private Bucket[] _buckets;
    private Entry[] _entries;
    private uint _numBuckets;
    private uint _count;
    private uint _mask;
    private uint _last;
    private const byte quadraticProbeLength = 6;
    private static readonly byte INACTIVE = 0;
    private int _length;
    private double _loadFactor;
    private uint _maxCountBeforeResize;
    private THasher _hasher;
    private uint _lastBucket = INACTIVE;

    #endregion
    /// <summary>
    /// Initializes a new instance of the <see cref="BlitzMap{TKey, TValue, THasher}"/> class
    /// with a default initial capacity and load factor.
    /// </summary>
    /// <remarks>
    /// Uses a small initial capacity (2) to minimize memory usage while still being able
    /// to grow dynamically. The default load factor (0.8) balances memory efficiency
    /// and performance.
    /// </remarks>
    public BlitzMap() : this(2, 0.8) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlitzMap{TKey, TValue, THasher}"/> class
    /// with the specified initial capacity and a default load factor of 0.8.
    /// </summary>
    /// <param name="length">The initial capacity of the map.</param>
    /// <remarks>
    /// The capacity determines the number of buckets allocated initially.
    /// A sensible default load factor of 0.8 ensures a balance between memory usage
    /// and performance before resizing is required.
    /// </remarks>
    public BlitzMap(int length) : this(length, 0.8) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlitzMap{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="length">The initial size of the hash table.</param>
    /// <param name="loadFactor">The load factor to control resizing behavior.</param>
    public BlitzMap(int length, double loadFactor)
    {
        // Validate load factor.
        if (loadFactor <= 0.0 || loadFactor >= 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(loadFactor), "Load factor must be > 0.0 and < 1.0");
        }

        // Clamp load factor.
        if (loadFactor > 0.9)
        {
            loadFactor = 0.9;
        }

        // Round capacity to power-of-two (minimum 2).
        uint cap = (uint)length;
        if (cap < 2u)
        {
            cap = 2u;
        }

        cap = BitOperations.RoundUpToPowerOf2(cap);

        _length = (int)cap;
        _mask = cap - 1u;
        _loadFactor = loadFactor;

        // Allocate buckets and zero them (Signature/Next must start as INACTIVE=0).
        _buckets = GC.AllocateUninitializedArray<Bucket>(_length);
        ref var bucketBase = ref MemoryMarshal.GetArrayDataReference(_buckets);
        Unsafe.InitBlockUnaligned(
            ref Unsafe.As<Bucket, byte>(ref bucketBase),
            0,
            (uint)(_length * Unsafe.SizeOf<Bucket>()));

        // Allocate entries; size is ceil(cap * loadFactor) using a fast integer ceil.
        uint entryCap = (uint)(cap * loadFactor);
        if (entryCap < cap * loadFactor)
        {
            entryCap++;
        }

        _entries = GC.AllocateUninitializedArray<Entry>((int)entryCap);

        _numBuckets = cap >> 1;
        _maxCountBeforeResize = (uint)(cap * loadFactor);
        _hasher = default;
        _count = 0;
        _last = 0;
        _lastBucket = INACTIVE;
    }

    #region Public Methods

    /// <summary>
    /// Retrieves the value associated with the specified key in the hash table. 
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <param name="value">The value associated with the key if found; otherwise, the default value.</param>
    /// <returns>True if the key exists in the hash table; false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Get(TKey key, out TValue value)
    {
        uint hash = _hasher.ComputeHash(key);
        uint main = hash & _mask;
        uint sig = hash & ~_mask;

        // Cache base references to permanently eliminate bounds checks.
        ref Bucket buckets = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref Entry entries = ref MemoryMarshal.GetArrayDataReference(_entries);

        ref Bucket bucket = ref Unsafe.Add(ref buckets, main);

        // Signature==0 is the empty sentinel. Raw read avoids struct field barriers.
        uint slot = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<Bucket, byte>(ref bucket));
        if (slot == 0)
        { 
            goto NotFound;
        }

        // Packed layout: [ signature | entryIndex ], stored as +1 to preserve zero.
        uint packed = slot - 1;

        // Compare only high bits; low bits hold entry index and are masked out.
        if ((packed & ~_mask) == sig)
        {
            uint index = packed & _mask;
            ref Entry entry = ref Unsafe.Add(ref entries, index);
            if (_hasher.Equals(key, entry.Key))
            {
                value = entry.Value;
                return true;
            }
        }

        uint next = bucket.Next;
        while (next != 0)
        {
            // Buckets reference entries using 1-based indices.
            bucket = ref Unsafe.Add(ref buckets, next - 1);

            // Packed as (signature | entryIndex) + 1; subtract to recover original layout and keep 0 as empty sentinel.
            packed = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<Bucket, byte>(ref bucket)) - 1;

            if ((packed & ~_mask) == sig)
            {
                uint index = packed & _mask;
                ref Entry entry = ref Unsafe.Add(ref entries, index);

                if (_hasher.Equals(key, entry.Key))
                {
                    value = entry.Value;
                    return true;
                }
            }

            // Prefetch only while table remains cache-resident.
            if (_mask <= (1u << 20))
            {
                _ = Unsafe.Add(ref buckets, (next + 4) & _mask);
            }

            next = bucket.Next;
        }

        NotFound:
        value = default!;
        return false;
    }

    /// <summary>
    /// Determines whether the specified key exists in the map.
    ///
    /// The lookup is optimized for the hot path by:
    /// - splitting the hash into a bucket index and a high-bit signature,
    /// - using raw unaligned loads to avoid struct field barriers,
    /// - walking the collision chain with pointer arithmetic only,
    /// - avoiding bounds checks by caching base array references,
    /// - issuing size-guarded prefetches for cache-resident tables.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns><c>true</c> if the key exists; otherwise <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Contains(TKey key)
    {
        uint hash = _hasher.ComputeHash(key);
        uint main = hash & _mask;
        uint sig = hash & ~_mask;

        // Cache base references to permanently remove bounds checks.
        ref Bucket buckets = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref Entry entries = ref MemoryMarshal.GetArrayDataReference(_entries);

        ref Bucket bucket = ref Unsafe.Add(ref buckets, main);

        // Signature == 0 is the empty sentinel; raw read emits a single MOV.
        uint slot = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<Bucket, byte>(ref bucket));
        if (slot == 0)
            return false;

        // Packed as (signature | entryIndex) + 1 to preserve 0 as empty.
        uint packed = slot - 1;

        // Compare only the high bits; low bits encode the entry index.
        if ((packed & ~_mask) == sig)
        {
            uint index = packed & _mask;
            ref Entry entry = ref Unsafe.Add(ref entries, index);

            if (_hasher.Equals(key, entry.Key))
                return true;
        }

        uint next = bucket.Next;
        while (next != 0)
        {
            // Buckets use 1-based indices to keep 0 as the sentinel value.
            bucket = ref Unsafe.Add(ref buckets, next - 1);

            packed = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<Bucket, byte>(ref bucket)) - 1;
            if ((packed & ~_mask) == sig)
            {
                uint index = packed & _mask;
                ref Entry entry = ref Unsafe.Add(ref entries, index);

                if (_hasher.Equals(key, entry.Key))
                    return true;
            }

            // Prefetch only while the table is small enough to stay cache-resident.
            if (_mask <= (1u << 20))
                _ = Unsafe.Add(ref buckets, (next + 4) & _mask);

            next = bucket.Next;
        }

        return false;
    }

    /// <summary>
    /// Update the value associated with the specified key in the hash table. 
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <param name="value">The value associated with the key if found; otherwise, the default value.</param>
    /// <returns>True if the key exists in the hash table; false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Update(TKey key, TValue value)
    {
        uint hash = _hasher.ComputeHash(key);
        uint main = hash & _mask;
        uint sig = hash & ~_mask;

        // Cache base references to permanently eliminate bounds checks.
        ref Bucket buckets = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref Entry entries = ref MemoryMarshal.GetArrayDataReference(_entries);

        ref Bucket bucket = ref Unsafe.Add(ref buckets, main);

        // Signature==0 is the empty sentinel. Raw read avoids struct field barriers.
        uint slot = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<Bucket, byte>(ref bucket));
        if (slot == 0)
            return false;

        // Packed as (signature | entryIndex) + 1.
        uint packed = slot - 1;

        // Compare only high bits; low bits hold entry index and are masked out.
        if ((packed & ~_mask) == sig)
        {
            uint index = packed & _mask;
            ref Entry entry = ref Unsafe.Add(ref entries, index);

            if (_hasher.Equals(key, entry.Key))
            {
                entry.Value = value;
                return true;
            }
        }

        uint next = bucket.Next;
        while (next != 0)
        {
            // Buckets reference entries using 1-based indices.
            bucket = ref Unsafe.Add(ref buckets, next - 1);

            packed = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<Bucket, byte>(ref bucket)) - 1;
            if ((packed & ~_mask) == sig)
            {
                uint index = packed & _mask;
                ref Entry entry = ref Unsafe.Add(ref entries, index);

                if (_hasher.Equals(key, entry.Key))
                {
                    entry.Value = value;
                    return true;
                }
            }

            // Prefetch only while the table remains cache-resident.
            if (_mask <= (1u << 20))
                _ = Unsafe.Add(ref buckets, (next + 4) & _mask);

            next = bucket.Next;
        }

        return false;
    }

    /// <summary>
    /// Inserts a key-value pair into the hash table with high performance.
    /// </summary>
    /// <param name="key">The key to insert.</param>
    /// <param name="value">The value associated with the key.</param>
    /// <returns>True if the insertion is successful; false if the key already exists.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Insert(TKey key, TValue value)
    {
        if (_count == _maxCountBeforeResize)
            Resize();

        uint hash = _hasher.ComputeHash(key);
        uint main = hash & _mask;
        uint sig = hash & ~_mask;

        // Cache base refs to remove bounds checks
        ref Bucket buckets = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref Entry entries = ref MemoryMarshal.GetArrayDataReference(_entries);

        ref Bucket bucket = ref Unsafe.Add(ref buckets, main);

        // Fast empty-home insert
        uint slotSig = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<Bucket, byte>(ref bucket));
        if (slotSig == INACTIVE)
        {
            uint slot = _count++;
            Unsafe.Add(ref entries, slot) = new Entry(key, value);

            bucket.Signature = (sig | slot) + 1u;
            bucket.Next = INACTIVE;
            _lastBucket = main;
            return true;
        }

        uint packed = slotSig - 1u;
        uint index = packed & _mask;

        // Evict foreign root
        uint owner = _hasher.ComputeHash(Unsafe.Add(ref entries, index).Key) & _mask;
        if (owner != main)
        {
            KickoutBucket(ref buckets, owner, main);

            uint slot = _count++;
            Unsafe.Add(ref entries, slot) = new Entry(key, value);

            bucket.Signature = (sig | slot) + 1u;
            bucket.Next = INACTIVE;
            _lastBucket = main;
            return true;
        }

        // Reject duplicate in root
        if ((packed & ~_mask) == sig && _hasher.Equals(key, Unsafe.Add(ref entries, index).Key))
            return false;

        // First chain node
        if (bucket.Next == INACTIVE)
        {
            uint n = FindEmptyBucket(ref buckets, main, 1);
            bucket.Next = n + 1u;

            uint slot = _count++;
            Unsafe.Add(ref entries, slot) = new Entry(key, value);

            ref Bucket node = ref Unsafe.Add(ref buckets, n);
            node.Signature = (sig | slot) + 1u;
            node.Next = INACTIVE;

            _lastBucket = n;
            return true;
        }

        // Walk chain
        uint next = bucket.Next;
        while (true)
        {
            ref Bucket node = ref Unsafe.Add(ref buckets, next - 1u);

            packed = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<Bucket, byte>(ref node)) - 1u;
            if ((packed & ~_mask) == sig)
            {
                uint slot = packed & _mask;
                if (_hasher.Equals(key, Unsafe.Add(ref entries, slot).Key))
                    return false;
            }

            if (node.Next == INACTIVE)
            {
                bucket = ref node;
                break;
            }

            next = node.Next;
        }

        // Append new node
        uint newBucket = FindEmptyBucket(ref buckets, main, 1);
        bucket.Next = newBucket + 1u;

        uint newSlot = _count++;
        Unsafe.Add(ref entries, newSlot) = new Entry(key, value);

        ref Bucket newNode = ref Unsafe.Add(ref buckets, newBucket);
        newNode.Signature = (sig | newSlot) + 1u;
        newNode.Next = INACTIVE;

        _lastBucket = newBucket;
        return true;
    }

    /// <summary>
    /// Inserts a new key-value pair into the hash table or updates the value if the key already exists.
    /// This method handles collision resolution and dynamically resizes the hash table when needed.
    /// </summary>
    /// <param name="key">The key to insert or update in the hash table.</param>
    /// <param name="value">The value associated with the key.</param>
    /// <returns>True if the insertion or update is successful.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool InsertOrUpdate(TKey key, TValue value)
    {
        if (_count == _maxCountBeforeResize)
            Resize();

        uint hash = _hasher.ComputeHash(key);
        uint main = hash & _mask;
        uint sig = hash & ~_mask;

        // Cache base references to eliminate bounds checks.
        ref Bucket buckets = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref Entry entries = ref MemoryMarshal.GetArrayDataReference(_entries);

        ref Bucket bucket = ref Unsafe.Add(ref buckets, main);

        // Fast empty-root insert.
        uint slotSig = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<Bucket, byte>(ref bucket));
        if (slotSig == INACTIVE)
        {
            uint slot = _count++;
            Unsafe.Add(ref entries, slot) = new Entry(key, value);

            bucket.Signature = (sig | slot) + 1u;
            bucket.Next = INACTIVE;
            _lastBucket = main;
            return true;
        }

        uint packed = slotSig - 1u;
        uint index = packed & _mask;

        // Evict foreign root.
        uint owner = _hasher.ComputeHash(Unsafe.Add(ref entries, index).Key) & _mask;
        if (owner != main)
        {
            KickoutBucket(ref buckets, owner, main);

            uint slot = _count++;
            Unsafe.Add(ref entries, slot) = new Entry(key, value);

            bucket.Signature = (sig | slot) + 1u;
            bucket.Next = INACTIVE;
            _lastBucket = main;
            return true;
        }

        // Update root if present.
        if ((packed & ~_mask) == sig)
        {
            ref Entry entry = ref Unsafe.Add(ref entries, index);
            if (_hasher.Equals(key, entry.Key))
            {
                entry.Value = value;
                return true;
            }
        }

        // First chain node.
        if (bucket.Next == INACTIVE)
        {
            uint n = FindEmptyBucket(ref buckets, main, 1);
            bucket.Next = n + 1u;

            uint slot = _count++;
            Unsafe.Add(ref entries, slot) = new Entry(key, value);

            ref Bucket node = ref Unsafe.Add(ref buckets, n);
            node.Signature = (sig | slot) + 1u;
            node.Next = INACTIVE;

            _lastBucket = n;
            return true;
        }

        // Walk chain.
        uint next = bucket.Next;
        while (true)
        {
            ref Bucket node = ref Unsafe.Add(ref buckets, next - 1u);

            packed = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<Bucket, byte>(ref node)) - 1u;
            if ((packed & ~_mask) == sig)
            {
                uint slot = packed & _mask;
                ref Entry entry = ref Unsafe.Add(ref entries, slot);
                if (_hasher.Equals(key, entry.Key))
                {
                    entry.Value = value;
                    return true;
                }
            }

            if (node.Next == INACTIVE)
            {
                bucket = ref node;
                break;
            }

            next = node.Next;
        }

        // Append new node.
        uint newBucket = FindEmptyBucket(ref buckets, main, 1);
        bucket.Next = newBucket + 1u;

        uint newSlot = _count++;
        Unsafe.Add(ref entries, newSlot) = new Entry(key, value);

        ref Bucket newNode = ref Unsafe.Add(ref buckets, newBucket);
        newNode.Signature = (sig | newSlot) + 1u;
        newNode.Next = INACTIVE;

        _lastBucket = newBucket;
        return true;
    }

    /// <summary>
    /// Removes the entry associated with the specified key from the hash table.
    /// </summary>
    /// <param name="key">The key of the entry to remove.</param>
    /// <returns>True if the key was found and removed; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Remove(TKey key)
    {
        uint hash = _hasher.ComputeHash(key);
        uint main = hash & _mask;
        uint sig = hash & ~_mask;

        // Cache base refs to permanently eliminate bounds checks.
        ref Bucket buckets = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref Entry entries = ref MemoryMarshal.GetArrayDataReference(_entries);

        ref Bucket root = ref Unsafe.Add(ref buckets, main);

        // Raw signature load: single MOV instead of struct field access.
        uint slotSig = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<Bucket, byte>(ref root));
        if (slotSig == INACTIVE)
        {
            return false;
        }

        uint packed = slotSig - 1u;
        uint slot = packed & _mask;

        // Root hit.
        if ((packed & ~_mask) == sig &&
            _hasher.Equals(key, Unsafe.Add(ref entries, slot).Key))
        {
            uint ebucket = EraseBucket(ref buckets, main, main);
            EraseSlot(ref buckets, ref entries, slot, ebucket);
            return true;
        }

        uint next = root.Next;
        while (next != 0)
        {
            uint b = next - 1u;
            ref Bucket node = ref Unsafe.Add(ref buckets, b);

            packed = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<Bucket, byte>(ref node)) - 1u;
            slot = packed & _mask;

            if ((packed & ~_mask) == sig &&
                _hasher.Equals(key, Unsafe.Add(ref entries, slot).Key))
            {
                // Physical bucket may differ after previous root-copy operations.
                uint realBucket = SigToBucket(ref buckets, ref entries, slot);

                uint ebucket = EraseBucket(ref buckets, realBucket, main);
                EraseSlot(ref buckets, ref entries, slot, ebucket);
                return true;
            }

            next = node.Next;
        }

        return false;
    }


    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose value to get or set.</param>
    /// <returns>The value associated with the specified key.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the specified key does not exist in the collection.</exception>
    public TValue this[TKey key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (Get(key, out var result))
            {
                return result;
            }

            throw new KeyNotFoundException($"Unable to find entry - {key.GetType().FullName} key - {key.GetHashCode()}");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (!InsertOrUpdate(key, value))
            {
                throw new KeyNotFoundException($"Unable to find entry - {key.GetType().FullName} key - {key.GetHashCode()}");
            }
        }
    }

    /// <summary>
    /// Copies all key-value pairs from the specified map into the current map.
    /// </summary>
    /// <remarks>Existing entries with the same keys in the current map will be overwritten by values from the
    /// source map. The operation does not clear the current map before copying.</remarks>
    /// <param name="other">The source map whose entries will be copied to this map. Cannot be null.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Copy(BlitzMap<TKey, TValue> other)
    {
        foreach (var item in other)
        {
            Insert(item.Key, item.Value);
        }
    }

    /// <summary>
    /// Removes all entries from the collection, resetting it to its initial state.
    /// </summary>
    /// <remarks>After calling this method, the collection will be empty and its capacity may be reset or
    /// reduced. Any references to previously stored entries will be cleared. This method is not thread-safe; ensure
    /// that no other operations are performed on the collection concurrently.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        // Clear only the live entries so GC can drop references (skip if Entry is blittable).
        if (RuntimeHelpers.IsReferenceOrContainsReferences<Entry>() && _count != 0)
            Array.Clear(_entries, 0, (int)_count);

        // Zero all buckets (Signature=0, Next=0) in one tight memclr.
        ref var bucketBase = ref MemoryMarshal.GetArrayDataReference(_buckets);
        Unsafe.InitBlockUnaligned(
            ref Unsafe.As<Bucket, byte>(ref bucketBase),
            0,
            (uint)(_buckets.Length * Unsafe.SizeOf<Bucket>()));

        // Reset state.
        _count = 0;
        _last = 0;
        _lastBucket = INACTIVE;
        _numBuckets = (uint)_length;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Resizes the hash table by doubling its capacity and rehashing existing entries.
    /// This method is automatically called when the number of elements exceeds the load factor threshold.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Resize()
    {
        // Grow table geometry.
        _length <<= 1;
        _mask = (uint)_length - 1u;
        _maxCountBeforeResize = (uint)(_length * _loadFactor);

        _last = 0;
        _numBuckets = (uint)_length >> 1;
        _lastBucket = INACTIVE;

        // Snapshot old storage.
        var oldEntriesArr = _entries;
        uint oldCount = _count;

        // Allocate new storage.
        int bucketCount = _length;
        int entryCount = (int)(_length * _loadFactor);

        _entries = GC.AllocateUninitializedArray<Entry>(entryCount);
        _buckets = GC.AllocateUninitializedArray<Bucket>(bucketCount);

        // Buckets must start fully inactive (Signature=0, Next=0).
        ref var newBucketBase = ref MemoryMarshal.GetArrayDataReference(_buckets);
        Unsafe.InitBlockUnaligned(
            ref Unsafe.As<Bucket, byte>(ref newBucketBase),
            0,
            (uint)(bucketCount * Unsafe.SizeOf<Bucket>()));

        // Reinsert into the new table.
        ref var newEntryBase = ref MemoryMarshal.GetArrayDataReference(_entries);
        ref var oldEntryBase = ref MemoryMarshal.GetArrayDataReference(oldEntriesArr);

        _count = 0;

        for (uint i = 0; i < oldCount; i++)
        {
            ref var e = ref Unsafe.Add(ref oldEntryBase, i);
            InsertInternal(ref newBucketBase, ref newEntryBase, e.Key, e.Value);
        }
    }

    /// <summary>
    /// Inserts a key-value pair into the hash table during resizing or internal operations.
    /// This method handles collision resolution using quadratic probing and linked buckets.
    /// </summary>
    /// <param name="bucketBase">A reference to the base of the bucket array.</param>
    /// <param name="entryBase">A reference to the base of the entry array.</param>
    /// <param name="key">The key to insert into the hash table.</param>
    /// <param name="value">The value associated with the key.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void InsertInternal(ref Bucket buckets, ref Entry entries, TKey key, TValue value)
    {
        uint hash = _hasher.ComputeHash(key);
        uint main = hash & _mask;
        uint sig = hash & ~_mask;

        ref Bucket bucket = ref Unsafe.Add(ref buckets, main);

        // Raw signature load: single MOV instead of struct field access.
        uint slotSig = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<Bucket, byte>(ref bucket));

        // Fast empty-root insert.
        if (slotSig == INACTIVE)
        {
            uint slot = _count++;
            ref Entry entry = ref Unsafe.Add(ref entries, slot);
            entry.Key = key;
            entry.Value = value;

            bucket.Signature = (sig | slot) + 1u;
            bucket.Next = INACTIVE;
            _lastBucket = main;
            return;
        }

        uint packed = slotSig - 1u;
        uint index = packed & _mask;

        // Evict foreign root.
        uint owner = _hasher.ComputeHash(Unsafe.Add(ref entries, index).Key) & _mask;
        if (owner != main)
        {
            KickoutBucket(ref buckets, owner, main);

            uint slot = _count++;
            ref Entry entry = ref Unsafe.Add(ref entries, slot);
            entry.Key = key;
            entry.Value = value;

            bucket.Signature = (sig | slot) + 1u;
            bucket.Next = INACTIVE;
            _lastBucket = main;
            return;
        }

        // First chain node.
        if (bucket.Next == INACTIVE)
        {
            uint n = FindEmptyBucket(ref buckets, main, 1);
            bucket.Next = n + 1u;

            uint slot = _count++;
            ref Entry entry = ref Unsafe.Add(ref entries, slot);
            entry.Key = key;
            entry.Value = value;

            ref Bucket node = ref Unsafe.Add(ref buckets, n);
            node.Signature = (sig | slot) + 1u;
            node.Next = INACTIVE;

            _lastBucket = n;
            return;
        }

        // Walk chain to tail.
        uint next = bucket.Next;
        while (true)
        {
            ref Bucket node = ref Unsafe.Add(ref buckets, next - 1u);

            if (node.Next == INACTIVE)
            {
                bucket = ref node;
                break;
            }

            next = node.Next;
        }

        // Append new node.
        uint newBucket = FindEmptyBucket(ref buckets, main, 1);
        bucket.Next = newBucket + 1u;

        uint newSlot = _count++;
        ref Entry newEntry = ref Unsafe.Add(ref entries, newSlot);
        newEntry.Key = key;
        newEntry.Value = value;

        ref Bucket newNode = ref Unsafe.Add(ref buckets, newBucket);
        newNode.Signature = (sig | newSlot) + 1u;
        newNode.Next = INACTIVE;

        _lastBucket = newBucket;
    }

    /// <summary>
    /// Relocates the entry at the specified bucket to a new empty bucket, updating the owner chain accordingly.
    /// </summary>
    /// <remarks>This method is typically used during insertion or reorganization in hash-based data
    /// structures to resolve collisions by moving an existing entry to a new location. The owner chain is updated to
    /// maintain correct linkage after the relocation.</remarks>
    /// <param name="index">The index of the main bucket that owns the chain containing the bucket to be relocated.</param>
    /// <param name="bucket">The index of the bucket to be relocated (kicked out) to a new position.</param>
    /// <returns>The index of the bucket that was freed by the relocation.</returns> 
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void KickoutBucket(ref Bucket buckets, uint index, uint bucket)
    {
        // Node currently stored in 'bucket' belongs to another root.
        ref Bucket victim = ref Unsafe.Add(ref buckets, bucket);

        // Raw reads avoid struct field barriers.
        uint next = victim.Next;
        uint sig = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<Bucket, byte>(ref victim));

        // Find a nearby empty bucket to preserve cache locality.
        uint newBucket = FindEmptyBucket(
            ref buckets,
            next == INACTIVE ? bucket : next - 1u,
            2);

        // Find the previous bucket in the owner's chain that references this one.
        uint prev = FindPrevBucket(ref buckets, index, bucket);

        // Move displaced node to new location.
        ref Bucket dst = ref Unsafe.Add(ref buckets, newBucket);
        dst.Signature = sig;
        dst.Next = next;

        // Patch owner's chain to point at the relocated node.
        Unsafe.Add(ref buckets, prev).Next = newBucket + 1u;

        // Clear original bucket so it becomes available.
        victim.Signature = INACTIVE;
        victim.Next = INACTIVE;
    }


    /// <summary>
    /// Finds the index of the bucket that directly precedes the specified target bucket in the linked list, starting
    /// from the given main bucket.
    /// </summary>
    /// <param name="main">The index of the bucket from which to begin the search.</param>
    /// <param name="target">The index of the target bucket whose predecessor is to be found.</param>
    /// <returns>The index of the bucket that directly precedes the target bucket in the linked list.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint FindPrevBucket(ref Bucket buckets, uint main, uint target)
    {
        uint cur = main;

        while (true)
        {
            uint next = Unsafe.Add(ref buckets, cur).Next;
#if DEBUG
            if (next == INACTIVE)
            {
                throw new InvalidOperationException("FindPrevBucket: target not in chain");
            }
#endif

            uint idx = next - 1u;
            if (idx == target)
            {
                return cur;
            }

            cur = idx;
        }
    }

    /// <summary>
    /// Finds an empty bucket in the hash table using quadratic probing and linear fallback.
    /// This method ensures the hash table maintains efficiency and avoids clustering.
    /// </summary>
    /// <param name="bucketBase">A reference to the base of the bucket array.</param>
    /// <param name="index">The initial index to start the search from.</param>
    /// <param name="cint">The current chain length to influence the search pattern.</param>
    /// <returns>The index of the empty bucket found using the probing strategy.</returns>  
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private uint FindEmptyBucket(ref Bucket buckets, uint index, uint cint)
    {
        // Cache base reference to remove bounds checks on Unsafe.Add.
        ref Bucket baseRef = ref buckets;
        uint baseIndex = index & _mask;

        // Probe the two most likely neighbours first – this catches short chains cheaply.
        uint bucket = baseIndex + 1;
        bucket &= _mask;

        ref Bucket slot = ref Unsafe.Add(ref baseRef, bucket);
        if (Unsafe.ReadUnaligned<uint>(ref Unsafe.As<Bucket, byte>(ref slot)) == 0)
        {
            return bucket; 
        }

        slot = ref Unsafe.Add(ref slot, 1);
        uint next = bucket + 1;
        next &= _mask;

        if (Unsafe.ReadUnaligned<uint>(ref Unsafe.As<Bucket, byte>(ref slot)) == 0)
            return next;

        // Triangular probing spreads probes while preserving locality around the root.
        uint n = 1;
        while (n < quadraticProbeLength)
        {
            uint t = (n * (n + 1)) >> 1;

            bucket = baseIndex + t + cint;
            bucket &= _mask;

            slot = ref Unsafe.Add(ref baseRef, bucket);
            if (Unsafe.ReadUnaligned<uint>(ref Unsafe.As<Bucket, byte>(ref slot)) == 0)
                return bucket;

            slot = ref Unsafe.Add(ref slot, 1);
            next = bucket + 1;
            next &= _mask;

            if (Unsafe.ReadUnaligned<uint>(ref Unsafe.As<Bucket, byte>(ref slot)) == 0)
            {
                return next;
            }

            // Touch a bucket ~1.5 cache lines ahead to overlap miss latency.
            _ = Unsafe.Add(ref baseRef, (bucket + 8) & _mask);

            n++;
        }

        // Fallback linear scan biased by the last successful insertion point.
        uint last = _last;

        while (true)
        {
            last++;
            last &= _mask;

            slot = ref Unsafe.Add(ref baseRef, last);
            if (Unsafe.ReadUnaligned<uint>(ref Unsafe.As<Bucket, byte>(ref slot)) == 0)
            {
                _last = last;
                return last;
            }

            last++;
            last &= _mask;

            slot = ref Unsafe.Add(ref baseRef, last);
            if (Unsafe.ReadUnaligned<uint>(ref Unsafe.As<Bucket, byte>(ref slot)) == 0)
            {
                _last = last;
                return last;
            }

            // Medium jump reduces primary clustering when table is near saturation.
            uint medium = last + _numBuckets;
            medium &= _mask;

            slot = ref Unsafe.Add(ref baseRef, medium);
            if (Unsafe.ReadUnaligned<uint>(ref Unsafe.As<Bucket, byte>(ref slot)) == 0)
            {
                _last = medium;
                return medium;
            }
        }
    }

    /// <summary>
    /// Removes the specified bucket or chain node from the bucket list and updates the structure accordingly.
    /// </summary>
    /// <remarks>If the specified bucket is the root of a chain, the method promotes the first chain node to
    /// root if available. Otherwise, it unlinks the chain node from its predecessor. The erased bucket's fields are set
    /// to inactive to indicate removal.</remarks>
    /// <param name="buckets">A reference to the collection of buckets to modify.</param>
    /// <param name="bucket">The index of the bucket or chain node to erase.</param>
    /// <param name="main">The index of the main bucket in the chain, used to determine root removal behavior.</param>
    /// <returns>The index of the bucket that was erased or updated. If the root bucket is erased and a chain node is promoted,
    /// returns the index of the promoted node; otherwise, returns the index of the erased bucket.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint EraseBucket(ref Bucket buckets, uint bucket, uint main)
    {
        ref Bucket victim = ref Unsafe.Add(ref buckets, bucket);
        uint next = victim.Next;

        // Remove root bucket
        if (bucket == main)
        {
            if (next == INACTIVE)
            {
                // mov dword ptr[bucket], 0; Signature
                // mov dword ptr[bucket + 4], 0; Next
                // vs
                // mov qword ptr [bucket], 0
                Unsafe.WriteUnaligned(ref Unsafe.As<Bucket, byte>(ref victim), default(Bucket));
                return main;
            }

            uint nb = next - 1u;
            ref Bucket src = ref Unsafe.Add(ref buckets, nb);

            // Move child into root
            victim = src;

            // Clear child bucket in one store
            Unsafe.WriteUnaligned(ref Unsafe.As<Bucket, byte>(ref src), default(Bucket));
            return nb;
        }

        // Remove chain node
        uint prev = FindPrevBucket(ref buckets, main, bucket);
        Unsafe.Add(ref buckets, prev).Next = next;

        // Clear victim in one store
        Unsafe.WriteUnaligned(
            ref Unsafe.As<Bucket, byte>(ref victim),
            default(Bucket));

        return bucket;
    }


    /// <summary>
    /// Removes the entry at the specified slot and compacts the entries to maintain contiguous storage.
    /// </summary>
    /// <remarks>After erasing the specified slot, the last entry is moved into the vacated position to avoid
    /// gaps. The bucket metadata is updated to reflect the new slot index. This method does not preserve the order of
    /// entries.</remarks>
    /// <param name="buckets">A reference to the collection of buckets that manage entry placement within the data structure.</param>
    /// <param name="entries">A reference to the collection of entries from which the specified slot will be erased.</param>
    /// <param name="slot">The zero-based index of the slot to erase. Must be less than the current entry count.</param>
    /// <param name="ebucket">The index of the bucket associated with the slot being erased.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EraseSlot(ref Bucket buckets, ref Entry entries, uint slot, uint ebucket)
    {
        uint lastSlot = --_count;

        // _lastBucket is a one-shot hint; clear it for all paths.
        uint hint = _lastBucket;
        _lastBucket = INACTIVE;

        // Removing the last slot needs no compaction.
        if (slot == lastSlot)
            return;

        // Move last entry into the freed slot.
        Unsafe.Add(ref entries, slot) = Unsafe.Add(ref entries, lastSlot);

        // Use the hint unless it was never set or the hinted bucket was just freed.
        uint lastBucket =
            (hint == INACTIVE || hint == ebucket)
                ? SigToBucket(ref buckets, ref entries, lastSlot)
                : hint;

        // Patch the bucket that used to reference lastSlot so it now references slot.
        ref var node = ref Unsafe.Add(ref buckets, lastBucket);
        uint sig = (node.Signature - 1u) & ~_mask;
        node.Signature = (sig | slot) + 1u;
    }

    /// <summary>
    /// Finds the index of the bucket that contains the specified slot within the hash table structure.
    /// </summary>
    /// <remarks>This method uses the hash of the entry's key to determine the main bucket and traverses the
    /// bucket chain if necessary to locate the correct bucket. The method is intended for internal use in hash table
    /// operations and assumes that the provided slot exists within the entries.</remarks>
    /// <param name="buckets">A reference to the collection of buckets used to organize entries in the hash table.</param>
    /// <param name="entries">A reference to the collection of entries stored in the hash table.</param>
    /// <param name="index">The slot index to locate within the buckets. Must be a valid index within the entries collection.</param>
    /// <returns>The index of the bucket that contains the specified slot.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint SigToBucket(ref Bucket buckets, ref Entry entries, uint index)
    {
        uint hash = _hasher.ComputeHash(Unsafe.Add(ref entries, index).Key);
        uint main = hash & _mask;

        ref var root = ref Unsafe.Add(ref buckets, main);

        // Slot is in root bucket.
        if (((root.Signature - 1u) & _mask) == index)
        {
            return main;
        }

        // Otherwise walk chain until the slot is found.
        uint next = root.Next;
        while (true)
        {
            uint b = next - 1u;
            if (((Unsafe.Add(ref buckets, b).Signature - 1u) & _mask) == index)
            {
                return b;
            }

            next = Unsafe.Add(ref buckets, b).Next;
        }
    }

    #endregion

    #region Structs

    /// <summary>
    /// Represents a key/value pair entry with a specified key and value.
    /// </summary>
    /// <param name="key">The key associated with the entry.</param>
    /// <param name="value">The value associated with the key.</param>
    [StructLayout(LayoutKind.Sequential)]
    public record struct Entry(TKey Key, TValue Value);

    /// <summary>
    /// Represents a bucket in the hash table, which holds the signature and the index of the next bucket.
    /// The bucket structure is used to manage collisions through linked lists within the hash table.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Bucket
    {
        public uint Signature;
        public uint Next;
    }

    /// <summary>
    /// Provides an enumerator for iterating over a read-only span of Entry values.
    /// </summary>
    /// <remarks>SpanEnumerator is a ref struct and can only be used on the stack. It enables efficient,
    /// allocation-free iteration over a ReadOnlySpan<Entry> using a pattern similar to foreach, but does not implement
    /// IEnumerator or IEnumerable.</remarks>
    public ref struct SpanEnumerator
    {
        private ReadOnlySpan<Entry> _span;
        private int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpanEnumerator(ReadOnlySpan<Entry> span)
        {
            _span = span;
            _index = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            int next = _index + 1;
            if (next < _span.Length)
            {
                _index = next;
                return true;
            }
            return false;
        }

        public ref readonly Entry Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _span[_index];
        }
    }

    #endregion
}