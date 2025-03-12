using Faster.Map.Contracts;
using System.Runtime.CompilerServices;

namespace Faster.Map.Hasher
{
    internal readonly struct DefaultHasher<T> : IHasher<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public uint ComputeHash(T key) => (uint)key.GetHashCode();
    }

    internal readonly struct DefaultUlongHasher : IHasher<ulong>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(ulong x)
        {
            x *= 0xBF58476D1CE4E5B9UL; // First multiplication step
            x ^= x >> 56;              // XOR with high bits for more diffusion
            x *= 0x94D049BB133111EBUL; // Second multiplication step
            return (uint)x;
        }
    }

    internal readonly struct DefaultUintHasher : IHasher<uint>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHash(uint x)
        {
            x ^= x >> 15;
            x *= 0x2c1b3c6d;  // High-entropy constant
            x ^= x >> 13;
            x *= 0x297a2d39;  // Another high-entropy constant
            x ^= x >> 15;
            return x;
        }
    }



}
