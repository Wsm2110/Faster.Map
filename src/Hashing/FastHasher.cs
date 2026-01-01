using Faster.Map.Contracts;
using Faster.Map.Hashing.Algorithm;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Faster.Map.Hashing;

/// <summary>
/// Provides specialized high-performance hashing strategies using the FastHash algorithm.
/// </summary>
/// <remarks>
/// FastHash produces well-mixed 32-bit hashes with low collision rates, making it 
/// ideal for high-throughput hash table operations.
/// </remarks>
public static class FastHasher
{
    /// <summary>
    /// A high-performance hashing strategy for <see cref="uint"/> keys using FastHash.
    /// </summary>
    public readonly struct UInt : IHasher<uint>
    {
        /// <summary>
        /// Computes a 32-bit hash for a <see cref="uint"/> key.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(uint key)
            => FastHash.HashU64(key).AsUInt32().GetElement(0);

        /// <summary>
        /// Performs a direct equality comparison between two <see cref="uint"/> values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(uint x, uint y) => x == y;
    }

    /// <summary>
    /// A high-performance hashing strategy for <see cref="ulong"/> keys using FastHash.
    /// </summary>
    public readonly struct Ulong : IHasher<ulong>
    {
        /// <summary>
        /// Computes a 32-bit hash for a <see cref="ulong"/> key.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(ulong key)
            => FastHash.HashU64(key).AsUInt32().GetElement(0);

        /// <summary>
        /// Performs a direct equality comparison between two <see cref="ulong"/> values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ulong x, ulong y) => x == y;  
    }

    /// <summary>
    /// A high-performance ordinal string hasher backed by FastHash.
    /// </summary>
    public readonly struct String : IHasher<string>
    {
        /// <summary>
        /// Computes a 32-bit hash for a string by hashing its UTF-16 byte representation.
        /// </summary>
        /// <param name="key">The string to hash. Must not be null.</param>
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(string x, string y)
            => string.Equals(x, y, StringComparison.Ordinal);
    }
}