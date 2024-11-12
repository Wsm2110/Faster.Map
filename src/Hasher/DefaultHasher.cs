using System.Runtime.CompilerServices;
using Faster.Map.Contracts;

namespace Faster.Map.Hasher
{
    public class DefaultHasher<TKey> : IHasher<TKey>
    {
        ulong _goldenRatio = 11400714819323198485;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ComputeHash(TKey key) => (uint)key.GetHashCode() * _goldenRatio;
    }
}
