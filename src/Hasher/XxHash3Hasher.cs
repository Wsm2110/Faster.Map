using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Faster.Map.Contracts;

namespace Faster.Map.Hasher
{
    public class XxHash3Hasher<T> : IHasher<T> where T : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(T key)
        {
            var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(key), 1));
            var result = XxHash3.HashToUInt64(MemoryMarshal.AsBytes(span)) >> 32;
            return Unsafe.As<ulong, uint>(ref result);         
        }
    }
}
