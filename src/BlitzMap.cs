using Faster.Map.Contracts;
using Faster.Map.Hasher;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;

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
    private uint linearProbeLength = (2 * CacheLineSize) / indexSize;
    private static readonly uint indexSize = 8; // two uints results in 8 bytes
    private const byte quadraticProbeLength = 6;
    private const uint CacheLineSize = 64;
    private static readonly uint INACTIVE = uint.MaxValue;
    private int _length;
    private double _loadFactor;

    // Interfaces
    private IHasher<TKey> _hasher;
    private IEqualityComparer<TKey> _eq;

    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="length"></param>
    /// <param name="loadFactor"></param>
    /// <param name="hasher"></param>
    public BlitzMap(int length, double loadFactor, IHasher<TKey> hasher)
    {
        _length = (int)BitOperations.RoundUpToPowerOf2((uint)length);
        _mask = (uint)_length - 1;
        _loadFactor = loadFactor;
        _hasher = hasher ?? new GoldenRatioHasher<TKey>();
        _eq = EqualityComparer<TKey>.Default;

        _buckets = GC.AllocateUninitializedArray<Bucket>(_length, true);
        // fill array with inactive marker
        _buckets.AsSpan().Fill(new Bucket { Fingerprint = INACTIVE, Next = INACTIVE });
        _entries = GC.AllocateArray<Entry>((int)(_length));
        _numBuckets = (uint)_length >> 1;
    }

    #region Public Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Get(TKey key, out TValue value)
    {
        uint hashcode = (uint)key.GetHashCode();
        uint index = hashcode & _mask; // Primary index
        uint fingerprint = index | (hashcode & ~_mask);

        ref var bucketBase = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref var entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        Bucket bucket = Unsafe.Add(ref bucketBase, index);

        // Traverse the chain
        while (true)
        {
            if (fingerprint == bucket.Fingerprint)
            {
                var entry = Unsafe.Add(ref entryBase, bucket.Next);
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
        uint hashcode = (uint)key.GetHashCode();
        uint index = hashcode & _mask; // Primary index

        // Fast path: Insert directly if the bucket is empty    
        ref Bucket bucket = ref _buckets[index];
        // Calculate Fingerprint
        uint fingerprint = index | (hashcode & ~_mask);

        if (bucket.Fingerprint == uint.MaxValue)
        {
            // Determine fingerprint which acts as a chain identifier
            bucket.Fingerprint = fingerprint;
            _entries[index] = new Entry(key, value);
            ++_count;
            return true;
        }

        // To prevent long probe chains we kickout the entry whose homebucket is not the current index
        var c = bucket.Fingerprint & _mask;
        if (c != index)
        {
            var nextBucket = bucket.Next;
            var newBucket = FindEmptyBucket(nextBucket);
            var prevBucket = FindPreviousBucket(c, index);

            var last = nextBucket == bucket.Next ? newBucket : nextBucket;

            _buckets[newBucket] = bucket;
         
            _buckets[prevBucket].Next = newBucket;

            // start of new chain
            _buckets[index].Fingerprint = fingerprint;
            _buckets[index].Next = INACTIVE;

            //// Find a new empty bucket to relocate the displaced entry.
            //uint newBucket = FindEmptyBucket(bucket.Next);
            //uint previousBucket = FindPreviousBucket(c, index);
            //_buckets[newBucket] = bucket;

            //_buckets[index].Fingerprint = (index) | (hashcode & ~_mask);
            //_buckets[index].Next = INACTIVE;
            //// Reassign old bucket and reattach
            //_buckets[previousBucket].Next = newBucket;

            ++_count;
            return true;
        }
        else if (bucket.Next == bucket.Fingerprint)
        {
            // next slot is empty
        }

        // Traverse the chain
        while (true)
        {
            // Exit the chain if the chainId stored in the fingerprint is different
            // Or the next bucket is inactive
            if (bucket.Next == INACTIVE)
            {
                break;
            }

            if (bucket.Fingerprint == fingerprint && _eq.Equals(key, _entries[index].Key))
            {
                return false; // Key already exists
            }


            index = bucket.Next;
            bucket = ref _buckets[index];
        }

        // Link a new bucket and insert the pair
        uint newBucketIndex = FindEmptyBucket(index);

        _buckets[index].Next = newBucketIndex;
        _buckets[newBucketIndex].Fingerprint = fingerprint;
        _entries[index] = new Entry(key, value);
        ++_count;
        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Update(TKey key, TValue value)
    {
        uint hashcode = (uint)key.GetHashCode();
        uint index = hashcode & _mask; // Primary index
        uint iMask = hashcode & ~_mask; // Secondary mask for collision handling

        // Fast path: Insert directly if the bucket is empty    
        ref Bucket bucket = ref _buckets[index];

        if (bucket.Fingerprint == uint.MaxValue)
        {
            bucket.Fingerprint = _count | iMask;
            _entries[_count++].Value = value;
            return true;
        }

        // Traverse the chain
        while (true)
        {
            if (iMask == (bucket.Fingerprint & ~_mask) && _eq.Equals(key, _entries[bucket.Fingerprint & _mask].Key))
            {
                _entries[bucket.Fingerprint & _mask].Value = value;
                return true; // Key already exists
            }

            // Exit the chain if the next bucket is inactive
            if (bucket.Next == INACTIVE)
            {
                return false;
            }

            index = bucket.Next;
            bucket = ref _buckets[index];
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey key)
    {
        uint hashcode = (uint)key.GetHashCode();
        uint index = hashcode & _mask; // Primary index
        uint iMask = hashcode & ~_mask; // Secondary mask for collision handling

        // Fast path: Insert directly if the bucket is empty    
        ref Bucket bucket = ref _buckets[index];

        if (iMask == (bucket.Fingerprint & ~_mask) && _eq.Equals(key, _entries[bucket.Fingerprint & _mask].Key))
        {
            bucket.Fingerprint = uint.MaxValue;
            _entries[bucket.Fingerprint & _mask].Value = default;
            if (bucket.Next == INACTIVE)
            {
                return true;
            }
        }

        ref Bucket nextBucket = ref _buckets[bucket.Next];

        // Traverse the chain
        while (true)
        {
            if (iMask == (bucket.Fingerprint & ~_mask) && _eq.Equals(key, _entries[bucket.Fingerprint & _mask].Key))
            {
                bucket.Fingerprint = uint.MaxValue;

                _entries[bucket.Fingerprint & _mask].Value = default;
                _count--;
                return true; // Key already exists
            }

            // Exit the chain if the next bucket is inactive
            if (bucket.Next == INACTIVE)
            {
                return false;
            }

            index = bucket.Next;
            bucket = ref _buckets[index];
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///
    /// </summary>
    /// <param name="index"></param>
    /// <param name="chainSize"></param>
    /// <returns></returns>   
    private uint FindEmptyBucket(uint index)
    {
        ref var bucketBase = ref MemoryMarshal.GetArrayDataReference(_buckets);

        byte offset = 4;
        byte step = 3;

        // Quadratic probing: Expand search range
        while (step < quadraticProbeLength)
        {
            uint bucket = (index + offset) & _mask;

            // Check if the bucket is empty
            if (Unsafe.Add(ref bucketBase, bucket).Fingerprint == INACTIVE || Unsafe.Add(ref bucketBase, ++bucket).Fingerprint == INACTIVE)
            {
                return bucket;
            }

            offset += step++; // Increment offset and step
        }

        // Fallback to a wrapped linear probing for the remaining buckets
        while (true)
        {
            if (Unsafe.Add(ref bucketBase, ++_last).Fingerprint == INACTIVE) // cannot overflow 
            {
                return _last;
            }

            // Try a medium offset (reduces clustering by jumping further)
            uint medium = (_numBuckets + _last) & _mask;
            if (Unsafe.Add(ref bucketBase, medium).Fingerprint == INACTIVE)
            {
                return medium;
            }
        }
    }

    /// <summary>
    /// KickoutBucket:
    /// If an entry is found not to be in its home bucket, it is "kicked out".
    /// The displaced entry is moved to a new empty bucket and the chain is re-linked.
    /// </summary>
    /// <param name="kmain">The home bucket index for the displaced key (its ideal position)</param>
    /// <param name="bucket">The bucket index where the displaced key currently resides</param>
    /// <returns>The bucket index that was kicked out (now marked inactive)</returns>
    //private void KickoutBucket(uint kmain, uint bucket)
    //{
    //    // Reference to the current bucket (to avoid extra loads)
    //    ref Bucket curr = ref _buckets[bucket];

    //    // Retrieve the next bucket in the chain.
    //    uint nextBucket = curr.Next;



    //    // Find the previous bucket in the chain starting from the home bucket.
    //    uint prevBucket = FindPreviousBucket(kmain, bucket);

    //    // Determine the new "next" pointer for the relocated entry.
    //    // If nextBucket equals the current bucket, then there is no proper chain;
    //    // use newBucket as the chain terminator.
    //    uint last = (nextBucket == bucket) ? newBucket : nextBucket;

    //    // Move the displaced entry to the new bucket.
    //    _buckets[newBucket] = new Bucket { Next = last, Fingerprint = curr.Fingerprint };

    //    // Update the previous bucket in the chain to point to the new bucket.
    //    _buckets[prevBucket].Next = newBucket;

    //    // Mark the current bucket as inactive (kicked out).
    //    curr.Next = curr.Fingerprint;
    //}

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


    /// <summary>
    /// Finds the element in the array at the specified index.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="array">The array to search.</param>
    /// <param name="index">The index to look up.</param>
    /// <returns>A reference to the found element.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref Bucket Find(Bucket[] array, uint index)
    {
        //no bounds check
        ref var arr0 = ref MemoryMarshal.GetArrayDataReference(array);
        return ref Unsafe.Add(ref arr0, index);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bucket"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsEmpty(uint bucket) { return Find(_buckets, bucket).Fingerprint == uint.MaxValue; }

    [DebuggerDisplay("Empty:{Fingerprint == uint.MaxValue} Next:{Next}")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct Bucket
    {
        public uint Next; // next stores the offset in the chain
        public uint Fingerprint;
    };

    [StructLayout(LayoutKind.Sequential)]
    internal struct Entry(TKey key, TValue value)
    {
        public TKey Key = key;
        public TValue Value = value;
    }

    #endregion
}
