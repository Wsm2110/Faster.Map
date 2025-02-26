using System.Collections.Generic;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Faster.Map.Contracts;

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
        var hashcode = Compute((uint)key.GetHashCode());
        var index = hashcode & _mask; // Primary index
        var signature = hashcode & ~_mask; // Secondary mask for collision handling

        ref var bucketBase = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref var entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        ref var bucket = ref Unsafe.Add(ref bucketBase, index);

        // Fast path: Insert directly if the bucket is empty
        if (bucket.Signature == INACTIVE)
        {
            bucket.Signature = _count | signature;
            Unsafe.Add(ref entryBase, _count++) = new Entry(key, value);
            return true;
        }

        // Check if the bucket contains the key
        if (signature == (bucket.Signature & ~_mask) && _eq.Equals(key, Unsafe.Add(ref entryBase, bucket.Signature & _mask).Key))
        {
            return false; // Key already exists
        }

        // If there's an empty next slot, insert directly
        if (bucket.Next == INACTIVE)
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
            bucket = Unsafe.Add(ref bucketBase, index);

            if (signature == (bucket.Signature & ~_mask) && _eq.Equals(key, Unsafe.Add(ref entryBase, bucket.Signature & _mask).Key))
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint FindEmptyBucket(ref Bucket bucketBase, uint index, uint csize)
    {
        var bucket = index;
        // Check if the bucket is empty
        if (Unsafe.Add(ref bucketBase, bucket).Signature == INACTIVE ||
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
            if (Unsafe.Add(ref bucketBase, ++medium).Signature == INACTIVE ||
                Unsafe.Add(ref bucketBase, ++medium).Signature == INACTIVE)
            {
                return medium;
            }           
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Compute(uint x)
    {
        x ^= x >> 15;
        x *= 0x2c1b3c6dU;
        x ^= x >> 12;
        x *= 0x297a2d39U;
        x ^= x >> 15;
        return x;         
    }

    public static ulong Xmx(ulong x)
    {
        x ^= x >> 32;
        x *= 0x94d049bb133111ebUL;
        x ^= x >> 47;
        return x;
    }

    public static ulong Compute(ulong h)
    {
        h ^= h >> 23;
        h *= 0x2127599BF4325C37UL;
        h ^= h >> 47;
        return h;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Entry(TKey key, TValue value)
    {
        public TKey Key = key;
        public TValue Value = value;
    }

    #endregion
}
