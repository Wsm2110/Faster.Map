namespace Faster.Map.Contracts
{
    public interface IHasher<in TKey>
    {
        uint ComputeHash(TKey key);
    }
}
