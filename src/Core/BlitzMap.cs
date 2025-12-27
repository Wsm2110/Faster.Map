// Copyright (c) 2026, Wiljan Ruizendaal. All rights reserved. <wruizendaal@gmail.com> 
// Distributed under the MIT Software License, Version 1.0.

using Faster.Map.Contracts;
using Faster.Map.Hashing;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Get(TKey key, out TValue value)
    {
        uint hash = _hasher.ComputeHash(key);
        uint main = hash & _mask;
        uint sig = hash & ~_mask;

        ref var buckets = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref var entries = ref MemoryMarshal.GetArrayDataReference(_entries);

        // 1) Start at the home bucket
        ref var bucket = ref Unsafe.Add(ref buckets, main);

        // 2) Empty home bucket => not found
        uint slot = bucket.Signature;
        if (slot == 0u)
        {
            value = default!;
            return false;
        }

        // 3) Check the home entry (99% hit path)
        uint packed = slot - 1u;
        if ((packed & ~_mask) == sig)
        {
            uint index = packed & _mask;
            ref var entry = ref Unsafe.Add(ref entries, index);

            if (_hasher.Equals(key, entry.Key))
            {
                value = entry.Value;
                return true;
            }
        }

        // 4) Walk the collision chain (rare)
        uint next = bucket.Next;
        while (next != 0u)
        {
            bucket = ref Unsafe.Add(ref buckets, next - 1u);

            packed = bucket.Signature - 1u;
            if ((packed & ~_mask) == sig)
            {
                uint index = packed & _mask;
                ref var entry = ref Unsafe.Add(ref entries, index);

                if (_hasher.Equals(key, entry.Key))
                {
                    value = entry.Value;
                    return true;
                }
            }

            next = bucket.Next;
        }

        // 5) Reached end of chain
        value = default!;
        return false;
    }

    /// <summary>
    /// Retrieves the value associated with the specified key in the hash table. 
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <param name="value">The value associated with the key if found; otherwise, the default value.</param>
    /// <returns>True if the key exists in the hash table; false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(TKey key)
    {
        uint hash = _hasher.ComputeHash(key);
        uint main = hash & _mask;
        uint sig = hash & ~_mask;

        ref var buckets = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref var entries = ref MemoryMarshal.GetArrayDataReference(_entries);

        // 1) Start at the home bucket
        ref var bucket = ref Unsafe.Add(ref buckets, main);

        // 2) Empty home bucket => not present
        uint slot = bucket.Signature;
        if (slot == 0u)
            return false;

        // 3) Check home entry (99% case)
        uint packed = slot - 1u;
        if ((packed & ~_mask) == sig)
        {
            uint index = packed & _mask;
            if (_hasher.Equals(key, Unsafe.Add(ref entries, index).Key))
                return true;
        }

        // 4) Walk the chain (rare)
        uint next = bucket.Next;
        while (next != 0u)
        {
            bucket = ref Unsafe.Add(ref buckets, next - 1u);

            packed = bucket.Signature - 1u;
            if ((packed & ~_mask) == sig)
            {
                uint index = packed & _mask;
                if (_hasher.Equals(key, Unsafe.Add(ref entries, index).Key))
                    return true;
            }

            next = bucket.Next;
        }

        // 5) End of chain
        return false;
    }

    /// <summary>
    /// Update the value associated with the specified key in the hash table. 
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <param name="value">The value associated with the key if found; otherwise, the default value.</param>
    /// <returns>True if the key exists in the hash table; false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Update(TKey key, TValue value)
    {
        uint hash = _hasher.ComputeHash(key);
        uint main = hash & _mask;
        uint sig = hash & ~_mask;

        ref var buckets = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref var entries = ref MemoryMarshal.GetArrayDataReference(_entries);

        // 1) Go to the home bucket
        ref var bucket = ref Unsafe.Add(ref buckets, main);

        // 2) Empty home bucket => nothing to update
        uint slot = bucket.Signature;
        if (slot == 0u)
            return false;

        // 3) Check the home entry first (99% case)
        uint packed = slot - 1u;
        if ((packed & ~_mask) == sig)
        {
            uint index = packed & _mask;
            ref var entry = ref Unsafe.Add(ref entries, index);

            if (_hasher.Equals(key, entry.Key))
            {
                entry.Value = value;
                return true;
            }
        }

        // 4) Walk the chain (rare)
        uint next = bucket.Next;
        while (next != 0u)
        {
            bucket = ref Unsafe.Add(ref buckets, next - 1u);

            packed = bucket.Signature - 1u;
            if ((packed & ~_mask) == sig)
            {
                uint index = packed & _mask;
                ref var entry = ref Unsafe.Add(ref entries, index);

                if (_hasher.Equals(key, entry.Key))
                {
                    entry.Value = value;
                    return true;
                }
            }

            next = bucket.Next;
        }

        // 5) Reached end of chain
        return false;
    }

    /// <summary>
    /// Inserts a key-value pair into the hash table with high performance.
    /// </summary>
    /// <param name="key">The key to insert.</param>
    /// <param name="value">The value associated with the key.</param>
    /// <returns>True if the insertion is successful; false if the key already exists.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Insert(TKey key, TValue value)
    {
        if (_count == _maxCountBeforeResize)
            Resize();

        uint hash = _hasher.ComputeHash(key);
        uint main = hash & _mask;
        uint signature = hash & ~_mask;

        ref var buckets = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref var entries = ref MemoryMarshal.GetArrayDataReference(_entries);

        ref var bucket = ref Unsafe.Add(ref buckets, main);

        // Insert into empty home bucket.
        if (bucket.Signature == INACTIVE)
        {
            uint slot = _count++;
            Unsafe.Add(ref entries, slot) = new Entry(key, value);
            bucket.Signature = (signature | slot) + 1u;
            bucket.Next = INACTIVE;
            _lastBucket = main;
            return true;
        }

        uint packed = bucket.Signature - 1u;
        uint index = packed & _mask;

        // Evict foreign root and claim home bucket.
        uint owner = _hasher.ComputeHash(Unsafe.Add(ref entries, index).Key) & _mask;
        if (owner != main)
        {
            KickoutBucket(ref buckets, owner, main);

            uint slot = _count++;
            Unsafe.Add(ref entries, slot) = new Entry(key, value);
            bucket.Signature = (signature | slot) + 1u;
            bucket.Next = INACTIVE;
            _lastBucket = main;
            return true;
        }

        // Reject duplicate in root.
        if ((packed & ~_mask) == signature && _hasher.Equals(key, Unsafe.Add(ref entries, index).Key))
        {
            return false;
        }

        // Create first chain node.
        if (bucket.Next == INACTIVE)
        {
            uint n = FindEmptyBucket(ref buckets, main, 1);
            bucket.Next = n + 1u;

            uint slot = _count++;
            Unsafe.Add(ref entries, slot) = new Entry(key, value);

            ref var node = ref Unsafe.Add(ref buckets, n);
            node.Signature = (signature | slot) + 1u;
            node.Next = INACTIVE;

            _lastBucket = n;
            return true;
        }

        // Walk chain to find duplicate or tail.
        uint next = bucket.Next;
        while (true)
        {
            ref var node = ref Unsafe.Add(ref buckets, next - 1u);

            packed = node.Signature - 1u;
            if ((packed & ~_mask) == signature)
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

        // Append new chain node.
        uint newBucket = FindEmptyBucket(ref buckets, main, 1);
        bucket.Next = newBucket + 1u;

        uint newSlot = _count++;
        Unsafe.Add(ref entries, newSlot) = new Entry(key, value);

        ref var newNode = ref Unsafe.Add(ref buckets, newBucket);
        newNode.Signature = (signature | newSlot) + 1u;
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool InsertOrUpdate(TKey key, TValue value)
    {
        if (_count == _maxCountBeforeResize)
            Resize();

        uint hash = _hasher.ComputeHash(key);
        uint main = hash & _mask;
        uint signature = hash & ~_mask;

        ref var buckets = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref var entries = ref MemoryMarshal.GetArrayDataReference(_entries);

        ref var bucket = ref Unsafe.Add(ref buckets, main);

        // Insert into empty home bucket.
        if (bucket.Signature == INACTIVE)
        {
            uint slot = _count++;
            Unsafe.Add(ref entries, slot) = new Entry(key, value);
            bucket.Signature = (signature | slot) + 1u;
            bucket.Next = INACTIVE;
            _lastBucket = main;
            return true;
        }

        uint packed = bucket.Signature - 1u;
        uint index = packed & _mask;

        // Evict foreign root and claim home bucket.
        uint owner = _hasher.ComputeHash(Unsafe.Add(ref entries, index).Key) & _mask;
        if (owner != main)
        {
            KickoutBucket(ref buckets, owner, main);

            uint slot = _count++;
            Unsafe.Add(ref entries, slot) = new Entry(key, value);
            bucket.Signature = (signature | slot) + 1u;
            bucket.Next = INACTIVE;
            _lastBucket = main;
            return true;
        }

        // Update root if present.
        if ((packed & ~_mask) == signature)
        {
            ref var entry = ref Unsafe.Add(ref entries, index);
            if (_hasher.Equals(key, entry.Key))
            {
                entry.Value = value;
                return true;
            }
        }

        // Create first chain node.
        if (bucket.Next == INACTIVE)
        {
            uint n = FindEmptyBucket(ref buckets, main, 1);
            bucket.Next = n + 1u;

            uint slot = _count++;
            Unsafe.Add(ref entries, slot) = new Entry(key, value);

            ref var node = ref Unsafe.Add(ref buckets, n);
            node.Signature = (signature | slot) + 1u;
            node.Next = INACTIVE;

            _lastBucket = n;
            return true;
        }

        // Walk chain to update or reach tail.
        uint next = bucket.Next;
        while (true)
        {
            ref var node = ref Unsafe.Add(ref buckets, next - 1u);

            packed = node.Signature - 1u;
            if ((packed & ~_mask) == signature)
            {
                uint slot = packed & _mask;
                ref var entry = ref Unsafe.Add(ref entries, slot);
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

        // Append new chain node.
        uint newBucket = FindEmptyBucket(ref buckets, main, 1);
        bucket.Next = newBucket + 1u;

        uint newSlot = _count++;
        Unsafe.Add(ref entries, newSlot) = new Entry(key, value);

        ref var newNode = ref Unsafe.Add(ref buckets, newBucket);
        newNode.Signature = (signature | newSlot) + 1u;
        newNode.Next = INACTIVE;

        _lastBucket = newBucket;
        return true;
    }

    /// <summary>
    /// Removes the entry associated with the specified key from the hash table.
    /// </summary>
    /// <param name="key">The key of the entry to remove.</param>
    /// <returns>True if the key was found and removed; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey key)
    {
        uint hash = _hasher.ComputeHash(key);
        uint main = hash & _mask;
        uint signature = hash & ~_mask;

        ref var buckets = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref var entries = ref MemoryMarshal.GetArrayDataReference(_entries);

        ref var root = ref Unsafe.Add(ref buckets, main);

        // No entry at all.
        if (root.Signature == INACTIVE)
            return false;

        uint packed = root.Signature - 1u;
        uint slot = packed & _mask;

        // Root hit.
        if ((packed & ~_mask) == signature &&
            _hasher.Equals(key, Unsafe.Add(ref entries, slot).Key))
        {
            uint ebucket = EraseBucket(ref buckets, main, main);
            EraseSlot(ref buckets, ref entries, slot, ebucket);
            return true;
        }

        // Chain walk.
        uint next = root.Next;
        while (next != INACTIVE)
        {
            uint b = next - 1u;
            ref var node = ref Unsafe.Add(ref buckets, b);

            packed = node.Signature - 1u;
            slot = packed & _mask;

            if ((packed & ~_mask) == signature &&
                _hasher.Equals(key, Unsafe.Add(ref entries, slot).Key))
            {
                // Recompute physical bucket — erase may have root-copied earlier.
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InsertInternal(ref Bucket buckets, ref Entry entries, TKey key, TValue value)
    {
        uint hash = _hasher.ComputeHash(key);
        uint main = hash & _mask;
        uint signature = hash & ~_mask;

        ref var bucket = ref Unsafe.Add(ref buckets, main);

        // Empty home bucket: claim directly.
        if (bucket.Signature == INACTIVE)
        {
            uint slot = _count++;
            ref var entry = ref Unsafe.Add(ref entries, slot);
            entry.Key = key;
            entry.Value = value;

            bucket.Signature = (signature | slot) + 1u;
            bucket.Next = INACTIVE;
            _lastBucket = main;
            return;
        }

        uint packed = bucket.Signature - 1u;
        uint index = packed & _mask;

        // Foreign root: evict and immediately claim main.
        uint owner = _hasher.ComputeHash(Unsafe.Add(ref entries, index).Key) & _mask;
        if (owner != main)
        {
            KickoutBucket(ref buckets, owner, main);

            uint slot = _count++;
            ref var entry = ref Unsafe.Add(ref entries, slot);
            entry.Key = key;
            entry.Value = value;

            bucket.Signature = (signature | slot) + 1u;
            bucket.Next = INACTIVE;
            _lastBucket = main;
            return;
        }

        // No chain yet: create first chain node.
        if (bucket.Next == INACTIVE)
        {
            uint n = FindEmptyBucket(ref buckets, main, 1);
            bucket.Next = n + 1u;

            uint slot = _count++;
            ref var entry = ref Unsafe.Add(ref entries, slot);
            entry.Key = key;
            entry.Value = value;

            ref var node = ref Unsafe.Add(ref buckets, n);
            node.Signature = (signature | slot) + 1u;
            node.Next = INACTIVE;
            _lastBucket = n;
            return;
        }

        // Walk chain to tail.
        uint next = bucket.Next;
        while (true)
        {
            ref var node = ref Unsafe.Add(ref buckets, next - 1u);

            if (node.Next == INACTIVE)
            {
                bucket = ref node;
                break;
            }

            next = node.Next;
        }

        // Append at tail.
        uint newBucket = FindEmptyBucket(ref buckets, main, 1);
        bucket.Next = newBucket + 1u;

        uint newSlot = _count++;
        ref var newEntry = ref Unsafe.Add(ref entries, newSlot);
        newEntry.Key = key;
        newEntry.Value = value;

        ref var newNode = ref Unsafe.Add(ref buckets, newBucket);
        newNode.Signature = (signature | newSlot) + 1u;
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void KickoutBucket(ref Bucket buckets, uint index, uint bucket)
    {
        // The node currently in 'bucket' does not belong here.
        // We will move it to a free bucket and splice it back
        // into the chain that belongs to ownerMain.
        ref var victim = ref Unsafe.Add(ref buckets, bucket);
        uint next = victim.Next;
        uint sig = victim.Signature;

        // Find a free bucket near the displaced node to keep locality.
        uint newBucket = FindEmptyBucket(
            ref buckets,
            next == INACTIVE ? bucket : next - 1u,
            2);

        // Find the previous node in the owner's chain that points to this bucket.
        uint prev = FindPrevBucket(ref buckets, index, bucket);

        // Copy the displaced node into the new location.
        ref var dst = ref Unsafe.Add(ref buckets, newBucket);
        dst.Signature = sig;
        dst.Next = next;

        // Patch the owner's chain to point to the new location.
        Unsafe.Add(ref buckets, prev).Next = newBucket + 1u;

        // Clear the original bucket so it becomes empty.
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]  
    private uint FindEmptyBucket(ref Bucket buckets, uint index, uint cint)
    {
        // Fast check: try the next 2 buckets with wrapping.
        uint bucketIndex = (index + 1u) & _mask;
        if (Unsafe.Add(ref buckets, bucketIndex).Signature == INACTIVE)
        {
            return bucketIndex;
        }

        bucketIndex = (bucketIndex + 1u) & _mask;
        if (Unsafe.Add(ref buckets, bucketIndex).Signature == INACTIVE)
        {
            return bucketIndex;
        }

        // Quadratic-ish probing: (index + offset) with growing step.
        uint offset = 1u + cint;
        uint step = 3u;

        while (step < (uint)quadraticProbeLength)
        {
            bucketIndex = (index + offset) & _mask;
            if (Unsafe.Add(ref buckets, bucketIndex).Signature == INACTIVE)
            {
                return bucketIndex;
            }

            bucketIndex = (bucketIndex + 1u) & _mask;
            if (Unsafe.Add(ref buckets, bucketIndex).Signature == INACTIVE)
            {
                return bucketIndex;
            }

            offset += step;
            step++;
        }

        // Fallback: wrap linear scan using _last.
        while (true)
        {
            _last = (_last + 1u) & _mask;
            if (Unsafe.Add(ref buckets, _last).Signature == INACTIVE)
            {
                return _last;
            }

            _last = (_last + 1u) & _mask;
            if (Unsafe.Add(ref buckets, _last).Signature == INACTIVE)
            {
                return _last;
            }

            // Medium hop to reduce clustering.
            uint medium = (_numBuckets + _last) & _mask;
            if (Unsafe.Add(ref buckets, medium).Signature == INACTIVE)
            {
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
        uint next = Unsafe.Add(ref buckets, bucket).Next;

        // Remove root by pulling first chain node into root if it exists.
        if (bucket == main)
        {
            if (next == INACTIVE)
            {
                Unsafe.Add(ref buckets, main).Signature = INACTIVE;
                Unsafe.Add(ref buckets, main).Next = INACTIVE;
                return main;
            }

            uint nb = next - 1u;
            ref var src = ref Unsafe.Add(ref buckets, nb);

            Unsafe.Add(ref buckets, main).Signature = src.Signature;
            Unsafe.Add(ref buckets, main).Next = src.Next;

            src.Signature = INACTIVE;
            src.Next = INACTIVE;

            return nb;
        }

        // Remove a chain node by unlinking it from its predecessor.
        uint prev = FindPrevBucket(ref buckets, main, bucket);
        Unsafe.Add(ref buckets, prev).Next = next;

        Unsafe.Add(ref buckets, bucket).Signature = INACTIVE;
        Unsafe.Add(ref buckets, bucket).Next = INACTIVE;

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