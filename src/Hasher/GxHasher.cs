#if NET8_0_OR_GREATER

using Faster.Map.Contracts;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Faster.Map.Hasher
{
    public class GxHasher<T> : IHasher<T> where T : unmanaged
    {
        private UInt128 _seed = (UInt128)0x9216d5d98979fb1b * 0xd1310ba698dfb5ac;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ComputeHash(T key)
        {
            var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in key), 1));

            return GxHash.GxHash.HashU64(span, _seed);

        }
    }


    public class GxHasher : IHasher<string>
    {
        private UInt128 _seed = (UInt128)0x9216d5d98979fb1b * 0xd1310ba698dfb5ac;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ComputeHash(string key)
        {
            // Compute hash using a custom or external algorithm
            return GxHash.GxHash.HashU64(MemoryMarshal.AsBytes(key.AsSpan()), _seed);
        }
    }
}

#endif