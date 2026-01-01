using Faster.Map.Contracts;
using Faster.Map.Hashing.Algorithm;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Faster.Map.Hashing;

/// <summary>
/// Provides high-performance hashing strategies based on the WyHash algorithm.
/// </summary>
/// <remarks>
/// WyHash is a fast, non-cryptographic hash function that offers excellent 
/// distribution and avalanche properties for hash table indexing.
/// </remarks>
public static class WyHasher
{
    /// <summary>
    /// A high-performance string hasher based on the WyHash algorithm.
    /// </summary>
    public readonly struct String : IHasher<string>
    {
        /// <summary>
        /// Computes a 32-bit hash for the specified string using WyHash.
        /// </summary>
        /// <param name="key">The string to hash. Must not be null.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(string key)
        {
            // Hash the raw UTF-16 bytes of the string (allocation-free)
            ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(key.AsSpan());

            // WyHash returns a 64-bit value; cast to 32-bit for table indexing
            return (uint)WyHash.Hash(bytes);
        }

        /// <summary>
        /// Determines whether two strings are equal using ordinal comparison.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(string x, string y)
            => string.Equals(x, y, StringComparison.Ordinal);
          
    }

    /// <summary>
    /// A high-performance hashing strategy for <see cref="uint"/> keys using WyHash.
    /// </summary>
    public readonly struct UInt : IHasher<uint>
    {
        /// <summary>
        /// Computes a 32-bit hash for the specified uint using WyHash.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(uint key)
        {
            // Treat the uint as a 4-byte span to feed into the WyHash engine
            return (uint)WyHash.Hash(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref key, 1)));
        }

        /// <summary>
        /// Performs a direct equality comparison between two <see cref="uint"/> values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(uint x, uint y) => x == y;
    }

    /// <summary>
    /// A high-performance hashing strategy for <see cref="uint"/> keys using WyHash.
    /// </summary>
    public readonly struct Ulong : IHasher<ulong>
    {
        /// <summary>
        /// Computes a 32-bit hash for the specified uint using WyHash.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(ulong key)
        {        
            return (uint)WyHash.Hash(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref key, 1)));
        }

        /// <summary>
        /// Performs a direct equality comparison between two <see cref="uint"/> values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ulong x, ulong y) => x == y;
    }

}