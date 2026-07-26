// Copyright (c) 2026, Wiljan Ruizendaal. All rights reserved. <wruizendaal@gmail.com> 
// Distributed under the MIT Software License, Version 1.0.

using Faster.Map.Contracts;
using Faster.Map.Hashing;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Faster.Map.Core;

/// <summary>
/// A specialized implementation of <see cref="DenseMap{TKey, TValue, THasher}"/> that
/// defaults to using the <see cref="GoldenRatioHasher{TKey}"/> for efficient hashing.
/// </summary>
public class DenseMap<TKey, TValue> : DenseMap<TKey, TValue, DefaultHasher.Generic<TKey>>
{
    public DenseMap(uint length) : base(length, 0.875) { }
    public DenseMap(uint length, double loadFactor) : base(length, loadFactor) { }
    public DenseMap() : base(32, 0.875) { }
}

/// <summary>
/// DenseMap is a high-performance hashmap implementation achieving C++ Abseil/F14 parity.
/// It features Array of Structs (AoS) locality, Asymmetric Metadata Sign-Bit optimization, 
/// linear group prefetching, lazy SIMD masking, and zero floating-point overhead on hot paths.
/// </summary>
public class DenseMap<TKey, TValue, THasher> where THasher : struct, IHasher<TKey>
{
    #region Properties

    public int Count { get; private set; }
    public uint Size => (uint)_entries.Length;

    public IEnumerable<KeyValuePair<TKey, TValue>> Entries
    {
        get
        {
            for (int i = 0; i < _length; ++i)
                if (_controlBytes[i] > 0)
                    yield return new KeyValuePair<TKey, TValue>(_entries[i].Key, _entries[i].Value);
        }
    }

    public IEnumerable<TKey> Keys
    {
        get
        {
            for (int i = 0; i < _length; ++i)
                if (_controlBytes[i] > 0)
                    yield return _entries[i].Key;
        }
    }

    public IEnumerable<TValue> Values
    {
        get
        {
            for (int i = 0; i < _length; ++i)
                if (_controlBytes[i] > 0)
                    yield return _entries[i].Value;
        }
    }

    private const uint _groupWidth = 32;

    #endregion

    #region Fields

    private const sbyte _emptyBucket = -128;
    private const sbyte _tombstone = 0;

    private sbyte[] _controlBytes;
    private Entry[] _entries;

    private uint _maxTombstoneBeforeRehash;
    private uint _maxLookupsBeforeResize;
    private uint _tombstoneCounter;

    private uint _length;
    private uint _mask;
    private readonly double _loadFactor;
    private readonly THasher _hasher;
    private bool _suppressRebuild;

    #endregion

    #region Constructor

    public DenseMap(uint length) : this(length, 0.875) { }
    public DenseMap() : this(32, 0.875) { }
    public DenseMap(uint length, double loadFactor)
    {
        if (!Vector256.IsHardwareAccelerated)
            throw new NotSupportedException("AVX2 or equivalent is required.");

        _length = length;
        _loadFactor = loadFactor > 0.875 ? 0.875 : loadFactor;
        _hasher = default;

        // Vector alignment constraint: Minimum map size must be 32 to align with AVX2 boundaries.
        _length = _length < 32 ? 32 : BitOperations.RoundUpToPowerOf2(_length);

        _maxLookupsBeforeResize = (uint)(_length * _loadFactor);
        _maxTombstoneBeforeRehash = (uint)(_length * 0.125);

        _controlBytes = GC.AllocateArray<sbyte>((int)_length + (int)_groupWidth);
        _entries = GC.AllocateArray<Entry>((int)_length);

        _controlBytes.AsSpan().Fill(_emptyBucket);
        _mask = _length - 1;
    }

    #endregion

    #region Public Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Insert(TKey key, TValue value)
    {
        if ((uint)Count + _tombstoneCounter >= _maxLookupsBeforeResize)
        {
            Resize();
        }

        var hashcode = _hasher.ComputeHash(key);
        var h2 = H2(hashcode);

        uint index = hashcode & _mask;
        uint firstAvailableSlot = uint.MaxValue;

        ref sbyte ctrl = ref MemoryMarshal.GetArrayDataReference(_controlBytes);
        ref Entry entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        while (true)
        {
            var source = Vector256.LoadUnsafe(ref ctrl, (nuint)index);
            uint emptyMask = source.ExtractMostSignificantBits();

            // HOLY GRAIL: Lazy Tombstone Masking
            // Evaluates vector equality only if an available slot hasn't been found yet.
            if (firstAvailableSlot == uint.MaxValue)
            {
                uint tombstoneMask = Vector256.Equals(source, Vector256<sbyte>.Zero).ExtractMostSignificantBits();
                uint combinedMask = emptyMask | tombstoneMask;

                if (combinedMask != 0)
                {
                    firstAvailableSlot = (index + (uint)BitOperations.TrailingZeroCount(combinedMask)) & _mask;
                }
            }

            if (emptyMask != 0)
            {
                // Eradicated ternary operator logic: firstAvailableSlot is mathematically guaranteed to be assigned.
                uint slot = firstAvailableSlot;

                if (Unsafe.Add(ref ctrl, (nuint)slot) == _tombstone)
                {
                    _tombstoneCounter--;
                }

                SetCtrl(ref ctrl, slot, h2);

                ref var entry = ref Unsafe.Add(ref entryBase, (nuint)slot);
                entry.Key = key;
                entry.Value = value;

                ++Count;
                return;
            }

            index = (index + _groupWidth) & _mask;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InsertOrUpdate(TKey key, TValue value)
    {
        if ((uint)Count + _tombstoneCounter >= _maxLookupsBeforeResize) Resize();

        var hash = _hasher.ComputeHash(key);
        var h2 = H2(hash);
        var target = Vector256.Create(h2);

        uint index = hash & _mask;
        uint firstAvailableSlot = uint.MaxValue;

        ref sbyte ctrl = ref MemoryMarshal.GetArrayDataReference(_controlBytes);
        ref Entry entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        while (true)
        {
            var source = Vector256.LoadUnsafe(ref ctrl, (nuint)index);
            var matchMask = Vector256.Equals(source, target).ExtractMostSignificantBits();

            while (matchMask != 0)
            {
                uint slot = (index + (uint)BitOperations.TrailingZeroCount(matchMask)) & _mask;
                ref var matchEntry = ref Unsafe.Add(ref entryBase, (nuint)slot);

                if (_hasher.Equals(matchEntry.Key, key))
                {
                    matchEntry.Value = value;
                    return;
                }
                matchMask = ResetLowestSetBit(matchMask);
            }

            uint emptyMask = source.ExtractMostSignificantBits();

            if (firstAvailableSlot == uint.MaxValue)
            {
                uint tombstoneMask = Vector256.Equals(source, Vector256<sbyte>.Zero).ExtractMostSignificantBits();
                uint combinedMask = emptyMask | tombstoneMask;

                if (combinedMask != 0)
                {
                    firstAvailableSlot = (index + (uint)BitOperations.TrailingZeroCount(combinedMask)) & _mask;
                }
            }

            if (emptyMask != 0)
            {
                uint slot = firstAvailableSlot;

                if (Unsafe.Add(ref ctrl, (nuint)slot) == _tombstone) _tombstoneCounter--;

                SetCtrl(ref ctrl, slot, h2);

                ref var newEntry = ref Unsafe.Add(ref entryBase, (nuint)slot);
                newEntry.Key = key;
                newEntry.Value = value;
                Count++;
                return;
            }

            index = (index + _groupWidth) & _mask;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Get(TKey key, out TValue value)
    {
        var hashcode = _hasher.ComputeHash(key);
        var h2 = H2(hashcode);
        var target = Vector256.Create(h2);

        uint index = hashcode & _mask;

        ref sbyte ctrl = ref MemoryMarshal.GetArrayDataReference(_controlBytes);
        ref Entry entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        while (true)
        {
            var source = Vector256.LoadUnsafe(ref ctrl, (nuint)index);
            var matchMask = Vector256.Equals(source, target).ExtractMostSignificantBits();

            while (matchMask != 0)
            {
                uint bit = (uint)BitOperations.TrailingZeroCount(matchMask);
                uint slot = (index + bit) & _mask;

                ref var entry = ref Unsafe.Add(ref entryBase, (nuint)slot);
                if (_hasher.Equals(entry.Key, key))
                {
                    value = entry.Value;
                    return true;
                }

                matchMask = ResetLowestSetBit(matchMask);
            }

            if (source.ExtractMostSignificantBits() != 0)
            {
                value = default!;
                return false;
            }

            index = (index + _groupWidth) & _mask;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TValue GetValueRefOrAddDefault(TKey key)
    {
        if ((uint)Count + _tombstoneCounter >= _maxLookupsBeforeResize)
            Resize();

        var hashcode = _hasher.ComputeHash(key);
        var h2 = H2(hashcode);
        var target = Vector256.Create(h2);

        uint index = hashcode & _mask;
        uint firstAvailableSlot = uint.MaxValue;

        ref sbyte ctrl = ref MemoryMarshal.GetArrayDataReference(_controlBytes);
        ref Entry entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        while (true)
        {
            var source = Vector256.LoadUnsafe(ref ctrl, (nuint)index);
            var matchMask = Vector256.Equals(source, target).ExtractMostSignificantBits();

            while (matchMask != 0)
            {
                uint bit = (uint)BitOperations.TrailingZeroCount(matchMask);
                uint slot = (index + bit) & _mask;

                ref var matchEntry = ref Unsafe.Add(ref entryBase, (nuint)slot);
                if (_hasher.Equals(matchEntry.Key, key))
                    return ref matchEntry.Value;

                matchMask = ResetLowestSetBit(matchMask);
            }

            uint emptyMask = source.ExtractMostSignificantBits();

            if (firstAvailableSlot == uint.MaxValue)
            {
                uint tombstoneMask = Vector256.Equals(source, Vector256<sbyte>.Zero).ExtractMostSignificantBits();
                uint combinedMask = emptyMask | tombstoneMask;

                if (combinedMask != 0)
                {
                    firstAvailableSlot = (index + (uint)BitOperations.TrailingZeroCount(combinedMask)) & _mask;
                }
            }

            if (emptyMask != 0)
            {
                uint slot = firstAvailableSlot;

                if (Unsafe.Add(ref ctrl, (nuint)slot) == _tombstone) _tombstoneCounter--;

                SetCtrl(ref ctrl, slot, h2);

                ref var newEntry = ref Unsafe.Add(ref entryBase, (nuint)slot);
                newEntry.Key = key;
                Count++;
                return ref newEntry.Value;
            }

            index = (index + _groupWidth) & _mask;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Update(TKey key, TValue value)
    {
        var hashcode = _hasher.ComputeHash(key);
        var h2 = H2(hashcode);
        var target = Vector256.Create(h2);

        uint index = hashcode & _mask;

        ref sbyte ctrl = ref MemoryMarshal.GetArrayDataReference(_controlBytes);
        ref Entry entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        while (true)
        {
            var source = Vector256.LoadUnsafe(ref ctrl, (nuint)index);
            var matchMask = Vector256.Equals(source, target).ExtractMostSignificantBits();

            while (matchMask != 0)
            {
                uint bit = (uint)BitOperations.TrailingZeroCount(matchMask);
                uint slot = (index + bit) & _mask;

                ref var entry = ref Unsafe.Add(ref entryBase, (nuint)slot);
                if (_hasher.Equals(entry.Key, key))
                {
                    entry.Value = value;
                    return true;
                }

                matchMask = ResetLowestSetBit(matchMask);
            }

            if (source.ExtractMostSignificantBits() != 0) return false;

            index = (index + _groupWidth) & _mask;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool WasNeverFull(ref sbyte ctrl, uint index)
    {
        uint emptyAfterMask = Vector256.LoadUnsafe(ref ctrl, (nuint)index).ExtractMostSignificantBits();
        uint emptyBeforeMask = Vector256.LoadUnsafe(ref ctrl, (nuint)((index - _groupWidth) & _mask)).ExtractMostSignificantBits();

        if (emptyAfterMask == 0 || emptyBeforeMask == 0) return false;

        int runRight = BitOperations.TrailingZeroCount(emptyAfterMask);
        int runLeft = BitOperations.LeadingZeroCount(emptyBeforeMask);

        return (runRight + runLeft) < _groupWidth;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey key)
    {
        if (!_suppressRebuild && _tombstoneCounter >= _maxTombstoneBeforeRehash)
        {
            Rebuild();
        }

        var hashcode = _hasher.ComputeHash(key);
        var h2 = H2(hashcode);
        var target = Vector256.Create(h2);

        uint index = hashcode & _mask;

        ref sbyte ctrl = ref MemoryMarshal.GetArrayDataReference(_controlBytes);
        ref Entry entryBase = ref MemoryMarshal.GetArrayDataReference(_entries);

        while (true)
        {
            var source = Vector256.LoadUnsafe(ref ctrl, (nuint)index);
            var matchMask = Vector256.Equals(source, target).ExtractMostSignificantBits();

            while (matchMask != 0)
            {
                uint bit = (uint)BitOperations.TrailingZeroCount(matchMask);
                uint slot = (index + bit) & _mask;

                ref var entry = ref Unsafe.Add(ref entryBase, (nuint)slot);
                if (_hasher.Equals(entry.Key, key))
                {
                    if (WasNeverFull(ref ctrl, slot))
                    {
                        SetCtrl(ref ctrl, slot, _emptyBucket);
                    }
                    else
                    {
                        SetCtrl(ref ctrl, slot, _tombstone);
                        _tombstoneCounter++;
                    }

                    entry = default;
                    --Count;
                    return true;
                }
                matchMask = ResetLowestSetBit(matchMask);
            }

            if (source.ExtractMostSignificantBits() != 0) return false;

            index = (index + _groupWidth) & _mask;
        }
    }

    private void Resize()
    {
        var oldEntries = _entries;
        var oldControlBytes = _controlBytes;
        var oldLength = _length;

        _length = oldLength << 1;
        _mask = _length - 1;

        _maxLookupsBeforeResize = (uint)(_length * _loadFactor);
        _maxTombstoneBeforeRehash = (uint)(_length * 0.125);
        _tombstoneCounter = 0;

        var newEntries = GC.AllocateArray<Entry>((int)_length);
        var newControlBytes = GC.AllocateArray<sbyte>((int)_length + (int)_groupWidth);

        newControlBytes.AsSpan().Fill(_emptyBucket);

        ref sbyte newCtrl = ref MemoryMarshal.GetArrayDataReference(newControlBytes);
        ref Entry newEnt = ref MemoryMarshal.GetArrayDataReference(newEntries);

        ref sbyte oldCtrl = ref MemoryMarshal.GetArrayDataReference(oldControlBytes);
        ref Entry oldEnt = ref MemoryMarshal.GetArrayDataReference(oldEntries);

        for (uint i = 0; i < oldLength; ++i)
        {
            var ctrl = Unsafe.Add(ref oldCtrl, (nuint)i);
            if (ctrl <= 0)
                continue;

            var entry = Unsafe.Add(ref oldEnt, (nuint)i);

            var hashcode = _hasher.ComputeHash(entry.Key);
            uint index = hashcode & _mask;

            while (true)
            {
                var source = Vector256.LoadUnsafe(ref newCtrl, (nuint)index);
                uint emptyMask = source.ExtractMostSignificantBits();

                if (emptyMask != 0)
                {
                    uint bit = (uint)BitOperations.TrailingZeroCount(emptyMask);
                    uint slot = (index + bit) & _mask;

                    Unsafe.Add(ref newEnt, (nuint)slot) = entry;
                    SetCtrl(ref newCtrl, slot, ctrl);
                    break;
                }

                index = (index + _groupWidth) & _mask;
            }
        }

        _controlBytes = newControlBytes;
        _entries = newEntries;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static sbyte H2(uint hashcode)
    {
        uint h = hashcode >> 25;
        return (sbyte)(h + ((h - 1) >> 31));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetCtrl(ref sbyte ctrl, uint slot, sbyte value)
    {
        Unsafe.Add(ref ctrl, (nuint)slot) = value;
        uint mirrorOffset = (uint)(((int)slot - 32) >> 31) & _length;
        Unsafe.Add(ref ctrl, (nuint)(slot + mirrorOffset)) = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ResetLowestSetBit(uint value)
    {
        return value & (value - 1);
    }

    private void Rebuild()
    {
        var oldEntries = _entries;
        var oldControlBytes = _controlBytes;
        var length = _length;

        var newEntries = GC.AllocateArray<Entry>((int)length);
        var newControlBytes = GC.AllocateArray<sbyte>((int)length + (int)_groupWidth);

        newControlBytes.AsSpan().Fill(_emptyBucket);

        ref sbyte newCtrl = ref MemoryMarshal.GetArrayDataReference(newControlBytes);
        ref Entry newEnt = ref MemoryMarshal.GetArrayDataReference(newEntries);

        ref sbyte oldCtrl = ref MemoryMarshal.GetArrayDataReference(oldControlBytes);
        ref Entry oldEnt = ref MemoryMarshal.GetArrayDataReference(oldEntries);

        for (uint i = 0; i < length; ++i)
        {
            var ctrl = Unsafe.Add(ref oldCtrl, (nuint)i);
            if (ctrl <= 0) continue;

            var entry = Unsafe.Add(ref oldEnt, (nuint)i);

            var hashcode = _hasher.ComputeHash(entry.Key);
            uint index = hashcode & _mask;

            while (true)
            {
                var source = Vector256.LoadUnsafe(ref newCtrl, (nuint)index);
                uint emptyMask = source.ExtractMostSignificantBits();

                if (emptyMask != 0)
                {
                    uint bit = (uint)BitOperations.TrailingZeroCount(emptyMask);
                    uint slot = (index + bit) & _mask;

                    Unsafe.Add(ref newEnt, (nuint)slot) = entry;
                    SetCtrl(ref newCtrl, slot, ctrl);
                    break;
                }

                index = (index + _groupWidth) & _mask;
            }
        }

        _controlBytes = newControlBytes;
        _entries = newEntries;
        _tombstoneCounter = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(TKey key)
    {
        var hashcode = _hasher.ComputeHash(key);
        var h2 = H2(hashcode);
        var target = Vector256.Create(h2);
        var index = hashcode & _mask;

        ref sbyte start = ref MemoryMarshal.GetArrayDataReference(_controlBytes);
        ref Entry entryStart = ref MemoryMarshal.GetArrayDataReference(_entries);

        while (true)
        {
            var source = Vector256.LoadUnsafe(ref start, (nuint)index);
            var mask = Vector256.Equals(source, target).ExtractMostSignificantBits();

            while (mask != 0)
            {
                var bitPos = BitOperations.TrailingZeroCount(mask);
                uint slot = (index + (uint)bitPos) & _mask;

                if (_hasher.Equals(Unsafe.Add(ref entryStart, (nuint)slot).Key, key))
                {
                    return true;
                }

                mask = ResetLowestSetBit(mask);
            }

            if (source.ExtractMostSignificantBits() != 0) return false;

            index = (index + _groupWidth) & _mask;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Copy(DenseMap<TKey, TValue, THasher> other)
    {
        var otherControlBytes = other._controlBytes;
        var otherEntries = other._entries;
        var otherLength = other._length;

        for (int i = 0; i < otherLength; ++i)
        {
            if (otherControlBytes[i] > 0)
            {
                ref var entry = ref otherEntries[i];
                InsertOrUpdate(entry.Key, entry.Value);
            }
        }
    }

    public void Clear()
    {
        Array.Clear(_entries);
        _controlBytes.AsSpan().Fill(_emptyBucket);
        Count = 0;
        _tombstoneCounter = 0;
    }

    public TValue this[TKey key]
    {
        get
        {
            if (Get(key, out var result)) return result;
            throw new KeyNotFoundException($"Unable to find entry - {key.GetType().FullName} key - {key.GetHashCode()}");
        }
        set
        {
            if (!Update(key, value))
                throw new KeyNotFoundException($"Unable to find entry - {key.GetType().FullName} key - {key.GetHashCode()}");
        }
    }

    public void BeginBulkRemove() => _suppressRebuild = true;

    public void EndBulkRemove()
    {
        _suppressRebuild = false;
        if (_tombstoneCounter > _maxTombstoneBeforeRehash) Rebuild();
    }

    #endregion

    [StructLayout(LayoutKind.Sequential)]
    internal struct Entry
    {
        public TKey Key;
        public TValue Value;
    };
}