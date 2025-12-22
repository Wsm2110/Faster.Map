using System;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Faster.Map.Contracts;

namespace Faster.Map.Hasher
{
    #region XxHash3Hasher<T>

    /// <summary>
    /// High-performance XXH3-based hasher for unmanaged value types.
    ///
    /// Hashing is performed over the raw in-memory byte representation of
    /// <typeparamref name="T"/> without allocation or copying.
    /// </summary>
    /// <typeparam name="T">
    /// An unmanaged value type. The hash is computed over its binary layout.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// Equality is defined as bitwise equality (<c>x == y</c>), which is valid
    /// for unmanaged value types.
    /// </para>
    /// <para>
    /// The hash is stable only if the binary layout of <typeparamref name="T"/>
    /// is stable. This hasher should not be used with types that contain padding
    /// bytes with undefined values.
    /// </para>
    /// </remarks>
    public readonly struct XxHash3Hasher<T> : IHasher<T>
        where T : unmanaged
    {
        /// <summary>
        /// Computes a 32-bit hash for the specified key using XXH3.
        /// </summary>
        /// <param name="key">The value to hash.</param>
        /// <returns>
        /// A 32-bit hash derived from the raw bytes of <typeparamref name="T"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(T key)
        {
            // Create a read-only span over the raw bytes of the value
            ReadOnlySpan<byte> span =
                MemoryMarshal.AsBytes(
                    MemoryMarshal.CreateReadOnlySpan(
                        ref Unsafe.AsRef(in key), 1));

            // Fold the 64-bit XXH3 result down to 32 bits
            return (uint)XxHash3.HashToUInt64(span);
        }

        /// <summary>
        /// Determines whether two values are equal using direct value comparison.
        /// </summary>
        /// <remarks>
        /// For unmanaged types, this compiles to an efficient, non-virtual comparison.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(T x, T y) => x.Equals(y);
    }

    #endregion

    #region XxHash3StringHasher

    /// <summary>
    /// High-performance XXH3-based hasher for strings.
    ///
    /// Hashing is performed over the UTF-16 byte representation of the string,
    /// and equality is defined using ordinal comparison.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This hasher is culture-invariant, allocation-free, and deterministic.
    /// </para>
    /// <para>
    /// Hash and equality semantics are consistent: two strings that are
    /// ordinal-equal will always produce the same hash.
    /// </para>
    /// </remarks>
    public readonly struct XxHash3StringHasher : IHasher<string>
    {
        /// <summary>
        /// Computes a 32-bit hash for a string using XXH3.
        /// </summary>
        /// <param name="key">The string to hash. Must not be <see langword="null"/>.</param>
        /// <returns>A 32-bit hash derived from the string's UTF-16 bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(string key)
        {
            ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(key.AsSpan());
            return (uint)XxHash3.HashToUInt64(bytes);
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

    #endregion
}
