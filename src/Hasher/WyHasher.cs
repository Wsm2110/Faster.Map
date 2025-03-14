using Faster.Map.Hash;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Faster.Map.Hasher
{
    public readonly struct WyHasher : IHasher<string>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(string key)
        {
            return (uint)WyHash.Hash(MemoryMarshal.AsBytes(key.AsSpan()));
        }
    }
}
