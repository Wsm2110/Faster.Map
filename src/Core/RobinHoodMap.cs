using Faster.Map.Contracts;
using Faster.Map.Hashing;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Convenience wrapper using DefaultHasher.
/// </summary>
public sealed class RobinhoodMap<TKey, TValue> : RobinhoodMap<TKey, TValue, DefaultHasher.Generic<TKey>>
    where TKey : notnull
{
    public RobinhoodMap() : base(16, 0.5d) { }
    public RobinhoodMap(uint capacity) : base(capacity, 0.5d) { }
    public RobinhoodMap(uint capacity, double loadFactor) : base(capacity, loadFactor) { }
}

/// <summary>
/// Robin Hood hashing with linear probing.
/// Buckets store: (index+1) | (psl << entryBits) | (fingerprint << fpShift)
/// - bucket == 0 => empty
/// - PSL starts at 0 for home slot
/// - fingerprint bits depend on capacity (layout computed from entryBits)
/// </summary>
public class RobinhoodMap<TKey, TValue, THasher>
    where THasher : struct, IHasher<TKey>
{
    #region Constants / Layout

    // probe sequence length bits inside bucket word (max PSL representable = 2^PSL_BITS - 1)
    private const int PSL_BITS = 24;
    private const uint PSL_MASK = (1u << PSL_BITS) - 1u;

    #endregion

    #region Fields

    private readonly double _loadFactor;

    private THasher _hasher;

    // dense storage: valid entries are [0 .. _count-1]
    private Entry[] _entries;

    // bucket table: length is power-of-two
    private ulong[] _buckets;

    // count of live entries
    private int _count;

    // resize threshold
    private uint _maxCountBeforeResize;

    // slot mask = buckets.Length - 1
    private uint _mask;

    // layout (computed from capacity)
    private int _entryBits;
    private int _fpBits;
    private int _pslShift;
    private int _fpShift;
    private ulong _entryMask;

    #endregion

    #region Public API

    public int Count => _count;

    public RobinhoodMap() : this(16, 0.5d) { }

    public RobinhoodMap(uint capacity) : this(capacity, 0.5d) { }

    public RobinhoodMap(uint capacity, double loadFactor)
    {
        if (!(loadFactor > 0.0d && loadFactor <= 1.0d))
            throw new ArgumentOutOfRangeException(nameof(loadFactor), "loadFactor must be in the range (0, 1].");

        _loadFactor = loadFactor;

        capacity = Math.Max(16u, capacity);
        capacity = BitOperations.RoundUpToPowerOf2(capacity);

        _buckets = new ulong[capacity];
        _entries = new Entry[capacity];
        _hasher = default;

        ConfigureLayout(capacity);
        _maxCountBeforeResize = (uint)(capacity * _loadFactor);
        _count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Emplace(TKey key, TValue value)
    {
        if ((uint)_count >= _maxCountBeforeResize)
        {
            Resize();
        }

        uint hash = _hasher.ComputeHash(key);
        uint fp = FingerprintOf(hash);

        uint slot = hash & _mask;
        uint distance = 0;

        ref ulong bucketBase = ref MemoryMarshal.GetArrayDataReference(_buckets);

        // ---- pass 1: check duplicate + find insertion start (empty or PSL break) ----
        while (true)
        {
            ulong b = Unsafe.Add(ref bucketBase, (nint)slot);

            if (b == 0)
                break;

            uint bPsl = GetDistance(b);
            if (bPsl < distance)
                break;

            if (GetFingerprint(b) == fp)
            {
                uint idx = GetIndex(b);
                if (_hasher.Equals(_entries[idx].Key, key))
                    return false;
            }

            ++distance;
            slot = (slot + 1) & _mask;
        }

        // allocate new dense entry
        uint newIndex = (uint)_count++;
        _entries[newIndex] = new Entry(key, value);

        // ---- pass 2: robin-hood shift insertion (no duplicate checks) ----
        InsertCarried(ref bucketBase, slot, newIndex, distance, fp);
        return true;
    }

    /// <summary>
    /// Get value by key. Terminates early when bucket PSL < current probe distance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Get(TKey key, out TValue value)
    {
        uint hash = _hasher.ComputeHash(key);
        uint slot = hash & _mask;

        uint fp = FingerprintOf(hash);
        uint distance = 0;

        ref ulong bucketBase = ref MemoryMarshal.GetArrayDataReference(_buckets);
        ref Entry entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        while (true)
        {
            // Check if next bucket is in cache while we process this one.
            ulong b = Unsafe.Add(ref bucketBase, (nint)slot);

            // Empty bucket must terminate lookup immediately.
            if (b == 0 || GetDistance(b) < distance)
                break;

            // Most probes are NOT the match. We want the CPU to assume the
            // fingerprint check fails to keep the pipeline moving.
            if (GetFingerprint(b) == fp)
            {
                uint idx = GetIndex(b);
                Entry entry = Unsafe.Add(ref entryBase, (nint)idx);

                if (_hasher.Equals(entry.Key, key))
                {
                    value = entry.Value;
                    return true;
                }
            }

            distance++;
            slot = (slot + 1) & _mask;
        }

        value = default!;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Update(TKey key, TValue value)
    {
        if (!TryFindBucket(key, out _, out ulong foundBucket))
            return false;

        _entries[GetIndex(foundBucket)].Value = value;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey key)
    {
        if (_count == 0)
            return false;

        if (!TryFindBucket(key, out uint slot, out ulong foundBucket))
            return false;

        uint removedEntryIndex = GetIndex(foundBucket);

        // delete from table (backward shift)
        RemoveAtSlot(slot);

        // dense compaction: move last entry into removedEntryIndex
        int last = --_count;
        if ((uint)last != removedEntryIndex)
        {
            _entries[removedEntryIndex] = _entries[last];
            _entries[last] = default;

            // fix bucket that points to the old "last" index
            FixMovedEntryBucket((uint)last, removedEntryIndex);
        }
        else
        {
            _entries[last] = default;
        }

        return true;
    }

    #endregion

    #region Core Helpers

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InsertCarried(ref ulong bucketBase, uint startSlot, uint carriedIndex, uint carriedPsl, uint carriedFp)
    {
        uint slot = startSlot;

        while (true)
        {
            ref ulong b = ref Unsafe.Add(ref bucketBase, (nint)slot);

            if (b == 0)
            {
                b = Pack(carriedIndex, carriedPsl, carriedFp);
                return;
            }

            uint bPsl = GetDistance(b);

            // robin-hood: steal spot if incumbent PSL is smaller
            if (bPsl < carriedPsl)
            {
                ulong placed = Pack(carriedIndex, carriedPsl, carriedFp);

                // carry the displaced bucket
                ulong displaced = b;
                b = placed;

                carriedIndex = GetIndex(displaced);
                carriedFp = GetFingerprint(displaced);
                carriedPsl = GetDistance(displaced);
            }

            // move carried one step forward => PSL++
            ++carriedPsl;
            slot = (slot + 1) & _mask;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryFindBucket(TKey key, out uint slot, out ulong bucket)
    {
        uint hash = _hasher.ComputeHash(key);
        uint fp = FingerprintOf(hash);

        slot = hash & _mask;
        uint distance = 0;

        ref ulong bucketBase = ref MemoryMarshal.GetArrayDataReference(_buckets);

        while (true)
        {
            bucket = Unsafe.Add(ref bucketBase, (nint)slot);

            if (bucket == 0)
                return false;

            if (GetDistance(bucket) < distance)
                return false;

            if (GetFingerprint(bucket) == fp)
            {
                uint idx = GetIndex(bucket);
                if (_hasher.Equals(_entries[idx].Key, key))
                    return true;
            }

            ++distance;
            slot = (slot + 1) & _mask;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveAtSlot(uint slot)
    {
        // backward-shift deletion
        uint hole = slot;
        uint next = (hole + 1) & _mask;

        ref ulong bucketBase = ref MemoryMarshal.GetArrayDataReference(_buckets);

        while (true)
        {
            ulong bNext = Unsafe.Add(ref bucketBase, (nint)next);

            if (bNext == 0)
                break;

            uint pslNext = GetDistance(bNext);
            if (pslNext == 0)
                break;

            // shift next into hole, decrement its PSL
            Unsafe.Add(ref bucketBase, (nint)hole) = Pack(GetIndex(bNext), pslNext - 1, GetFingerprint(bNext));

            hole = next;
            next = (next + 1) & _mask;
        }

        Unsafe.Add(ref bucketBase, (nint)hole) = 0;
    }

    private void FixMovedEntryBucket(uint oldIndex, uint newIndex)
    {
        // Find the bucket that currently references oldIndex and rewrite it to newIndex (keeping PSL/fp intact).
        TKey movedKey = _entries[newIndex].Key;
        uint hash = _hasher.ComputeHash(movedKey);
        uint slot = hash & _mask;
        uint distance = 0;

        ref ulong bucketBase = ref MemoryMarshal.GetArrayDataReference(_buckets);

        while (true)
        {
            ulong b = Unsafe.Add(ref bucketBase, (nint)slot);

            if (b == 0)
                throw new InvalidOperationException("Bucket for moved entry not found (unexpected empty slot).");

            if (GetDistance(b) < distance)
                throw new InvalidOperationException("Bucket for moved entry not found (unexpected PSL break).");

            if (GetIndex(b) == oldIndex)
            {
                Unsafe.Add(ref bucketBase, (nint)slot) = Pack(newIndex, GetDistance(b), GetFingerprint(b));
                return;
            }

            ++distance;
            slot = (slot + 1) & _mask;
        }
    }

    private void Resize()
    {
        uint oldCapacity = (uint)_buckets.Length;
        uint newCapacity = oldCapacity << 1;

        var oldEntries = _entries;
        int oldCount = _count;

        _buckets = new ulong[newCapacity];
        _entries = new Entry[newCapacity];
        Array.Copy(oldEntries, 0, _entries, 0, oldCount);

        ConfigureLayout(newCapacity);
        _maxCountBeforeResize = (uint)(newCapacity * _loadFactor);

        // rebuild buckets from dense entries [0..oldCount)
        ref ulong bucketBase = ref MemoryMarshal.GetArrayDataReference(_buckets);

        for (uint i = 0; i < (uint)oldCount; i++)
        {
            uint hash = _hasher.ComputeHash(_entries[i].Key);
            uint fp = FingerprintOf(hash);

            uint slot = hash & _mask;
            uint psl = 0;

            // insert carried (no dup check)
            InsertCarried(ref bucketBase, slot, i, psl, fp);
        }
    }

    private void ConfigureLayout(uint capacity)
    {
        // entries length == capacity, index stored as (index+1) => need bits to represent [1..capacity]
        int capLog2 = BitOperations.Log2(capacity);
        _entryBits = capLog2 + 1;

        // remaining bits split between PSL and fingerprint (fingerprint clamped)
        _fpBits = Math.Clamp(64 - PSL_BITS - _entryBits, 8, 32);

        _pslShift = _entryBits;
        _fpShift = 64 - _fpBits;

        _entryMask = (1UL << _entryBits) - 1UL;
        _mask = capacity - 1;

        Debug.Assert(_entryBits > 0);
        Debug.Assert(_fpBits >= 8 && _fpBits <= 32);
        Debug.Assert(_pslShift + PSL_BITS + _fpBits <= 64 || capacity > 0);
    }

    #endregion

    #region Bucket packing

    // The fingerprint is derived from the high bits of the hash.
    // Its width (_fpBits) is dynamic and depends on the current table capacity.
    // As the table grows, more bits are reserved for entry indices, so fewer bits
    // remain available for the fingerprint.
    // The fingerprint never changes for an entry while the table size is fixed
    // and is used as a fast pre-filter before full key comparison.

    /// <summary>
    /// Extracts the dynamic fingerprint from the high bits of the hash.
    /// The number of bits depends on the current table size.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint FingerprintOf(uint hash)
        => hash >> (32 - _fpBits);

    /// <summary>
    /// Packs (index+1) | (psl << entryBits) | (fp << fpShift). 0 means empty.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private ulong Pack(uint index, uint psl, uint fp)
    {
        ulong storedIndex = (ulong)(index + 1);
        ulong storedPsl = ((ulong)(psl & PSL_MASK)) << _pslShift;
        ulong storedFp = ((ulong)fp) << _fpShift;
        return storedIndex | storedPsl | storedFp;
    }

    /// <summary>
    /// Extracts the dense entry index from a packed bucket.
    /// The index is stored as (index + 1) so that a bucket value of 0 can represent an empty slot.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private uint GetIndex(ulong b)
        => (uint)((b & _entryMask) - 1);

    /// <summary>
    /// Extracts the probe sequence length (PSL) from a packed bucket.
    /// The PSL represents how far the entry is from its home slot and is used to
    /// enforce Robin Hood ordering and early-termination during lookup.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private uint GetDistance(ulong b)
        => (uint)((b >> _pslShift) & PSL_MASK);

    /// <summary>
    /// Extracts the fingerprint from a packed bucket.
    /// The fingerprint consists of the high bits of the hash and its width is
    /// dynamically determined by the current table size.
    /// It is used as a fast pre-filter before performing a full key comparison.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private uint GetFingerprint(ulong b) => (uint)(b >> _fpShift);

    #endregion

    #region Entry

    [DebuggerDisplay("{Key} {Value}")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct Entry
    {
        public TKey Key;
        public TValue Value;

        public Entry(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }

    #endregion
}