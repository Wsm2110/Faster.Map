namespace Faster.Map.Contracts
{
    public interface IHasher<in TKey>
    {
        ulong ComputeHash(TKey key);      
    }
}
