using Faster.Map.Contracts;
using Faster.Map.Hasher;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net.Sockets;
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
    private Pair[] _pairs;
    private uint _numBuckets;
    private uint _count;
    private uint _mask;
    private uint _last;
    private uint linearProbeLength = (2 * CacheLineSize) / indexSize;
    private static readonly uint indexSize = 8; // two uints results in 8 bytes
    private const byte quadraticProbeLength = 6;
    private const uint CacheLineSize = 64;
    public const uint INACTIVE = uint.MaxValue;
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
        _buckets.AsSpan().Fill(new Bucket { Current = uint.MaxValue, Next = uint.MaxValue });
        _pairs = GC.AllocateArray<Pair>((int)(_length * loadFactor), true);
        _numBuckets = (uint)_length >> 1;
    }

    #region Public Methods

    public bool Get(TKey key, out TValue value)
    {
        var hashcode = (uint)key.GetHashCode();
        var index = hashcode & _mask;
        uint iMask = hashcode & ~_mask; // Secondary mask for collision handling

        // Directly use array indexing for clarity and performance
        Bucket[] chain = _buckets;
        Pair[] pairs = _pairs;
        Bucket bucket = chain[index];

        if (bucket.Current == uint.MaxValue)
        {
            value = default;
            return false;
        }

        uint slot = bucket.Current & _mask;
        // Compare hashes and keys
        if (iMask == (bucket.Current & ~_mask))
        {
            if (_eq.Equals(key, _pairs[slot].Key))
            {
                value = _pairs[slot].Value;
                return true;
            }
        }

        // End of the chain
        if (bucket.Next == uint.MaxValue)
        {
            value = default;
            return false;
        }

        // Traverse linked list of buckets
        while (true)
        {
            var pair = pairs[slot];
            // Check for duplicate key
            if (iMask == (bucket.Current & ~_mask) && _eq.Equals(key, pair.Key))
            {
                value = pair.Value;
                return true; // Key already exists
            }

            // Move to the next bucket if it exists
            if (bucket.Next == uint.MaxValue)
            {
                break;
            }

            bucket = chain[bucket.Next];
            slot = bucket.Current & _mask;
        }

        value = default;
        return false;
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
        uint iMask = hashcode & ~_mask; // Secondary mask for collision handling

        // Fast path: Insert directly if the bucket is empty    
        if (_buckets[index].Current == uint.MaxValue)
        {         
            _buckets[index].Current = _count | iMask;
            _pairs[_count++] = new Pair(key, value);
            return true;
        }

        Bucket bucket = _buckets[index];
        // Traverse the chain
        while (true)
        {
            if (iMask == (bucket.Current & ~_mask) && _eq.Equals(key, _pairs[bucket.Current & _mask].Key))
            {
                return false; // Key already exists
            }

            // Exit the chain if the next bucket is inactive
            if (bucket.Next == INACTIVE)
            {
                break;
            }

            index = bucket.Next;
            bucket = Find(_buckets, index);
        }

        //// Link a new bucket and insert the pair
        uint newBucketIndex = FindEmptyBucket(index);
        // link previous bucket
        _buckets[index].Next = newBucketIndex;
        _buckets[newBucketIndex].Current = _count | iMask;
        _pairs[_count++] = new Pair(key, value);
        return true;
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///
    /// </summary>
    /// <param name="index"></param>
    /// <param name="chainSize"></param>
    /// <returns></returns>
    /// 

    private uint FindEmptyBucket(uint index)
    {  
        byte offset = 4;
        byte step = 3;
        // Quadratic probing: Expand search range
        while (step < quadraticProbeLength)
        {
            uint bucket = (index + offset) & _mask;

            // Check if the bucket is empty
            if (IsEmpty(bucket) || IsEmpty(++bucket))
                return bucket;

            offset += step++; // Increment offset and step
        }

        // Fallback to a wrapped linear probing for the remaining buckets
        while (true)
        {
            //  _last = ; // & _mask; // Wrap around using the mask
            if (IsEmpty(++_last)) // cannot overflow 
            {
                return _last;
            }

            // Try a medium offset (reduces clustering by jumping further)
            uint medium = (_numBuckets + _last) & _mask;
            if (IsEmpty(medium))
            {
                return medium;
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
    private bool IsEmpty(uint bucket) { return Find(_buckets, bucket).Current == uint.MaxValue; }

    [DebuggerDisplay("Empty:{Current == uint.MaxValue} Next:{Next}")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct Bucket
    {
        public uint Next; // next stores the offset in the chain
        public uint Current; // while current stores the position of the pair bucket
    };

    [StructLayout(LayoutKind.Sequential)]
    internal struct Pair(TKey key, TValue value)
    {
        public TKey Key = key;
        public TValue Value = value;
    }

    #endregion
}
