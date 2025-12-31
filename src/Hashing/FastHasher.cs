using Faster.Map.Contracts;
using Faster.Map.Hashing.Algorithm;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Faster.Map.Hashing;

/// <summary>
/// Specialized high-performance hasher for <see cref="uint"/> keys.
///
/// Uses FastHash to produce a well-mixed hash while relying on
/// direct value comparison for equality.
/// </summary>
public readonly struct FastHasherUint : IHasher<uint>
{
    /// <summary>
    /// Computes a 32-bit hash for a <see cref="uint"/> key using FastHash.
    /// </summary>
    /// <param name="key">The key to hash.</param>
    /// <returns>A well-distributed 32-bit hash.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ComputeHash(uint key)
        => FastHash.HashU64(key).AsUInt32().GetElement(0);

    /// <summary>
    /// Performs a direct equality comparison between two <see cref="uint"/> values.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(uint x, uint y) => x == y;
}

public readonly struct FastHasherUlong : IHasher<ulong>
{
    /// <summary>
    /// Computes a 32-bit hash for a <see cref="uint"/> key using FastHash.
    /// </summary>
    /// <param name="key">The key to hash.</param>
    /// <returns>A well-distributed 32-bit hash.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ComputeHash(ulong key)
        => FastHash.HashU64(key).AsUInt32().GetElement(0);

    /// <summary>
    /// Performs a direct equality comparison between two <see cref="uint"/> values.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ulong x, ulong y) => x == y;
}

/// <summary>
/// High-performance ordinal string hasher backed by FastHash.
///
/// Uses byte-level hashing over the UTF-16 representation of the string
/// and performs equality using ordinal comparison.
/// </summary>
/// <remarks>
/// This hasher is optimized for performance and determinism.
/// It intentionally avoids culture-aware or case-insensitive semantics.
/// </remarks>
public readonly struct FastHasherString : IHasher<string>
{
    /// <summary>
    /// Computes a 32-bit hash for a string by hashing its UTF-16 byte representation.
    /// </summary>
    /// <param name="key">The string to hash. Must not be <see langword="null"/>.</param>
    /// <returns>A well-distributed 32-bit hash.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ComputeHash(string key)
    {
        // Hash the raw UTF-16 bytes of the string (no allocation)
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(key.AsSpan());
        return FastHash.HashU64(bytes).AsUInt32().GetElement(0);
    }

    /// <summary>
    /// Determines whether two strings are equal using ordinal comparison.
    /// </summary>
    /// <remarks>
    /// This comparison is non-virtual, culture-invariant, and suitable
    /// for use in performance-critical hash table operations.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(string x, string y)
        => string.Equals(x, y, StringComparison.Ordinal);
}

