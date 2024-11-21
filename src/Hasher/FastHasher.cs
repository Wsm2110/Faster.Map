using Faster.Map.Contracts;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Faster.Map.Hasher
{
    public class FastHasher<T> : IHasher<T> where T: unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ComputeHash(T key)
        {
            var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in key), 1));

            return FastHash.HashU64(span);
        }
    }

    public class FastHasherUint : IHasher<uint> 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ComputeHash(uint key)
        {    
            return FastHash.HashU64(key);
        }
    }

    public class FastHasher : IHasher<string>
    {
        public FastHasher()
        {
         //  FastHash.CreateSeed((UInt128)FastHash.Multiplier8 * FastHash.Multiplier7);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ComputeHash(string key)
        {
            return FastHash.HashU64(MemoryMarshal.AsBytes(key.AsSpan()));
        }
    }
}
