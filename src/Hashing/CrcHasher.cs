using Faster.Map.Contracts;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;

namespace Faster.Map.Hashing
{
    /// <summary>
    /// Provides ultra-high-performance hashing using hardware-accelerated CRC32 instructions,
    /// with optimized fallbacks for ARM64 and unsupported architectures.
    /// </summary>
    public static class CrcHasher
    {
        // Golden ratio constant for fast 32-bit software hashing fallback.
        private const uint GoldenRatio32 = 2654435761u;

        /// <summary>
        /// Hardware-accelerated hasher for 32-bit signed integers.
        /// </summary>
        public struct Int : IHasher<int>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public uint ComputeHash(int key)
            {
                if (Sse42.IsSupported)
                {
                    // Absolute Limit: Direct hardware instruction with no Span overhead.
                    return Sse42.Crc32(uint.MaxValue, Unsafe.As<int, uint>(ref key));
                }

                if (Crc32.IsSupported)
                {
                    // ARM64 hardware accelerated fallback.
                    return Crc32.ComputeCrc32C(uint.MaxValue, Unsafe.As<int, uint>(ref key));
                }

                // Software fallback utilizing Knuth's multiplicative hash.
                return Unsafe.As<int, uint>(ref key) * GoldenRatio32;
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
                if (Sse42.IsSupported)
                {
                    // Absolute Limit: Direct hardware instruction (CRC32 r32, r/m32).
                    return Sse42.Crc32(uint.MaxValue, key);
                }

                if (Crc32.IsSupported)
                {
                    // ARM64 hardware accelerated fallback.
                    return Crc32.ComputeCrc32C(uint.MaxValue, key);
                }

                // Software fallback utilizing Knuth's multiplicative hash.
                return key * GoldenRatio32;
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
                if (Sse42.X64.IsSupported)
                {
                    // Absolute Limit: Uses the 64-bit variant (CRC32 r64, r/m64).
                    // This processes all 8 bytes in a single hardware cycle.
                    return (uint)Sse42.X64.Crc32(uint.MaxValue, key);
                }

                if (Crc32.Arm64.IsSupported)
                {
                    // ARM64 hardware accelerated fallback.
                    return Crc32.Arm64.ComputeCrc32C(uint.MaxValue, key);
                }

                // Software fallback: Fold the 64-bit integer into 32 bits and multiply.
                uint folded = (uint)key ^ (uint)(key >> 32);
                return folded * GoldenRatio32;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(ulong x, ulong y) => x == y;
        }
    }
}