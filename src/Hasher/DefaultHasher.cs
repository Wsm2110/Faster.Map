using Faster.Map.Contracts;
using System.Runtime.CompilerServices;

namespace Faster.Map.Hasher
{
    internal class DefaultHasher<T> : IHasher<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public ulong ComputeHash(T key) => (uint)key.GetHashCode();

    }
}
