using System;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Faster.Map.Contracts;

namespace Faster.Map.Hasher
{
    public class XxHash3StringHasher : IHasher<string>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(string key)
        {
            var result = XxHash3.HashToUInt64(MemoryMarshal.AsBytes(key.AsSpan())) >> 32;
            return Unsafe.As<ulong, uint>(ref result);
        }
    }
}
