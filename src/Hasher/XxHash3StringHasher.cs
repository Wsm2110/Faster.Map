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
        public ulong ComputeHash(string key)
        {        
            return XxHash3.HashToUInt64(MemoryMarshal.AsBytes(key.AsSpan()));        
        }
    }
}
