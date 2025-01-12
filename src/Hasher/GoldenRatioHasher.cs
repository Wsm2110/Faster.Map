using System.Runtime.CompilerServices;
using Faster.Map.Contracts;

namespace Faster.Map.Hasher
{
    public class GoldenRatioHasher<TKey> : IHasher<TKey>
    {
        ulong _goldenRatio = 11400714819323198485;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ComputeHash(TKey key)
        {
            return ((uint)key.GetHashCode()) * _goldenRatio;
        }     
    }
}
