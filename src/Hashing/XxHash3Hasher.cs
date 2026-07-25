using Faster.Map.Contracts;
using System;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Faster.Map.Hashing;

/// <summary>
/// Provides high-performance hashing strategies based on the XXH3 algorithm.
/// </summary>
/// <remarks>
/// XXH3 is a state-of-the-art hashing algorithm that provides extreme throughput 
/// and excellent bit distribution, particularly effective for modern 64-bit CPUs.
/// </remarks>
public static class XxHash3Hasher
{
    /// <summary>
    /// High-performance XXH3-based hasher for unmanaged value types.
    /// </summary>
    /// <typeparam name="T">An unmanaged value type.</typeparam>
    public readonly struct Generic<T> : IHasher<T>
    {
        /// <summary>
        /// Computes a 32-bit hash. Uses XXH3 for value types without references.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(T key)
        {
            // JIT-time constant check: branch is eliminated for specific T types.
            if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                // To bypass the 'unmanaged' constraint of AsBytes, we cast the reference 
                // to a byte and create a span of the appropriate size.
                ReadOnlySpan<byte> span = MemoryMarshal.CreateReadOnlySpan(
                    ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in key)),
                    Unsafe.SizeOf<T>());

                return (uint)XxHash3.HashToUInt64(span);
            }

            // Fallback for reference types or structs containing references
            return (uint)EqualityComparer<T>.Default.GetHashCode(key!);
        }

        /// <summary>
        /// Determines equality between two values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(T x, T y)
        {
            if (typeof(T).IsValueType)
            {
                return x!.Equals(y);
            }

            return EqualityComparer<T>.Default.Equals(x!, y!);
        }

    }

    /// <summary>
    /// High-performance XXH3-based hasher for strings.
    /// </summary>
    public readonly struct String : IHasher<string>
    {
        /// <summary>
        /// Computes a 32-bit hash for a string using XXH3 over its UTF-16 bytes.
        /// </summary>
        /// <param name="key">The string to hash. Must not be null.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(string key)
        {
            // Access raw bytes of the string without allocation
            ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(key.AsSpan());
            return (uint)XxHash3.HashToUInt64(bytes);
        }

        /// <summary>
        /// Determines whether two strings are equal using ordinal comparison.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(string x, string y)
            => string.Equals(x, y, StringComparison.Ordinal);
    }

    /// <summary>
    /// 
    /// </summary>
    public readonly struct Uint : IHasher<uint>
    {
        /// <summary>
        /// Computes a 32-bit hash for the specified key using XXH3.
        /// </summary>
        /// <param name="key">The value to hash.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(uint key)
        {
            // Create a read-only span over the raw bytes of the value (zero-allocation)
            ReadOnlySpan<byte> span = MemoryMarshal.AsBytes(
                MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in key), 1));

            // Fold the 64-bit XXH3 result down to 32 bits for table indexing
            return (uint)XxHash3.HashToUInt64(span);
        }

        /// <summary>
        /// Determines whether two values are equal using direct value comparison.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(uint x, uint y) => x.Equals(y);

    }

    /// <summary>
    /// 
    /// </summary>
    public readonly struct Ulong : IHasher<ulong>
    {
        /// <summary>
        /// Computes a 32-bit hash for the specified key using XXH3.
        /// </summary>
        /// <param name="key">The value to hash.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(ulong key)
        {
            // Create a read-only span over the raw bytes of the value (zero-allocation)
            ReadOnlySpan<byte> span = MemoryMarshal.AsBytes(
                MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in key), 1));

            // Fold the 64-bit XXH3 result down to 32 bits for table indexing
            return (uint)XxHash3.HashToUInt64(span);
        }

        /// <summary>
        /// Determines whether two values are equal using direct value comparison.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ulong x, ulong y) => x.Equals(y);

    }

    /// <summary>
    /// High-performance XXH3-based hasher for <see cref="int"/> keys.
    /// </summary>
    public readonly struct Int : IHasher<int>
    {
        /// <summary>
        /// Computes a 32-bit hash for the specified key using XXH3.
        /// </summary>
        /// <param name="key">The value to hash.</param>
        /// <returns>A 32-bit hash derived from the raw bytes of the integer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(int key)
        {
            // Reinterpret int as uint to access its raw bytes via span without copying
            uint tempKey = Unsafe.As<int, uint>(ref key);

            // Create a read-only span over the raw bytes of the value (zero-allocation)
            ReadOnlySpan<byte> span = MemoryMarshal.AsBytes(
                MemoryMarshal.CreateReadOnlySpan(ref tempKey, 1));

            // Fold the 64-bit XXH3 result down to 32 bits for table indexing
            return (uint)XxHash3.HashToUInt64(span);
        }

        /// <summary>
        /// Determines whether two values are equal using direct value comparison.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(int x, int y) => x == y;
    }
}