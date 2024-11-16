#if NET8_0_OR_GREATER

using Faster.Map.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using GxHash;

namespace Faster.Map.Hasher
{
    public class GxHasher : IHasher<string>
    {
        // https://github.com/ogxd/gxhash-csharp/blob/main/GxHash/

        // Arbitrary constants with high entropy. Hexadecimal digits of pi were used.        
        public const ulong ARBITRARY0 = 0x243f6a8885a308d3;
        public const ulong ARBITRARY1 = 0x13198a2e03707344;
        public const ulong ARBITRARY2 = 0xa4093822299f31d0;
        public const ulong ARBITRARY3 = 0x082efa98ec4e6c89;
        public const ulong ARBITRARY4 = 0x452821e638d01377;
        public const ulong ARBITRARY5 = 0xbe5466cf34e90c6c;
        public const ulong ARBITRARY6 = 0xc0ac29b7c97c50dd;
        public const ulong ARBITRARY7 = 0x3f84d5b5b5470917;
        public const ulong ARBITRARY8 = 0x9216d5d98979fb1b;
        public const ulong ARBITRARY9 = 0xd1310ba698dfb5ac;

        UInt128 _seed = (UInt128) 0x243f6a8885a308d3 * 0x3f84d5b5b5470917;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ComputeHash(string key)
        {
           return GxHash.GxHash.HashU64(MemoryMarshal.AsBytes(key.AsSpan()), _seed);     
        }      
    }
}
#endif