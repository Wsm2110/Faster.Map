using Faster.Map.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Faster.Map.Hashing
{
    /// <summary>
    /// Provides ultra-high-performance hashing using hardware-accelerated CRC32 instructions.
    /// </summary>
    public static class CrcHasher
    {
        /// <summary>
        /// Hardware-accelerated hasher for 32-bit signed integers.
        /// </summary>
        public struct Int : IHasher<int>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public uint ComputeHash(int key)
            {
                // Absolute Limit: Direct hardware instruction with no Span overhead.
                // Unsafe.As avoids the cost of a standard cast by reinterpreting memory.
                return Sse42.Crc32(uint.MaxValue, Unsafe.As<int, uint>(ref key));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(int x, int y) => x == y;
        }

        /// <summary>
        /// Hardware-accelerated hasher for 32-bit unsigned integers.
        /// </summary>
        public struct UInt : IHasher<uint>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public uint ComputeHash(uint key)
            {
                // Absolute Limit: Direct hardware instruction (CRC32 r32, r/m32).
                return Sse42.Crc32(uint.MaxValue, key);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(uint x, uint y) => x == y;
        }

        /// <summary>
        /// Hardware-accelerated hasher for 64-bit unsigned integers.
        /// </summary>
        public struct Ulong : IHasher<ulong>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public uint ComputeHash(ulong key)
            {
                // Absolute Limit: Uses the 64-bit variant (CRC32 r64, r/m64).
                // This processes all 8 bytes in a single hardware cycle.
                return (uint)Sse42.X64.Crc32(uint.MaxValue, key);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(ulong x, ulong y) => x == y;
        }
    }
}