using System.Runtime.CompilerServices;

namespace Faster.Map.Contracts
{
    public interface IHasher<TKey>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        uint ComputeHash(TKey key);            

    }
}
