using System;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Faster.Map.Contracts;

namespace Faster.Map.Hasher
{
    public class StringHasher : IHasher<string>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(string key)
        {
            var span = key.AsSpan();
            var result = XxHash3.Hash(MemoryMarshal.AsBytes(span));
            return Unsafe.ReadUnaligned<uint>(ref result[0]);
        }
    }
}
