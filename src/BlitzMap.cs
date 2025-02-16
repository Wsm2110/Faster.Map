using System.Collections.Generic;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Faster.Map;
using System.Net.Sockets;
using System.Runtime.Intrinsics.X86;

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
    private const byte quadraticProbeLength = 12;

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
        uint hashcode = (uint)key.GetHashCode();
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
        var hashcode = (uint)key.GetHashCode();
        var index = hashcode & _mask; // Primary index
        var signature = hashcode & ~_mask; // Secondary mask for collision handling

        // d
        ref var bucketBase = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref var bucket = ref FindRef(ref bucketBase, index);

        // Fast path: Insert directly if the bucket is empty    
        if (bucket.Signature == INACTIVE)
        {
            bucket.Signature = _count | signature;
            bucket.SetHomeBucket();
            _entries[_count++] = new Entry(key, value);
            return true;
        }

        // Check if key already exists
        if (signature == (bucket.Signature & ~_mask))
        {
            if (_eq.Equals(key, _entries[bucket.Signature & _mask].Key))
            {
                return false; // Key already exists
            }
        }

        // If not a home bucket, perform a "kickout" relocation preventing long probe chains
        if (!bucket.IsHomeBucket())
        {
            var nextBucket = bucket.Next;
            var newBucket = FindEmptyBucket(ref bucketBase, index, 1);

            var entry = _entries[bucket.RetrieveIndex(_mask)];
            var homeBucket = (uint)entry.Key.GetHashCode() & _mask;
            var prevBucket = FindPreviousBucket(homeBucket, index);

            // Reattach newBucket to chain
            _buckets[prevBucket].Next = newBucket;

            // Assign old bucket to new 
            _buckets[newBucket] = bucket;

            _buckets[index] = new Bucket
            {
                Signature = _count | signature,
                Next = INACTIVE
            };

            _buckets[index].SetHomeBucket();

            _entries[_count++] = new Entry(key, value);

            ++_count;
            return true;
        }

        // If there's an empty next slot, insert directly
        if (bucket.Next == INACTIVE)
        {
            //// Link a new bucket and insert the pair
            uint nBucket = FindEmptyBucket(ref bucketBase, index, 1);
            // link previous bucket
            bucket.Next = nBucket;

            _buckets[nBucket].Signature = _count | signature;
            _entries[_count++] = new Entry(key, value);
            return true;
        }

        uint cint = 1;

        // Traverse the chain
        while (true)
        {
            index = bucket.Next;
            bucket = ref _buckets[index];

            if (signature == (bucket.Signature & ~_mask) && _eq.Equals(key, _entries[bucket.Signature & _mask].Key))
            {
                return false; // Key already exists
            }

            // Exit the chain when the next bucket is inactive
            if (bucket.Next == INACTIVE)
            {
                break;
            }

            ++cint;
        }

        // Link a new bucket and insert the pair
        uint newBucketIndex = FindEmptyBucket(ref bucketBase, index, cint);
        _buckets[index].Next = newBucketIndex;
        _buckets[newBucketIndex].Signature = _count | signature;
        _entries[_count++] = new Entry(key, value);
        return true;
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///
    /// </summary>
    /// <param name="index"></param>
    /// /// <returns></returns>  
    private uint FindEmptyBucket(ref Bucket bucketBase, uint index, uint csize)
    {
        //const uint linearProbeLength = 2 + 2 * 64 / sizeof(int); // 64 for cache line size assumption
        //for (uint offset = csize + 2, step = 4; offset <= linearProbeLength;)
        //{
        //    uint bucket = (index + offset) & _mask;
        //    if (Unsafe.Add(ref bucketBase, bucket).Signature == INACTIVE ||
        //            Unsafe.Add(ref bucketBase, ++bucket).Signature == INACTIVE)
        //    {
        //        return bucket;
        //    }
        //        offset += step;
        //}

        uint offset = 1 + csize;
        byte step = 3;

        //// Quadratic probing: Expand search range
        while (step < quadraticProbeLength)
        {
            uint bucket = (index + offset) & _mask;

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
            if (Unsafe.Add(ref bucketBase, ++_last).Signature == INACTIVE) // cannot overflow
            {
                return _last;
            }

            // Try a medium offset (reduces clustering by jumping further)
            uint medium = (_numBuckets + _last) & _mask;
            if (Unsafe.Add(ref bucketBase, medium).Signature == INACTIVE)
            {
                return medium;
            }
        }
    }

    // Finds the previous bucket in the chain that points to targetBucket.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint FindPreviousBucket(uint homeBucket, uint targetBucket)
    {
        // Walk the chain until we find a bucket whose Next pointer equals targetBucket.
        while (true)
        {
            var bucket = _buckets[homeBucket];
            if (bucket.Next == targetBucket)
            {
                return homeBucket;
            }
            homeBucket = bucket.Next;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Find<T>(ref T bucketBase, uint index) => Unsafe.Add<T>(ref bucketBase, index);

    public ref T FindRef<T>(ref T bucketBase, uint index) => ref Unsafe.Add<T>(ref bucketBase, index);

    internal struct Entry(TKey key, TValue value)
    {
        public TKey Key = key;
        public TValue Value = value;
    }

    #endregion
}
