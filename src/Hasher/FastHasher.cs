using Faster.Map.Contracts;
using Faster.Map.Hash;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Faster.Map.Hasher;

/// <summary>
/// Specialized implementation of a fast hasher for the <c>uint</c> type.
/// </summary>
public readonly struct FastHasherUint : IHasher<uint>
{
    /// <summary>
    /// Computes a hash for a 32-bit unsigned integer using the FastHash library.
    /// </summary>
    /// <param name="key">The <c>uint</c> key to hash.</param>
    /// <returns>A 64-bit hash value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ComputeHash(uint key)
    {
        return FastHash.HashU64(key).AsUInt32().GetElement(0);
    }
}

public readonly struct FastHasherString : IHasher<string>
{
    /// <summary>
    /// Computes a hash for a 32-bit unsigned integer using the FastHash library.
    /// </summary>
    /// <param name="key">The <c>uint</c> key to hash.</param>
    /// <returns>A 64-bit hash value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ComputeHash(string key)
    {
        return FastHash.HashU64(MemoryMarshal.AsBytes(key.AsSpan())).AsUInt32().GetElement(0);
    }
}