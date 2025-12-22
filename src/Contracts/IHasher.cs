using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Faster.Map.Contracts;

/// <summary>
/// Defines a high-performance hashing and equality strategy for <c>BlitzMap</c>.
///
/// Implementations are intended to be <see langword="struct"/>s and supplied as generic
/// type parameters in order to enable full static dispatch, aggressive inlining, and
/// elimination of interface and virtual-call overhead in hot paths.
///
/// This interface is purpose-built for performance-critical hash tables and deliberately
/// trades runtime flexibility (e.g. pluggable comparers) for compile-time specialization.
///
/// Unlike <see cref="IEqualityComparer{T}"/>, implementations of this interface are expected
/// to be deterministic, allocation-free, and suitable for use inside tight loops.
/// </summary>
/// <typeparam name="TKey">
/// The type of the keys being hashed and compared.
/// </typeparam>
public interface IHasher<TKey>
{
    /// <summary>
    /// Computes a 32-bit hash code for the specified key.
    ///
    /// The returned hash must be consistent with <see cref="Equals"/>:
    ///
    /// <list type="bullet">
    /// <item>If <c>Equals(x, y)</c> returns <see langword="true"/>, then
    /// <c>ComputeHash(x)</c> and <c>ComputeHash(y)</c> <b>must</b> return the same value.</item>
    /// <item>Unequal keys may produce identical hashes, but high-quality implementations
    /// should minimize collisions through good bit diffusion.</item>
    /// </list>
    ///
    /// This method is invoked on every lookup, insertion, and removal operation and is
    /// therefore on the critical hot path. Implementations must be allocation-free and
    /// suitable for aggressive inlining.
    /// </summary>
    /// <param name="key">The key to hash. Must not be <see langword="null"/>.</param>
    /// <returns>
    /// A 32-bit unsigned hash value suitable for bucket indexing and signature derivation.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    uint ComputeHash(TKey key);

    /// <summary>
    /// Determines whether two keys are considered equal.
    ///
    /// Implementations must obey the standard equality contract
    /// (reflexive, symmetric, and transitive).
    ///
    /// This method is on the hot path for all map operations and should be implemented
    /// using the cheapest comparison possible:
    ///
    /// <list type="bullet">
    /// <item>For value types, this typically maps to <c>==</c>.</item>
    /// <item>For reference types, implementations should avoid culture-aware,
    /// allocation-heavy, or polymorphic comparisons unless explicitly required.</item>
    /// </list>
    /// </summary>
    /// <param name="x">The first key to compare.</param>
    /// <param name="y">The second key to compare.</param>
    /// <returns>
    /// <see langword="true"/> if the keys are equal; otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool Equals(TKey x, TKey y);
}
