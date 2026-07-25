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
/// </summary>
/// <typeparam name="TKey">The type of the keys in the map.</typeparam>
/// <typeparam name="TValue">The type of the values in the map.</typeparam>
public class BlitzMap<TKey, TValue> : BlitzMap<TKey, TValue, DefaultHasher.Generic<TKey>>
{
    public BlitzMap() : base(2, 0.8) { }
    public BlitzMap(int length) : base(length, 0.8) { }
    public BlitzMap(int length, double loadfactor) : base(length, loadfactor) { }
}

/// <summary>
/// A high-performance hash map implementation utilizing raw Sentinel values to bypass ALU packing 
/// and struct-based hashing for zero virtual dispatch.
/// </summary>
public class BlitzMap<TKey, TValue, THasher> where THasher : struct, IHasher<TKey>
{
    #region Properties
    public int Count => (int)_count;
    public int Size => _length;
    #endregion

    #region Enumerable
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

    // Sentinel Shift: Eliminates +1 / -1 ALU arithmetic on hot paths.
    private const uint INACTIVE = 0xFFFFFFFF;

    private int _length;
    private double _loadFactor;
    private uint _maxCountBeforeResize;
    private THasher _hasher;
    #endregion

    public BlitzMap() : this(2, 0.8) { }
    public BlitzMap(int length) : this(length, 0.8) { }

    public BlitzMap(int length, double loadFactor)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Capacity cannot be negative");

        if (loadFactor <= 0.0 || loadFactor >= 1.0)
            throw new ArgumentOutOfRangeException(nameof(loadFactor), "Load factor must be > 0.0 and < 1.0");

        if (loadFactor > 0.9) loadFactor = 0.9;

        uint cap = (uint)length;
        if (cap < 2u) cap = 2u;
        cap = BitOperations.RoundUpToPowerOf2(cap);

        _length = (int)cap;
        _mask = cap - 1u;
        _loadFactor = loadFactor;

        _buckets = GC.AllocateUninitializedArray<Bucket>(_length);
        _buckets.AsSpan().Fill(new Bucket { Signature = INACTIVE, Next = INACTIVE });

        uint entryCap = (uint)(cap * loadFactor);
        if (entryCap < cap * loadFactor) entryCap++;

        _entries = GC.AllocateUninitializedArray<Entry>((int)entryCap);

        _numBuckets = cap >> 1;
        _maxCountBeforeResize = (uint)(cap * loadFactor);
        _hasher = default;
        _count = 0;
        _last = 0;
    }

    #region Public Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Get(TKey key, out TValue value)
    {
        uint mask = _mask;
        uint hash = _hasher.ComputeHash(key);
        uint sig = hash & ~mask;

        ref Bucket buckets = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref Entry entries = ref MemoryMarshal.GetArrayDataReference(_entries);
        ref Bucket root = ref Unsafe.Add(ref buckets, hash & mask);

        if (BitConverter.IsLittleEndian)
        {
            ulong data = Unsafe.As<Bucket, ulong>(ref root);
            uint rootPacked = (uint)data;
            if (rootPacked == INACTIVE) goto NotFound;

            if ((rootPacked & ~mask) == sig)
            {
                ref Entry entry = ref Unsafe.Add(ref entries, rootPacked & mask);
                if (_hasher.Equals(key, entry.Key))
                {
                    value = entry.Value;
                    return true;
                }
            }

            uint next = (uint)(data >> 32);
            if (next == INACTIVE) goto NotFound;

            while (true)
            {
                ref Bucket node = ref Unsafe.Add(ref buckets, next);
                ulong nodeData = Unsafe.As<Bucket, ulong>(ref node);
                uint nodePacked = (uint)nodeData;

                if ((nodePacked & ~mask) == sig)
                {
                    ref Entry entry = ref Unsafe.Add(ref entries, nodePacked & mask);
                    if (_hasher.Equals(key, entry.Key))
                    {
                        value = entry.Value;
                        return true;
                    }
                }
                next = (uint)(nodeData >> 32);
                if (next == INACTIVE) break;
            }
        }
        else
        {
            uint rootPacked = root.Signature;
            if (rootPacked == INACTIVE) goto NotFound;

            if ((rootPacked & ~mask) == sig)
            {
                ref Entry entry = ref Unsafe.Add(ref entries, rootPacked & mask);
                if (_hasher.Equals(key, entry.Key))
                {
                    value = entry.Value;
                    return true;
                }
            }

            uint next = root.Next;
            if (next == INACTIVE) goto NotFound;

            while (true)
            {
                ref Bucket node = ref Unsafe.Add(ref buckets, next);
                uint nodePacked = node.Signature;

                if ((nodePacked & ~mask) == sig)
                {
                    ref Entry entry = ref Unsafe.Add(ref entries, nodePacked & mask);
                    if (_hasher.Equals(key, entry.Key))
                    {
                        value = entry.Value;
                        return true;
                    }
                }
                next = node.Next;
                if (next == INACTIVE) break;
            }
        }

        NotFound:
        value = default!;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public ref TValue GetValueRefOrNullRef(TKey key)
    {
        uint mask = _mask;
        uint hash = _hasher.ComputeHash(key);
        uint sig = hash & ~mask;

        ref Bucket buckets = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref Entry entries = ref MemoryMarshal.GetArrayDataReference(_entries);
        ref Bucket root = ref Unsafe.Add(ref buckets, hash & mask);

        if (BitConverter.IsLittleEndian)
        {
            ulong data = Unsafe.As<Bucket, ulong>(ref root);
            uint rootPacked = (uint)data;
            if (rootPacked == INACTIVE) return ref Unsafe.NullRef<TValue>();

            if ((rootPacked & ~mask) == sig)
            {
                ref Entry entry = ref Unsafe.Add(ref entries, rootPacked & mask);
                if (_hasher.Equals(key, entry.Key))
                {
                    return ref entry.Value;
                }
            }

            uint next = (uint)(data >> 32);
            if (next == INACTIVE) return ref Unsafe.NullRef<TValue>();

            while (true)
            {
                ref Bucket node = ref Unsafe.Add(ref buckets, next);
                ulong nodeData = Unsafe.As<Bucket, ulong>(ref node);
                uint nodePacked = (uint)nodeData;

                if ((nodePacked & ~mask) == sig)
                {
                    ref Entry entry = ref Unsafe.Add(ref entries, nodePacked & mask);
                    if (_hasher.Equals(key, entry.Key))
                    {
                        return ref entry.Value;
                    }
                }
                next = (uint)(nodeData >> 32);
                if (next == INACTIVE) break;
            }
        }
        else
        {
            uint rootPacked = root.Signature;
            if (rootPacked == INACTIVE) return ref Unsafe.NullRef<TValue>();

            if ((rootPacked & ~mask) == sig)
            {
                ref Entry entry = ref Unsafe.Add(ref entries, rootPacked & mask);
                if (_hasher.Equals(key, entry.Key))
                {
                    return ref entry.Value;
                }
            }

            uint next = root.Next;
            if (next == INACTIVE) return ref Unsafe.NullRef<TValue>();

            while (true)
            {
                ref Bucket node = ref Unsafe.Add(ref buckets, next);
                uint nodePacked = node.Signature;

                if ((nodePacked & ~mask) == sig)
                {
                    ref Entry entry = ref Unsafe.Add(ref entries, nodePacked & mask);
                    if (_hasher.Equals(key, entry.Key))
                    {
                        return ref entry.Value;
                    }
                }
                next = node.Next;
                if (next == INACTIVE) break;
            }
        }
        return ref Unsafe.NullRef<TValue>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Contains(TKey key)
    {
        uint hash = _hasher.ComputeHash(key);
        uint main = hash & _mask;
        uint sig = hash & ~_mask;

        ref Bucket buckets = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref Entry entries = ref MemoryMarshal.GetArrayDataReference(_entries);
        ref Bucket root = ref Unsafe.Add(ref buckets, main);

        uint rootPacked = root.Signature;
        if (rootPacked == INACTIVE) return false;

        if ((rootPacked & ~_mask) == sig)
        {
            if (_hasher.Equals(key, Unsafe.Add(ref entries, rootPacked & _mask).Key))
                return true;
        }

        uint next = root.Next;
        while (next != INACTIVE)
        {
            ref Bucket node = ref Unsafe.Add(ref buckets, next);
            uint nodePacked = node.Signature;

            if ((nodePacked & ~_mask) == sig)
            {
                if (_hasher.Equals(key, Unsafe.Add(ref entries, nodePacked & _mask).Key))
                {
                    return true;
                }
            }
            next = node.Next;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Update(TKey key, TValue value)
    {
        uint hash = _hasher.ComputeHash(key);
        uint main = hash & _mask;
        uint sig = hash & ~_mask;

        ref Bucket buckets = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref Entry entries = ref MemoryMarshal.GetArrayDataReference(_entries);
        ref Bucket root = ref Unsafe.Add(ref buckets, main);

        uint rootPacked = root.Signature;
        if (rootPacked == INACTIVE) return false;

        if ((rootPacked & ~_mask) == sig)
        {
            ref Entry entry = ref Unsafe.Add(ref entries, rootPacked & _mask);
            if (_hasher.Equals(key, entry.Key))
            {
                entry.Value = value;
                return true;
            }
        }

        uint next = root.Next;
        while (next != INACTIVE)
        {
            ref Bucket node = ref Unsafe.Add(ref buckets, next);
            uint nodePacked = node.Signature;

            if ((nodePacked & ~_mask) == sig)
            {
                ref Entry entry = ref Unsafe.Add(ref entries, nodePacked & _mask);
                if (_hasher.Equals(key, entry.Key))
                {
                    entry.Value = value;
                    return true;
                }
            }
            next = node.Next;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public bool Insert(TKey key, TValue value)
    {
        if (_count == _maxCountBeforeResize) Resize();

        uint hash = _hasher.ComputeHash(key);
        uint main = hash & _mask;
        uint sig = hash & ~_mask;

        ref Bucket buckets = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref Entry entries = ref MemoryMarshal.GetArrayDataReference(_entries);
        ref Bucket bucket = ref Unsafe.Add(ref buckets, main);

        uint slotSig = bucket.Signature;
        if (slotSig == INACTIVE)
        {
            uint slot = _count++;
            Unsafe.Add(ref entries, slot) = new Entry(key, value);
            bucket.Signature = sig | slot;
            bucket.Next = INACTIVE;
            return true;
        }

        uint packed = slotSig;
        uint index = packed & _mask;
        uint owner = _hasher.ComputeHash(Unsafe.Add(ref entries, index).Key) & _mask;

        if (owner != main)
        {
            KickoutBucket(ref buckets, owner, main);
            uint slot = _count++;
            Unsafe.Add(ref entries, slot) = new Entry(key, value);
            bucket.Signature = sig | slot;
            bucket.Next = INACTIVE;
            return true;
        }

        if ((packed & ~_mask) == sig && _hasher.Equals(key, Unsafe.Add(ref entries, index).Key))
            return false;

        if (bucket.Next == INACTIVE)
        {
            uint n = FindEmptyBucket(ref buckets, main, 1);
            bucket.Next = n;
            uint slot = _count++;
            Unsafe.Add(ref entries, slot) = new Entry(key, value);

            ref Bucket node = ref Unsafe.Add(ref buckets, n);
            node.Signature = sig | slot;
            node.Next = INACTIVE;
            return true;
        }

        uint next = bucket.Next;
        while (true)
        {
            ref Bucket node = ref Unsafe.Add(ref buckets, next);
            packed = node.Signature;

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

        uint newBucket = FindEmptyBucket(ref buckets, main, 1);
        bucket.Next = newBucket;
        uint newSlot = _count++;
        Unsafe.Add(ref entries, newSlot) = new Entry(key, value);

        ref Bucket newNode = ref Unsafe.Add(ref buckets, newBucket);
        newNode.Signature = sig | newSlot;
        newNode.Next = INACTIVE;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public bool InsertOrUpdate(TKey key, TValue value)
    {
        if (_count == _maxCountBeforeResize) Resize();

        uint hash = _hasher.ComputeHash(key);
        uint main = hash & _mask;
        uint sig = hash & ~_mask;

        ref Bucket buckets = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref Entry entries = ref MemoryMarshal.GetArrayDataReference(_entries);
        ref Bucket bucket = ref Unsafe.Add(ref buckets, main);

        uint slotSig = bucket.Signature;
        if (slotSig == INACTIVE)
        {
            uint slot = _count++;
            Unsafe.Add(ref entries, slot) = new Entry(key, value);
            bucket.Signature = sig | slot;
            bucket.Next = INACTIVE;
            return true;
        }

        uint packed = slotSig;
        uint index = packed & _mask;
        uint owner = _hasher.ComputeHash(Unsafe.Add(ref entries, index).Key) & _mask;

        if (owner != main)
        {
            KickoutBucket(ref buckets, owner, main);
            uint slot = _count++;
            Unsafe.Add(ref entries, slot) = new Entry(key, value);
            bucket.Signature = sig | slot;
            bucket.Next = INACTIVE;
            return true;
        }

        if ((packed & ~_mask) == sig)
        {
            ref Entry entry = ref Unsafe.Add(ref entries, index);
            if (_hasher.Equals(key, entry.Key))
            {
                entry.Value = value;
                return true;
            }
        }

        if (bucket.Next == INACTIVE)
        {
            uint n = FindEmptyBucket(ref buckets, main, 1);
            bucket.Next = n;
            uint slot = _count++;
            Unsafe.Add(ref entries, slot) = new Entry(key, value);

            ref Bucket node = ref Unsafe.Add(ref buckets, n);
            node.Signature = sig | slot;
            node.Next = INACTIVE;
            return true;
        }

        uint next = bucket.Next;
        while (true)
        {
            ref Bucket node = ref Unsafe.Add(ref buckets, next);
            packed = node.Signature;

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

        uint newBucket = FindEmptyBucket(ref buckets, main, 1);
        bucket.Next = newBucket;
        uint newSlot = _count++;
        Unsafe.Add(ref entries, newSlot) = new Entry(key, value);

        ref Bucket newNode = ref Unsafe.Add(ref buckets, newBucket);
        newNode.Signature = sig | newSlot;
        newNode.Next = INACTIVE;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public bool Remove(TKey key)
    {
        uint hash = _hasher.ComputeHash(key);
        uint main = hash & _mask;
        uint sig = hash & ~_mask;

        ref Bucket buckets = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref Entry entries = ref MemoryMarshal.GetArrayDataReference(_entries);
        ref Bucket root = ref Unsafe.Add(ref buckets, main);

        uint slotSig = root.Signature;
        if (slotSig == INACTIVE) return false;

        uint packed = slotSig;
        uint slot = packed & _mask;

        if ((packed & ~_mask) == sig && _hasher.Equals(key, Unsafe.Add(ref entries, slot).Key))
        {
            EraseBucket(ref buckets, main, main);
            EraseSlot(ref buckets, ref entries, slot);
            return true;
        }

        uint next = root.Next;
        while (next != INACTIVE)
        {
            uint b = next;
            ref Bucket node = ref Unsafe.Add(ref buckets, b);

            packed = node.Signature;
            slot = packed & _mask;

            if ((packed & ~_mask) == sig && _hasher.Equals(key, Unsafe.Add(ref entries, slot).Key))
            {
                EraseBucket(ref buckets, b, main);
                EraseSlot(ref buckets, ref entries, slot);
                return true;
            }
            next = node.Next;
        }
        return false;
    }

    public TValue this[TKey key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (Get(key, out var result)) return result;
            throw new KeyNotFoundException($"Unable to find entry - {key?.GetType().FullName} key - {key?.GetHashCode()}");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (!InsertOrUpdate(key, value))
            {
                throw new KeyNotFoundException($"Unable to find entry - {key?.GetType().FullName} key - {key?.GetHashCode()}");
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Copy(BlitzMap<TKey, TValue> other)
    {
        foreach (var item in other) Insert(item.Key, item.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<Entry>() && _count != 0)
            Array.Clear(_entries, 0, (int)_count);

        _buckets.AsSpan().Fill(new Bucket { Signature = INACTIVE, Next = INACTIVE });

        _count = 0;
        _last = 0;
        _numBuckets = (uint)_length;
    }

    #endregion

    #region Private Methods

    private void Resize()
    {
        _length <<= 1;
        _mask = (uint)_length - 1u;
        _maxCountBeforeResize = (uint)(_length * _loadFactor);

        _last = 0;
        _numBuckets = (uint)_length >> 1;

        var oldEntriesArr = _entries;
        uint oldCount = _count;

        int bucketCount = _length;
        int entryCount = (int)(_length * _loadFactor);

        _entries = GC.AllocateUninitializedArray<Entry>(entryCount);
        _buckets = GC.AllocateUninitializedArray<Bucket>(bucketCount);
        _buckets.AsSpan().Fill(new Bucket { Signature = INACTIVE, Next = INACTIVE });

        Array.Copy(oldEntriesArr, 0, _entries, 0, (int)oldCount);

        ref var newBucketBase = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref var newEntryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        for (uint i = 0; i < oldCount; i++)
        {
            ref var e = ref Unsafe.Add(ref newEntryBase, i);
            RebuildBucketsInternal(ref newBucketBase, ref newEntryBase, i, e.Key);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void RebuildBucketsInternal(ref Bucket buckets, ref Entry entries, uint slot, TKey key)
    {
        uint hash = _hasher.ComputeHash(key);
        uint main = hash & _mask;
        uint sig = hash & ~_mask;

        ref Bucket bucket = ref Unsafe.Add(ref buckets, main);
        uint slotSig = bucket.Signature;

        if (slotSig == INACTIVE)
        {
            bucket.Signature = sig | slot;
            bucket.Next = INACTIVE;
            return;
        }

        uint packed = slotSig;
        uint index = packed & _mask;
        uint owner = _hasher.ComputeHash(Unsafe.Add(ref entries, index).Key) & _mask;

        if (owner != main)
        {
            KickoutBucket(ref buckets, owner, main);
            bucket.Signature = sig | slot;
            bucket.Next = INACTIVE;
            return;
        }

        if (bucket.Next == INACTIVE)
        {
            uint n = FindEmptyBucket(ref buckets, main, 1);
            bucket.Next = n;

            ref Bucket node = ref Unsafe.Add(ref buckets, n);
            node.Signature = sig | slot;
            node.Next = INACTIVE;
            return;
        }

        uint next = bucket.Next;
        while (true)
        {
            ref Bucket node = ref Unsafe.Add(ref buckets, next);
            if (node.Next == INACTIVE)
            {
                bucket = ref node;
                break;
            }
            next = node.Next;
        }

        uint newBucket = FindEmptyBucket(ref buckets, main, 1);
        bucket.Next = newBucket;

        ref Bucket newNode = ref Unsafe.Add(ref buckets, newBucket);
        newNode.Signature = sig | slot;
        newNode.Next = INACTIVE;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void KickoutBucket(ref Bucket buckets, uint index, uint bucket)
    {
        ref Bucket victim = ref Unsafe.Add(ref buckets, bucket);
        uint next = victim.Next;
        uint sig = victim.Signature;

        uint newBucket = FindEmptyBucket(
            ref buckets,
            next == INACTIVE ? bucket : next,
            2);

        uint prev = FindPrevBucket(ref buckets, index, bucket);

        ref Bucket dst = ref Unsafe.Add(ref buckets, newBucket);
        dst.Signature = sig;
        dst.Next = next;

        Unsafe.Add(ref buckets, prev).Next = newBucket;

        victim.Signature = INACTIVE;
        victim.Next = INACTIVE;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint FindPrevBucket(ref Bucket buckets, uint main, uint target)
    {
        uint cur = main;
        while (true)
        {
            uint next = Unsafe.Add(ref buckets, cur).Next;

            if (next == INACTIVE)
                throw new InvalidOperationException("Map state corrupted: target bucket not found in chain.");

            if (next == target) return cur;
            cur = next;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private uint FindEmptyBucket(ref Bucket buckets, uint index, uint cint)
    {
        ref Bucket baseRef = ref buckets;
        uint baseIndex = index & _mask;

        uint bucket = (baseIndex + 1) & _mask;
        if (Unsafe.Add(ref baseRef, bucket).Signature == INACTIVE) return bucket;

        uint next = (bucket + 1) & _mask;
        if (Unsafe.Add(ref baseRef, next).Signature == INACTIVE) return next;

        uint n = 1;
        while (n < quadraticProbeLength)
        {
            uint t = (n * (n + 1)) >> 1;

            bucket = (baseIndex + t + cint) & _mask;
            if (Unsafe.Add(ref baseRef, bucket).Signature == INACTIVE) return bucket;

            next = (bucket + 1) & _mask;
            if (Unsafe.Add(ref baseRef, next).Signature == INACTIVE) return next;
            n++;
        }

        uint last = _last;
        while (true)
        {
            last = (last + 1) & _mask;
            if (Unsafe.Add(ref baseRef, last).Signature == INACTIVE)
            {
                _last = last;
                return last;
            }

            last = (last + 1) & _mask;
            if (Unsafe.Add(ref baseRef, last).Signature == INACTIVE)
            {
                _last = last;
                return last;
            }

            uint medium = (last + _numBuckets) & _mask;
            if (Unsafe.Add(ref baseRef, medium).Signature == INACTIVE)
            {
                _last = medium;
                return medium;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EraseBucket(ref Bucket buckets, uint bucket, uint main)
    {
        ref Bucket victim = ref Unsafe.Add(ref buckets, bucket);
        uint next = victim.Next;

        if (bucket == main)
        {
            if (next == INACTIVE)
            {
                victim.Signature = INACTIVE;
                victim.Next = INACTIVE;
                return;
            }

            uint nb = next;
            ref Bucket src = ref Unsafe.Add(ref buckets, nb);
            victim.Signature = src.Signature;
            victim.Next = src.Next;

            src.Signature = INACTIVE;
            src.Next = INACTIVE;
            return;
        }

        uint prev = FindPrevBucket(ref buckets, main, bucket);
        Unsafe.Add(ref buckets, prev).Next = next;

        victim.Signature = INACTIVE;
        victim.Next = INACTIVE;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EraseSlot(ref Bucket buckets, ref Entry entries, uint slot)
    {
        uint lastSlot = --_count;

        if (slot == lastSlot)
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<Entry>())
                Unsafe.Add(ref entries, slot) = default;
            return;
        }

        // Freshly compute the bucket pointing to the physical memory we are moving.
        // Bypassing brittle map-state caching guarantees stability across Robin Hood operations.
        uint lastBucket = SigToBucket(ref buckets, ref entries, lastSlot);

        Unsafe.Add(ref entries, slot) = Unsafe.Add(ref entries, lastSlot);

        if (RuntimeHelpers.IsReferenceOrContainsReferences<Entry>())
            Unsafe.Add(ref entries, lastSlot) = default;

        ref var node = ref Unsafe.Add(ref buckets, lastBucket);
        uint sig = node.Signature & ~_mask;
        node.Signature = sig | slot;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint SigToBucket(ref Bucket buckets, ref Entry entries, uint index)
    {
        uint hash = _hasher.ComputeHash(Unsafe.Add(ref entries, index).Key);
        uint main = hash & _mask;

        ref var root = ref Unsafe.Add(ref buckets, main);

        // Guard against Sentinel collision: If the index aligns with the _mask exactly, 
        // 0xFFFFFFFF & _mask will falsely evaluate true without this guard.
        if (root.Signature != INACTIVE && (root.Signature & _mask) == index) return main;

        uint next = root.Next;
        while (true)
        {
            if (next == INACTIVE)
                throw new InvalidOperationException("Map state corrupted: Target slot not found in chain.");

            uint b = next;
            ref Bucket node = ref Unsafe.Add(ref buckets, b);

            if (node.Signature != INACTIVE && (node.Signature & _mask) == index)
                return b;

            next = node.Next;
        }
    }

    #endregion

    #region Structs

    [StructLayout(LayoutKind.Sequential)]
    public struct Entry
    {
        public TKey Key;
        public TValue Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entry(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Bucket
    {
        public uint Signature;
        public uint Next;
    }

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