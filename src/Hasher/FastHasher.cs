using Faster.Map.Contracts;
using Faster.Map.Hash;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Faster.Map.Hasher
{
    /// <summary>
    /// Generic implementation of a fast hasher for unmanaged types.
    /// Uses memory marshaling to compute a hash for the given key.
    /// </summary>
    /// <typeparam name="T">The type of the key to hash, must be unmanaged.</typeparam>
    public class FastHasher<T> : IHasher<T> where T : unmanaged
    {
        /// <summary>
        /// Computes a hash for the given key using the FastHash library.
        /// </summary>
        /// <param name="key">The key to hash.</param>
        /// <returns>A 64-bit hash value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ComputeHash(T key)
        {
            var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in key), 1));
            return FastHash.HashU64(span).AsUInt64().GetElement(0);
        }
    }

    /// <summary>
    /// Specialized implementation of a fast hasher for the <c>ulong</c> type.
    /// </summary>
    public class FastHasherUlong : IHasher<ulong>
    {
        /// <summary>
        /// Computes a hash for a 64-bit unsigned integer using the FastHash library.
        /// </summary>
        /// <param name="key">The <c>ulong</c> key to hash.</param>
        /// <returns>A 64-bit hash value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ComputeHash(ulong key)
        {
            return FastHash.HashU64(key).AsUInt64().GetElement(0);
        }
    }

    /// <summary>
    /// Specialized implementation of a fast hasher for the <c>uint</c> type.
    /// </summary>
    public class FastHasherUint : IHasher<uint>
    {
        /// <summary>
        /// Computes a hash for a 32-bit unsigned integer using the FastHash library.
        /// </summary>
        /// <param name="key">The <c>uint</c> key to hash.</param>
        /// <returns>A 64-bit hash value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ComputeHash(uint key)
        {
            return FastHash.HashU64(key).AsUInt64().GetElement(0);
        }
    }

    /// <summary>
    /// Specialized implementation of a fast hasher for the <c>string</c> type.
    /// </summary>
    public class FastHasher : IHasher<string>
    {
        /// <summary>
        /// Computes a hash for a string using the FastHash library.
        /// Converts the string to a span of bytes before hashing.
        /// </summary>
        /// <param name="key">The string key to hash.</param>
        /// <returns>A 64-bit hash value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ComputeHash(string key)
        {
            var result = FastHash.HashU64(MemoryMarshal.AsBytes(key.AsSpan()));
            return result.AsUInt64().GetElement(0);
        }
    }
}
