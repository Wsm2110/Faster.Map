using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Faster.Map.Contracts;

namespace Faster.Map.Hasher;

/// <summary>
/// A generic fallback hasher that applies golden-ratio multiplicative mixing
/// to the default hash code of <typeparamref name="TKey"/>.
///
/// This hasher improves bit diffusion for poorly distributed hash codes
/// while preserving the standard equality semantics of the key type.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <remarks>
/// <para>
/// For value types, calls to <see cref="EqualityComparer{T}.Default"/> are typically
/// devirtualized and inlined by the JIT.
/// </para>
/// <para>
/// For reference types, hashing and equality may involve virtual dispatch.
/// Performance-critical scenarios should prefer a specialized hasher.
/// </para>
/// </remarks>
public readonly struct GoldenRatioHasher<TKey> : IHasher<TKey>
{
    /// <summary>
    /// The golden ratio constant (2³² / φ), commonly used for multiplicative hashing.
    /// </summary>
    private const uint GoldenRatio = 0x9E3779B9u;

    /// <summary>
    /// Computes a 32-bit hash for the specified key by applying
    /// golden-ratio multiplicative mixing to the default hash code.
    /// </summary>
    /// <param name="key">The key to hash.</param>
    /// <returns>A mixed 32-bit hash value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ComputeHash(TKey key)
    {
        // Use EqualityComparer to allow JIT intrinsics for value types
        uint hash = (uint)EqualityComparer<TKey>.Default.GetHashCode(key);

        // Golden-ratio multiplicative mixing to improve bit diffusion
        return hash * GoldenRatio;
    }

    /// <summary>
    /// Determines whether two keys are equal using the default equality comparer.
    /// </summary>
    /// <param name="x">The first key.</param>
    /// <param name="y">The second key.</param>
    /// <returns><see langword="true"/> if the keys are equal; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(TKey x, TKey y)
        => EqualityComparer<TKey>.Default.Equals(x, y);
}