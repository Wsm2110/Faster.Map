using System.Runtime.CompilerServices;
using Faster.Map.Contracts;

namespace Faster.Map.Hasher
{
    public class DefaultHasher<TKey> : IHasher<TKey>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(TKey key) => (uint)key.GetHashCode();
    }
}
