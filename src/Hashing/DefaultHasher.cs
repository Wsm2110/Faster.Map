using Faster.Map.Contracts;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Faster.Map.Hashing;

/// <summary>
/// Provides high-quality software-based hashing strategies for common types.
/// </summary>
/// <remarks>
/// These hashers serve as robust defaults, utilizing multiplicative mixing and 
/// bit-folding to ensure high entropy and low collision rates.
/// </remarks>
public static class DefaultHasher
{
    /// <summary>
    /// A generic fallback hashing strategy that delegates to
    /// <see cref="EqualityComparer{T}.Default"/>.
    /// </summary>
    /// <typeparam name="T">The type of the key to be hashed.</typeparam>
    public readonly struct Generic<T> : IHasher<T>
    {
        /// <summary>
        /// Computes a hash code for the specified key.
        /// </summary>
        /// <remarks>
        /// Using EqualityComparer{T}.Default ensures the JIT can devirtualize 
        /// the call for primitive types and avoid boxing for value types.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(T key)
        {
            // EqualityComparer<T>.Default is a JIT intrinsic. 
            // For value types, this becomes a direct non-boxing call.
            return (uint)EqualityComparer<T>.Default.GetHashCode(key!);
        }

        /// <summary>
        /// Determines whether two keys are equal.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(T x, T y)
        {
            // For value types (int, double, structs), the JIT recognizes this pattern
            // and emits a direct 'ceq' instruction or a non-virtual Equals call.
            if (typeof(T).IsValueType)
            {
                return x!.Equals(y);
            }

            // Fallback for reference types
            return EqualityComparer<T>.Default.Equals(x!, y!);
        }
    }

    /// <summary>
    /// A high-performance hashing strategy for <see cref="uint"/> values.
    /// </summary>
    public readonly struct UInt : IHasher<uint>
    {
        /// <summary>
        /// Computes a well-distributed hash for a 32-bit unsigned integer using an XMX-style mixer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(uint x)
        {
            x ^= x >> 15;
            x *= 0x85ebca6b;
            x ^= x >> 13;
            x *= 0xc2b2ae35;
            x ^= x >> 16;
            return x;
        }

        /// <summary>
        /// Performs a direct equality comparison between two <see cref="uint"/> values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(uint x, uint y) => x == y;
    }

    /// <summary>
    /// A high-quality hashing strategy for <see cref="ulong"/> values.
    /// </summary>
    public readonly struct Ulong : IHasher<ulong>
    {
        /// <summary>
        /// Computes a high-entropy 32-bit hash from a 64-bit input value using multiplicative mixing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(ulong x)
        {
            x *= 0xBF58476D1CE4E5B9UL;
            x ^= x >> 56;
            x *= 0x94D049BB133111EBUL;
            return (uint)x;
        }

        /// <summary>
        /// Performs a direct equality comparison between two <see cref="ulong"/> values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ulong x, ulong y) => x == y;

    }

    /// <summary>
    /// A high-performance hashing strategy for <see cref="int"/> values.
    /// </summary>
    public readonly struct Int : IHasher<int>
    {
        /// <summary>
        /// Computes a well-distributed hash for a 32-bit signed integer using an XMX-style mixer.
        /// </summary>
        /// <param name="key">The integer key to hash.</param>
        /// <returns>A mixed 32-bit unsigned hash code.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(int key)
        {
            // Reinterpret bits as uint to avoid sign-extension overhead
            uint x = Unsafe.As<int, uint>(ref key);

            x ^= x >> 15;
            x *= 0x85ebca6b;
            x ^= x >> 13;
            x *= 0xc2b2ae35;
            x ^= x >> 16;
            return x;
        }

        /// <summary>
        /// Performs a direct equality comparison between two <see cref="int"/> values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(int x, int y) => x == y;

        /// <summary>
        /// Explicit implementation for <see cref="IHasher{T}.ComputeHash(T)"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        uint IHasher<int>.ComputeHash(int key) => ComputeHash(key);

        /// <summary>
        /// Explicit implementation for <see cref="IHasher{T}.Equals(T, T)"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IHasher<int>.Equals(int x, int y) => Equals(x, y);
    }
}