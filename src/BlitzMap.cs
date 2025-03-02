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
    private static readonly uint INACTIVE = int.MaxValue;
    private int _length;
    private double _loadFactor;

    // Last inserted entry
    private uint _lastInsertedBucket = INACTIVE;

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
        _entries = GC.AllocateUninitializedArray<Entry>((int)(_length * loadFactor));
        _numBuckets = (uint)_length >> 1;
    }

    #region Public Methods

    /// <summary>
    /// Retrieves the value associated with the specified key in the hash table. 
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <param name="value">The value associated with the key if found; otherwise, the default value.</param>
    /// <returns>True if the key exists in the hash table; false otherwise.</returns>
    public bool Get(TKey key, out TValue value)
    {
        // Compute the hash code and determine the primary index in the bucket array
        uint hashcode = (uint)key.GetHashCode();
        uint index = hashcode & _mask; // Primary index within the array
        var signature = hashcode & ~_mask; // Secondary mask for collision handling

        // Obtain references to the start of the bucket and entry arrays
        ref var bucketBase = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref var entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        // Start with the initial bucket determined by the hash index
        Bucket bucket = Unsafe.Add(ref bucketBase, index);

        // Traverse the linked list of buckets to find the matching key
        while (true)
        {
            // Check if the signature matches the desired key's signature
            if (signature == (bucket.Signature & ~_mask))
            {
                // Retrieve the entry associated with the current bucket
                var entry = Unsafe.Add(ref entryBase, bucket.Signature & _mask);

                // Verify that the key matches using the equality comparer
                if (_eq.Equals(key, entry.Key))
                {
                    value = entry.Value; // Set the output value
                    return true; // Key found, return success
                }
            }

            // If the next bucket index is inactive, the search is exhausted
            if (bucket.Next == INACTIVE)
            {
                value = default; // Set default value if not found
                return false; // Key not found, return failure
            }

            // Move to the next bucket in the chain
            bucket = Unsafe.Add(ref bucketBase, bucket.Next);
        }
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
        // Generate a hash code for the provided key
        var hashcode = (uint)key.GetHashCode();

        // Calculate the primary index using the bitwise AND with the mask
        // This constrains the index within the array bounds (_mask is typically array length - 1)
        var index = hashcode & _mask;

        // Calculate the secondary signature for collision detection
        // The signature is the remaining bits of the hash not used by the index
        var signature = hashcode & ~_mask;

        // Obtain references to the start of the bucket and entry arrays
        ref var bucketBase = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref var entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        // Reference to the target bucket using the computed index
        ref var bucket = ref Unsafe.Add(ref bucketBase, index);

        // Fast path: If the target bucket is empty, insert the key-value pair directly
        if (bucket.Signature == INACTIVE)
        {
            // Set the bucket's signature with the count and the signature mask
            bucket.Signature = _count | signature;

            // Store the new entry in the entries array and increment the count
            Unsafe.Add(ref entryBase, _count++) = new Entry(key, value);

            _lastInsertedBucket = index;
            return true; // Indicate successful insertion
        }

        // Check if the current bucket already contains the key
        if (signature == (bucket.Signature & ~_mask) &&
            _eq.Equals(key, Unsafe.Add(ref entryBase, bucket.Signature & _mask).Key))
        {
            return false; // Key already exists, insertion fails
        }

        // If the next slot is empty, find a new bucket and insert directly
        if (bucket.Next == INACTIVE)
        {
            // Locate an empty bucket starting from the next index
            uint nBucket = FindEmptyBucket(ref bucketBase, index, 1);

            // Link the current bucket to the newly found empty bucket
            bucket.Next = nBucket;

            // Store the entry in the new bucket and update the signature
            Unsafe.Add(ref bucketBase, nBucket).Signature = _count | signature;
            Unsafe.Add(ref entryBase, _count++) = new Entry(key, value);

            _lastInsertedBucket = nBucket;

            return true; // Successful insertion
        }

        // Collision resolution: Traverse the linked list of buckets
        byte cint = 1; // Chain length counter

        while (true)
        {
            // Move to the next bucket in the chain
            bucket = ref Unsafe.Add(ref bucketBase, bucket.Next);

            // Check for an existing key in the chain
            if (signature == (bucket.Signature & ~_mask) &&
                _eq.Equals(key, _entries[bucket.Signature & _mask].Key))
            {
                return false; // Key already exists, insertion fails
            }

            // If we reach the end of the chain, exit the loop to add the new entry
            if (bucket.Next == INACTIVE)
            {
                break;
            }

            ++cint; // Increment the chain length counter
        }

        // Locate a new empty bucket to extend the chain
        uint newBucketIndex = FindEmptyBucket(ref bucketBase, index, cint);

        // Link the end of the chain to the new bucket
        bucket.Next = newBucketIndex;

        // Set the signature and insert the entry into the new bucket
        Unsafe.Add(ref bucketBase, newBucketIndex).Signature = _count | signature;
        Unsafe.Add(ref entryBase, _count++) = new Entry(key, value);

        _lastInsertedBucket = newBucketIndex;
        return true; // Insertion successful
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey key)
    {
        // Generate a hash code for the provided key
        var hashcode = (uint)key.GetHashCode();

        // Calculate the primary index using the bitwise AND with the mask
        // This constrains the index within the array bounds (_mask is typically array length - 1)
        var index = hashcode & _mask;

        // Calculate the secondary signature for collision detection
        // The signature is the remaining bits of the hash not used by the index
        var signature = hashcode & ~_mask;

        // Obtain references to the start of the bucket and entry arrays
        ref var bucketBase = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref var entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        // Start with the initial bucket determined by the hash index
        Bucket bucket = Unsafe.Add(ref bucketBase, index);

        // Traverse the linked list of buckets to find the matching key
        while (true)
        {
            // Check if the signature matches the desired key's signature
            if (signature == (bucket.Signature & ~_mask))
            {
                // Retrieve the entry associated with the current bucket

                var eIndex = bucket.Signature & _mask;
                ref var entry = ref Unsafe.Add(ref entryBase, eIndex);

                // Verify that the key matches using the equality comparer
                if (_eq.Equals(key, entry.Key))
                {
                    if (_lastInsertedBucket != INACTIVE)
                    {
                      // Swap(entry, );
                    }

                    bucket.Signature = INACTIVE;

                    // 1 reuse this entry index by swapping the last inserted entry

                    --_count;
                    //TODO Start removal
                    return true; // Key found, return success
                }
            }

            // If the next bucket index is inactive, the search is exhausted
            if (bucket.Next == INACTIVE)
            {
                return false; // Key not found, return failure
            }

            // Move to the next bucket in the chain
            bucket = Unsafe.Add(ref bucketBase, bucket.Next);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Swap(ref Bucket bucketbase, ref Entry entry, uint bucketIndex)
    {
        ref var eBucket = ref Unsafe.Add(ref bucketbase, _lastInsertedBucket);
        eBucket.Signature = 1;
        
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///
    /// </summary>
    /// <param name="index"></param>
    /// /// <returns></returns>  
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint FindEmptyBucket(ref Bucket bucketBase, uint index, uint cint)
    {
        byte step = 3;
        uint offset = 1 + cint;
        //// Quadratic probing: Expand search range
        while (step < quadraticProbeLength)
        {
            var bucket = (index + offset) & _mask;

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

            if (Unsafe.Add(ref bucketBase, medium).Signature == INACTIVE)
            {
                return medium;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Entry(TKey key, TValue value)
    {
        public TKey Key = key;
        public TValue Value = value;
    }

    #endregion
}
