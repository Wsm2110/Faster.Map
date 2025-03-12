using System.Runtime.CompilerServices;

namespace Faster.Map.Contracts
{
    public interface IHasher<in TKey>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        uint ComputeHash(TKey key);            

    }
}
