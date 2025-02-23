using System.Collections.Generic;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Faster.Map;
using System.Net.Sockets;
using System.Runtime.Intrinsics.X86;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO.Hashing;

namespace Faster.Map;

public class BlitzMap<TKey, TValue>
{
    #region Properties

    public int Count => (int)_count;

    #endregion

    #region Fields

    private Bucket[] _buckets;
    private Entry[] _entries;
    private uint _numBuckets;
    private uint _count;
    private uint _mask;
    private uint _last;
    private const byte quadraticProbeLength = 6;

    private static readonly uint INACTIVE = int.MaxValue;
    private int _length;
    private double _loadFactor;

    // Interfaces
    private IEqualityComparer<TKey> _eq;

    #endregion

    /// <summary>
    ///
    /// </summary>
    /// <param name="length"></param>
    /// <param name="loadFactor"></param>
    public BlitzMap(int length, double loadFactor)
    {
        _length = (int)BitOperations.RoundUpToPowerOf2((uint)length);
        _mask = (uint)_length - 1;
        _loadFactor = loadFactor;

        _eq = EqualityComparer<TKey>.Default;

        _buckets = GC.AllocateUninitializedArray<Bucket>(_length, true);
        // fill array with inactive marker
        _buckets.AsSpan().Fill(new Bucket { Signature = INACTIVE, Next = INACTIVE });
        _entries = GC.AllocateArray<Entry>((int)(_length * loadFactor));
        _numBuckets = (uint)_length >> 1;
    }

    #region Public Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Get(TKey key, out TValue value)
    {
        uint hashcode = Compute((uint)key.GetHashCode());
        uint index = hashcode & _mask; // Primary index
        var signature = hashcode & ~_mask; // Secondary mask for collision handling

        ref var bucketBase = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref var entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        Bucket bucket = Unsafe.Add(ref bucketBase, index);

        // Traverse the chain
        while (true)
        {
            if (signature == (bucket.Signature & ~_mask))
            {
                var entry = Unsafe.Add(ref entryBase, bucket.Signature & _mask);
                if (_eq.Equals(key, entry.Key))
                {
                    value = entry.Value;
                    return true; // Key already exists
                }
            }

            // Exit the chain if the next bucket is inactive
            if (bucket.Next == INACTIVE)
            {
                value = default;
                return false;
            }

            bucket = Unsafe.Add(ref bucketBase, bucket.Next);
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Insert(TKey key, TValue value)
    {
        var hashcode = Compute((uint)key.GetHashCode());

        var index = hashcode & _mask; // Primary index
        var signature = hashcode & ~_mask; // Secondary mask for collision handling

        ref var bucketBase = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref var entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        // jump to the correct bucket
        ref var bucket = ref Unsafe.Add(ref bucketBase, index);

        // Fast path: Insert directly if the bucket is empty
        if (bucket.Signature == INACTIVE)
        {
            bucket.Signature = _count | signature;
            bucket.SetHomeBucket();
            Unsafe.Add(ref entryBase, _count++) = new Entry(key, value);
            return true;
        }

        // Check if the bucket contains the key
        if (signature == (bucket.Signature & ~_mask) && _eq.Equals(key, _entries[bucket.Signature & _mask].Key))
        {
            return false; // Key already exists
        }

        // Hashing to a bucket which is already taken but not a homebucket results in a kickout algorithm
        if (!bucket.IsHomeBucket())
        {
            uint newBucket = FindEmptyBucket(ref bucketBase, index, 2);
            // Retrieve entry reference **once** to avoid multiple memory accesses
            ref Entry entryRef = ref Unsafe.Add(ref entryBase, bucket.RetrieveIndex(_mask));
            uint homeBucket = Compute((uint) entryRef.Key.GetHashCode()) & _mask;

            // Find previous bucket **without extra memory loads**
            uint previousBucket = FindPreviousBucket(ref bucketBase, homeBucket, index);

            // Direct memory assignment **avoiding unnecessary struct copies**
            ref Bucket newBucketRef = ref Unsafe.Add(ref bucketBase, newBucket);
            newBucketRef = bucket;

            // Efficiently update `Next` pointer **in one step**
            Unsafe.Add(ref bucketBase, previousBucket).Next = newBucket;

            // Direct in-place modification for new bucket
            ref Bucket indexBucketRef = ref Unsafe.Add(ref bucketBase, index);
            indexBucketRef.Next = INACTIVE;
            indexBucketRef.Signature = _count | signature;
            indexBucketRef.SetHomeBucket(); // Direct flag update without temporary struct creation

            // Direct inline entry creation **avoiding heap allocations**
            ref Entry newEntryRef = ref Unsafe.Add(ref entryBase, _count++);
            newEntryRef.Key = key;
            newEntryRef.Value = value;

            return true;
        }
        // If there's an empty next slot, insert directly
        else if (bucket.Next == INACTIVE)
        {
            uint nBucket = FindEmptyBucket(ref bucketBase, index, 1);
            bucket.Next = nBucket;

            Unsafe.Add(ref bucketBase, nBucket).Signature = _count | signature;
            Unsafe.Add(ref entryBase, _count++) = new Entry(key, value);
            return true;
        }

        uint cint = 1;

        // Traverse the chain efficiently
        while (true)
        {
            index = bucket.Next;
            bucket = ref Unsafe.Add(ref bucketBase, index);

            if (signature == (bucket.Signature & ~_mask) && _eq.Equals(key, _entries[bucket.Signature & _mask].Key))
            {
                return false; // Key already exists
            }

            if (bucket.Next == INACTIVE)
            {
                break;
            }

            ++cint;
        }

        uint newBucketIndex = FindEmptyBucket(ref bucketBase, index, cint);
        Unsafe.Add(ref bucketBase, index).Next = newBucketIndex;
        Unsafe.Add(ref bucketBase, newBucketIndex).Signature = _count | signature;
        Unsafe.Add(ref entryBase, _count++) = new Entry(key, value);
        return true;
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///
    /// </summary>
    /// <param name="index"></param>
    /// /// <returns></returns>  
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint FindEmptyBucket(ref Bucket bucketBase, uint index, uint csize)
    {
        var bucket = index;

        if (Unsafe.Add(ref bucketBase, ++bucket).Signature == INACTIVE ||
            Unsafe.Add(ref bucketBase, ++bucket).Signature == INACTIVE)
        {
            return bucket;
        }

        uint offset = 1 + csize;
        byte step = 3;

        //// Quadratic probing: Expand search range
        while (step < quadraticProbeLength)
        {
            bucket = (index + offset) & _mask;

            // Check if the bucket is empty
            if (Unsafe.Add(ref bucketBase, bucket).Signature == INACTIVE ||
                Unsafe.Add(ref bucketBase, ++bucket).Signature == INACTIVE)
            {
                return bucket;
            }

            offset += step++; // Increment offset and step
        }

        // Fallback to a wrapped linear probing for the remaining buckets
        while (true)
        {
            if (Unsafe.Add(ref bucketBase, ++_last).Signature == INACTIVE ||
                Unsafe.Add(ref bucketBase, ++_last).Signature == INACTIVE) // cannot overflow
            {
                return _last;
            }

            // Try a medium offset (reduces clustering by jumping further)
            uint medium = (_numBuckets + _last) & _mask;

            if (Unsafe.Add(ref bucketBase, medium).Signature == INACTIVE ||
                Unsafe.Add(ref bucketBase, ++medium).Signature == INACTIVE)
            {
                return medium;
            }
        }
    }

    // Finds the previous bucket in the chain that points to targetBucket.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint FindPreviousBucket(ref Bucket bucketBase, uint homeBucket, uint targetBucket)
    {
        // Walk the chain until we find a bucket whose Next pointer equals targetBucket.
        while (true)
        {
            var bucket = Unsafe.Add(ref bucketBase, homeBucket);
            if (bucket.Next == targetBucket)
            {
                return homeBucket;
            }
            homeBucket = bucket.Next;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Compute(uint x)
    {
        x ^= x >> 16;
        x *= 0x7FEB352D;
        x ^= x >> 15;
        x *= 0x846CA68B;
        x ^= x >> 16;
        return x;
    }

    public static ulong Compute(ulong h)
    {
        h ^= h >> 23;
        h *= 0x2127599BF4325C37UL;
        h ^= h >> 47;
        return h;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Find<T>(ref T bucketBase, uint index) => Unsafe.Add<T>(ref bucketBase, index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Fmix32(uint h)
    {
        h ^= h >> 16;
        h *= 0x85ebca6b;
        h ^= h >> 13;
        h *= 0xc2b2ae35;
        h ^= h >> 16;

        return h;
    }

    public ref T FindRef<T>(ref T bucketBase, uint index) => ref Unsafe.Add<T>(ref bucketBase, index);

    internal struct Entry(TKey key, TValue value)
    {
        public TKey Key = key;
        public TValue Value = value;
    }

    #endregion
}
