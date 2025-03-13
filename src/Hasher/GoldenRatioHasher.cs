using System.Runtime.CompilerServices;
using Faster.Map.Contracts;

namespace Faster.Map.Hasher
{
    public readonly struct GoldenRatioHasher<TKey> : IHasherStrategy<TKey>
    {
        const uint _goldenRatio = 0x9E3779B9;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(TKey key)
        {
            return (uint)key.GetHashCode() * _goldenRatio;
        }
    }
}
