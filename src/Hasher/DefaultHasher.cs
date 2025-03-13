using Faster.Map.Contracts;
using System;
using System.Runtime.CompilerServices;

namespace Faster.Map.Hasher;

/// <summary>
/// Defines a hashing strategy for generating 32-bit hash values from a given key type.
/// Implementations should provide an optimized hashing function for specific key types.
/// </summary>
/// <typeparam name="T">The type of the key to be hashed.</typeparam>
public interface IHasherStrategy<T>
{
    /// <summary>
    /// Computes a 32-bit hash value for the given key.
    /// </summary>
    /// <param name="key">The key to hash.</param>
    /// <returns>A 32-bit unsigned integer representing the hash of the key.</returns>
    uint ComputeHash(T key);
}

/// <summary>
/// A default hashing strategy that utilizes the built-in <see cref="object.GetHashCode"/> method.
/// Provides a generic fallback for types without a specialized hasher.
/// </summary>
/// <typeparam name="T">The type of the key to be hashed.</typeparam>
public readonly struct DefaultHasher<T> : IHasherStrategy<T>
{
    /// <summary>
    /// Computes a hash for the given key using its <see cref="object.GetHashCode"/> method.
    /// </summary>
    /// <param name="key">The key to hash.</param>
    /// <returns>A 32-bit unsigned integer representing the hash of the key.</returns>
    /// <remarks>
    /// This implementation simply calls <see cref="object.GetHashCode"/> and casts it to <see cref="uint"/>.
    /// Be cautious when using this with types that return negative hash codes, as the cast can cause unintended wrapping.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ComputeHash(T key) => (uint)key.GetHashCode();
}

/// <summary>
/// A fast default hashing strategy for <see cref="ulong"/> values.
/// Utilizes strong mixing operations to produce high-entropy hash values.
/// </summary>
internal readonly struct DefaultUlongHasher : IHasherStrategy<ulong>
{
    /// <summary>
    /// Computes a high-quality hash for a given <see cref="ulong"/> value.
    /// </summary>
    /// <param name="x">The input value to hash.</param>
    /// <returns>A 32-bit hash derived from the input value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ComputeHash(ulong x)
    {
        // Multiplication by a high-entropy constant (Murmur-inspired)
        x *= 0xBF58476D1CE4E5B9UL;

        // XOR with its high bits to improve diffusion and avalanche effect
        x ^= x >> 56;

        // Another multiplication to further mix bits
        x *= 0x94D049BB133111EBUL;

        // Return the lower 32 bits as the final hash
        return (uint)x;
    }
}

/// <summary>
/// A fast default hashing strategy for <see cref="uint"/> values.
/// Uses bit shifts and multiplications with high-entropy constants
/// to produce a well-distributed hash.
/// </summary>
internal readonly struct DefaultUintHasher : IHasherStrategy<uint>
{
    /// <summary>
    /// Computes a high-quality hash for a given <see cref="uint"/> value.
    /// </summary>
    /// <param name="x">The input value to hash.</param>
    /// <returns>A 32-bit hash derived from the input value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ComputeHash(uint x)
    {
        // Initial XOR shift to mix high and low bits
        x ^= x >> 15;
        // Multiply by a high-entropy constant for mixing
        x *= 0x2C1B3C6D;
        // Another XOR shift to spread the bits
        x ^= x >> 13;
        // Another multiplication with a different high-entropy constant
        x *= 0x297A2D39;
        // Final XOR shift for additional diffusion
        x ^= x >> 15;
        return x;
    }
}