using System.Collections.Generic;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

    private static readonly ushort INACTIVE = ushort.MaxValue;
    private int _length;
    private double _loadFactor;

    // Interfaces
    private IEqualityComparer<TKey> _eq;
    #endregion

    public BlitzMap(int length, double loadFactor)
    {
        _length = (int)BitOperations.RoundUpToPowerOf2((uint)length);
        _mask = (uint)_length - 1;
        _loadFactor = loadFactor;

        _eq = EqualityComparer<TKey>.Default;

        _buckets = GC.AllocateUninitializedArray<Bucket>(_length, true);
        _buckets.AsSpan().Fill(new Bucket { Signature = INACTIVE, Next = INACTIVE });
        _entries = GC.AllocateArray<Entry>((int)(_length * loadFactor));
        _numBuckets = (uint)_length >> 1;
    }

    #region Public Methods
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Get(TKey key, out TValue value)
    {
        uint hashcode = (uint)key.GetHashCode();
        uint mask = _mask;
        uint index = hashcode & mask;
        uint sig = hashcode & ~mask;

        ref var bucketBase = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref var entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);
        Bucket bucket = Unsafe.Add(ref bucketBase, index);

        if ((bucket.Signature & ~mask) == sig)
        {
            ref var entry = ref Unsafe.Add(ref entryBase, bucket.Signature & mask);
            if (_eq.Equals(key, entry.Key))
            {
                value = entry.Value;
                return true;
            }
        }

        var direction = bucket.IsHomeBucket() ? bucket.Next : bucket.Overflow;
        while (true)
        {
            bucket = Unsafe.Add(ref bucketBase, (index + direction) & _mask);
            if ((bucket.Signature & ~mask) == sig)
            {
                uint entryIndex = bucket.Signature & mask;
                ref var entry = ref Unsafe.Add(ref entryBase, (int)entryIndex);
                if (_eq.Equals(key, entry.Key))
                {
                    value = entry.Value;
                    return true;
                }
            }

            if (bucket.Next == INACTIVE)
                break;

            direction = bucket.Next;
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Insert(TKey key, TValue value)
    {
        var hashcode = (uint)key.GetHashCode();
        var index = hashcode & _mask;
        var signature = hashcode & ~_mask;

        if (index == 53767476) 
        {
        
        }

        ref var bucketBase = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref var entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);
        ref var bucket = ref Unsafe.Add(ref bucketBase, index);

        // Fast path: bucket is empty
        if (bucket.Signature == INACTIVE)
        {
            bucket.Signature = _count | signature;
            bucket.SetHomeBucket();
            Unsafe.Add(ref entryBase, _count++) = new Entry(key, value);
            return true;
        }

        if (signature == (bucket.Signature & ~_mask) && _eq.Equals(key, Unsafe.Add(ref entryBase, bucket.Signature & _mask).Key))
            return false;

        var homebucket = bucket.IsHomeBucket();
        if (!homebucket && bucket.Overflow == 0)
        {
            var h = FindEmptyBucket(ref bucketBase, index, 1);
            bucket.Overflow = h.Distance;
            Unsafe.Add(ref bucketBase, h.Index).Signature = _count | signature;
            Unsafe.Add(ref entryBase, _count++) = new Entry(key, value);
            return true;
        }      

        var direction = homebucket ? bucket.Next : bucket.Overflow;
        if (direction == INACTIVE) 
        {
            var h = FindEmptyBucket(ref bucketBase, index, 1);
            bucket.Next = h.Distance;
            Unsafe.Add(ref bucketBase, h.Index).Signature = _count | signature;
            Unsafe.Add(ref entryBase, _count++) = new Entry(key, value);
            return true;
        }


        byte cint = 1;

        while (true)
        {
            bucket = ref Unsafe.Add(ref bucketBase, (index + direction) & _mask);
            if (signature == (bucket.Signature & ~_mask) && _eq.Equals(key, _entries[bucket.Signature & _mask].Key))
                return false;

            if (bucket.Next == INACTIVE)
                break;

            direction = bucket.Next;
            ++cint;
        }

        var i = FindEmptyBucket(ref bucketBase, index, cint);
        bucket.Next = i.Distance;
        Unsafe.Add(ref bucketBase, i.Index).Signature = _count | signature;
        Unsafe.Add(ref entryBase, _count++) = new Entry(key, value);
        return true;
    }
    #endregion

    #region Private Methods
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IndexedDistance FindEmptyBucket(ref Bucket bucketBase, uint index, byte csize)
    {
        var bucket = index;
        if (Unsafe.Add(ref bucketBase, ++bucket).Signature == INACTIVE)
            return new IndexedDistance(bucket, 1);

        var offset = (ushort)(1 + csize);
        byte step = 3;

        while (true)
        {
            bucket = (index + offset) & _mask;
            if (Unsafe.Add(ref bucketBase, bucket).Signature == INACTIVE)
                return new IndexedDistance(bucket, offset);

            offset += step++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint FindPreviousBucket(ref Bucket bucketBase, uint homeBucket, uint targetBucket)
    {
        while (true)
        {
            var bucket = Unsafe.Add(ref bucketBase, homeBucket);
            if (bucket.Overflow == targetBucket)
                return homeBucket;

            homeBucket = bucket.Overflow;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Entry(TKey key, TValue value)
    {
        public TKey Key = key;
        public TValue Value = value;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct IndexedDistance(uint index, ushort distance)
    {
        public readonly uint Index = index;
        public readonly ushort Distance = distance;
    }
    #endregion
}
