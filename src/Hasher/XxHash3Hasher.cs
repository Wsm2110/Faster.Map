using System;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Faster.Map.Contracts;

namespace Faster.Map.Hasher
{
    internal readonly struct XxHash3Hasher<T> : IHasher<T> where T : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(T key)
        {
            var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in key), 1));
            return (uint)XxHash3.HashToUInt64(span);
        }
    }
    public readonly struct XxHash3StringHasher : IHasher<string>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(string key)
        {
            return (uint)XxHash3.HashToUInt64(MemoryMarshal.AsBytes(key.AsSpan()));
        }
    }
}
