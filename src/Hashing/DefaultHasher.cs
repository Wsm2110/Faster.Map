using Faster.Map.Contracts;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Faster.Map.Hashing;

/// <summary>
/// A generic fallback hashing strategy that delegates to
/// <see cref="EqualityComparer{T}.Default"/> for both hashing and equality.
///
/// This hasher prioritizes correctness and broad compatibility over absolute performance
/// and serves as the default when no specialized hasher is supplied.
/// </summary>
/// <typeparam name="T">
/// The type of the key to be hashed.
/// </typeparam>
/// <remarks>
/// <para>
/// For value types, calls to <see cref="EqualityComparer{T}.Default"/> are typically
/// devirtualized and inlined by the JIT.
/// </para>
/// <para>
/// For reference types, hashing and equality may involve virtual dispatch and therefore
/// incur additional overhead. Performance-critical code paths should prefer a specialized
/// hasher for such types.
/// </para>
/// </remarks>
public readonly struct DefaultHasher<T> : IHasher<T>
{
    /// <summary>
    /// Computes a hash code for the specified key using
    /// <see cref="EqualityComparer{T}.Default"/>.
    /// </summary>
    /// <param name="key">The key to hash.</param>
    /// <returns>
    /// A 32-bit unsigned hash code derived from the key.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ComputeHash(T key)
        => (uint)EqualityComparer<T>.Default.GetHashCode(key);

    /// <summary>
    /// Determines equality between two keys using
    /// <see cref="EqualityComparer{T}.Default"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(T x, T y)
        => EqualityComparer<T>.Default.Equals(x, y);
}

/// <summary>
/// A high-quality hashing strategy for <see cref="ulong"/> values.
///
/// Uses multiplicative mixing and bit folding inspired by Murmur-style finalizers
/// to achieve strong avalanche properties and low collision rates.
/// </summary>
internal readonly struct DefaultUlongHasher : IHasher<ulong>
{
    /// <summary>
    /// Computes a high-entropy 32-bit hash from a 64-bit input value.
    /// </summary>
    /// <param name="x">The value to hash.</param>
    /// <returns>A well-distributed 32-bit hash.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ComputeHash(ulong x)
    {
        // Multiply by a large odd constant to mix bits
        x *= 0xBF58476D1CE4E5B9UL;

        // Fold high bits into low bits to improve diffusion
        x ^= x >> 56;

        // Second mixing stage
        x *= 0x94D049BB133111EBUL;

        // Return lower 32 bits
        return (uint)x;
    }

    /// <summary>
    /// Performs a direct equality comparison between two <see cref="ulong"/> values.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ulong x, ulong y) => x == y;
}

/// <summary>
/// A high-performance hashing strategy for <see cref="uint"/> values.
///
/// Implements an XMX-style bit mixer to achieve good diffusion and avalanche behavior
/// while remaining inexpensive to compute.
/// </summary>
public readonly struct DefaultUintHasher : IHasher<uint>
{
    /// <summary>
    /// Computes a well-distributed hash for a 32-bit unsigned integer.
    /// </summary>
    /// <param name="x">The value to hash.</param>
    /// <returns>A mixed 32-bit hash value.</returns>
    /// <remarks>
    /// This mixer is inspired by common XMX-style finalizers and is suitable for
    /// hash table usage where speed and low collision rates are required.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ComputeHash(uint x)
    {
        x ^= x >> 15;
        x *= 0x85ebca6b;
        x ^= x >> 13;
        x *= 0xc2b2ae35;
        x ^= x >> 16;
        return x;


        // Initial XOR shift to mix high and low bits
        //x ^= x >> 15;
        // Multiply by a high-entropy constant for mixing
        //x *= 0x2C1B3C6D;
        // Another XOR shift to spread the bits
        //x ^= x >> 13;
        //// Another multiplication with a different high-entropy constant
        //x *= 0x297A2D39;
        // Final XOR shift for additional diffusion
        //x ^= x >> 15;
        //return x;
    }

    /// <summary>
    /// Performs a direct equality comparison between two <see cref="uint"/> values.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(uint x, uint y) => x == y;
}