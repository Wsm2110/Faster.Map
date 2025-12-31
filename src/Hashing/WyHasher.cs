using Faster.Map.Contracts;
using Faster.Map.Hashing.Algorithm;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Faster.Map.Hashing;

/// <summary>
/// High-performance string hasher based on the WyHash algorithm.
///
/// This hasher operates directly on the UTF-16 byte representation of strings
/// and uses ordinal equality semantics. It is optimized for speed, determinism,
/// and low collision rates in hash table workloads.
/// </summary>
/// <remarks>
/// <para>
/// Hashing is performed over the raw UTF-16 bytes of the string without allocation.
/// </para>
/// <para>
/// Equality is defined using ordinal comparison. As a result, hash and equality
/// semantics are fully consistent: two strings that are ordinal-equal will always
/// produce the same hash.
/// </para>
/// </remarks>
public readonly struct WyHasher : IHasher<string>
{
    /// <summary>
    /// Computes a 32-bit hash for the specified string using WyHash.
    /// </summary>
    /// <param name="key">The string to hash. Must not be <see langword="null"/>.</param>
    /// <returns>A 32-bit hash derived from the string's UTF-16 byte representation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ComputeHash(string key)
    {
        // Hash the raw UTF-16 bytes of the string (allocation-free)
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(key.AsSpan());

        // WyHash returns a 64-bit value; fold to 32-bit for table indexing
        return (uint)WyHash.Hash(bytes);
    }

    /// <summary>
    /// Determines whether two strings are equal using ordinal comparison.
    /// </summary>
    /// <remarks>
    /// This comparison is culture-invariant, allocation-free, and suitable
    /// for performance-critical hash table operations.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(string x, string y)
        => string.Equals(x, y, StringComparison.Ordinal);
}

public readonly struct WyHasherUint : IHasher<uint>
{
    /// <summary>
    /// Computes a 32-bit hash for the specified string using WyHash.
    /// </summary>
    /// <param name="key">The string to hash. Must not be <see langword="null"/>.</param>
    /// <returns>A 32-bit hash derived from the string's UTF-16 byte representation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ComputeHash(uint key)
    {     
        // WyHash returns a 64-bit value; fold to 32-bit for table indexing
        return (uint)WyHash.Hash(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref key, 1)));
    }

    /// <summary>
    /// Determines whether two strings are equal using ordinal comparison.
    /// </summary>
    /// <remarks>
    /// This comparison is culture-invariant, allocation-free, and suitable
    /// for performance-critical hash table operations.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(uint x, uint y)
        => x == y;
}