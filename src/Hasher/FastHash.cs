using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using X86Aes = System.Runtime.Intrinsics.X86.Aes;

namespace Faster.Map.Hasher
{
    public class FastHash
    {
        // Arbitrary constants with high entropy. Hexadecimal digits of pi were used.        
        public static readonly ulong Multiplier0 = 0x243f6a8885a308d3;
        public static readonly ulong Multiplier1 = 0x13198a2e03707344;
        public static readonly ulong Multiplier2 = 0xa4093822299f31d0;
        public static readonly ulong Multiplier3 = 0x082efa98ec4e6c89;
        public static readonly ulong Multiplier4 = 0x452821e638d01377;
        public static readonly ulong Multiplier5 = 0xbe5466cf34e90c6c;
        public static readonly ulong Multiplier6 = 0xc0ac29b7c97c50dd;
        public static readonly ulong Multiplier7 = 0x3f84d5b5b5470917;
        public static readonly ulong Multiplier8 = 0x9216d5d98979fb1b;
        public static readonly ulong Multiplier9 = 0xd1310ba698dfb5ac;

        private static readonly UInt128 _key1 = (UInt128)Multiplier1 * Multiplier0;
        private static readonly UInt128 _key2 = (UInt128)Multiplier2 * Multiplier3;
        private static readonly UInt128 _key3 = (UInt128)Multiplier4 * Multiplier5;
        private static readonly UInt128 _key4 = (UInt128)Multiplier6 * Multiplier7;
        private static readonly UInt128 _key5 = (UInt128)Multiplier8 * Multiplier1;

        private static Vector128<byte> _seedVector = Vector128<byte>.Zero;

        private static Vector128<byte> _keys1 = Unsafe.As<UInt128, Vector128<byte>>(ref _key1);
        private static Vector128<byte> _keys2 = Unsafe.As<UInt128, Vector128<byte>>(ref _key2);
        private static Vector128<byte> _keys3 = Unsafe.As<UInt128, Vector128<byte>>(ref _key3);
        private static Vector128<byte> _keys4 = Unsafe.As<UInt128, Vector128<byte>>(ref _key4);
        private static Vector128<byte> _keys5 = Unsafe.As<UInt128, Vector128<byte>>(ref _key5);
        private static Vector128<byte> _emptyVector = Vector128<byte>.Zero;

        private static readonly Vector128<byte> _indices = Vector128.Create((byte)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);

        /// <summary>
        /// Initializes a new instance of the Hasher class.
        /// Ensures the platform supports AES instructions and initializes the seed vector.
        /// </summary>
        static FastHash()
        {
            if (!X86Aes.IsSupported)
            {
                throw new PlatformNotSupportedException();
            }
        }

        /// <returns>A 64-bit hash value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateSeed(UInt128 source)
        {
            _seedVector = Unsafe.As<UInt128, Vector128<byte>>(ref source);
            _seedVector = X86Aes.Encrypt(_seedVector, _keys1);
            _seedVector = X86Aes.Encrypt(_seedVector, _keys2);
        }

        /// <summary>
        /// Computes a 64-bit hash value from the given input bytes.
        /// </summary>
        /// <remarks>
        /// - This method is optimized for raw performance, using a combination of 
        ///   compression and finalization steps to produce a hash value quickly.
        /// - The hashing process prioritizes speed over cryptographic security, 
        ///   making it suitable for non-security-critical scenarios such as hash-based 
        ///   lookups, caching, or deduplication.
        /// - It is less secure than cryptographic hash functions, as it may be more 
        ///   susceptible to collisions or deliberate manipulation.
        /// - Avoid using this method for scenarios requiring strong security guarantees, 
        ///   such as password hashing, digital signatures, or cryptographic protocols.
        /// </remarks>
        /// <param name="bytes">The input data to hash, provided as a read-only byte span.</param>
        /// <returns>A 64-bit hash value derived from the input bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong HashU64(ReadOnlySpan<byte> bytes)
        {
            return Finalize(Compress(bytes)).AsUInt64().GetElement(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong HashU64(uint source)
        {
            return X86Aes.Encrypt(Vector128.Create(source).AsByte(), _emptyVector).AsUInt64().GetElement(0);
        }

        /// <summary>
        /// Computes a secure 64-bit hash value from the given input bytes.
        /// </summary>
        /// <remarks>
        /// - This method is designed for scenarios where a balance between performance 
        ///   and enhanced security is required.
        /// - It uses compression and finalization steps to produce a hash value, while 
        ///   prioritizing the integrity of the process to reduce susceptibility to 
        ///   collisions or attacks.
        /// - The `[MethodImpl(MethodImplOptions.AggressiveInlining)]` attribute ensures 
        ///   the method is inlined for better performance, allowing it to remain efficient 
        ///   without compromising the underlying security mechanisms.
        /// - While it offers improved security compared to non-secure hashing methods, 
        ///   it may not meet the standards of cryptographic hash functions and should 
        ///   not be used for cryptographic purposes like password hashing or digital signatures.
        /// </remarks>
        /// <param name="bytes">The input data to hash, provided as a read-only byte span.</param>
        /// <returns>A secure 64-bit hash value derived from the input bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong HashU64Secure(ReadOnlySpan<byte> bytes)
        {
            return FinalizeSecure(Compress(bytes)).AsUInt64().GetElement(0);
        }

        /// <summary>
        /// Finalizes the hash computation by applying encryption rounds.
        /// </summary>
        /// <remarks>
        /// The empty vector (_emptyVector) is used as a key in the AES encryption process
        /// to leverage hardware-level optimizations (e.g., AES-NI). Zero vectors are 
        /// computationally efficient to process because:
        /// - XOR operations with zero introduce no additional complexity.
        /// - Modern CPUs can optimize specific patterns like all-zero inputs, 
        ///   reducing computational overhead and improving performance.
        /// 
        /// Using an empty vector in this context simplifies the encryption steps 
        /// while still producing a valid and unique encrypted result due to the 
        /// deterministic transformations applied by the AES algorithm. 
        /// This approach is particularly useful in scenarios where security is not 
        /// compromised by the lack of entropy in the vector itself, and performance 
        /// is a critical factor.
        /// </remarks>
        /// <param name="input">The input vector to finalize.</param>
        /// <returns>A finalized hash vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<byte> Finalize(Vector128<byte> input)
        {
            input = X86Aes.Encrypt(input, _emptyVector);
            input = X86Aes.EncryptLast(input, _emptyVector);
            return input;
        }

        /// <summary>
        /// Performs a sequence of AES encryption operations to finalize the transformation of the input vector.
        /// </summary>
        /// <remarks>
        /// - The method uses a seed vector (_seedVector) as part of the initial encryption step. 
        ///   The seed introduces additional variability into the encryption process, ensuring 
        ///   that the output is highly dependent on both the seed and the input data.
        /// - Subsequent encryption steps use predefined keys (_keys3 and _keys4) to further 
        ///   transform the input vector, maintaining the cryptographic strength of the process.
        /// - The use of a seed is critical for enhancing security, as it ensures that even 
        ///   similar inputs produce distinct outputs, preventing predictable patterns.
        /// - The final encryption step utilizes `EncryptLast`, optimized for concluding 
        ///   the encryption process efficiently, providing a robust final output.
        /// </remarks>
        /// <param name="input">The input vector to be processed through the AES encryption sequence.</param>
        /// <returns>The fully encrypted vector after applying the seed and subsequent transformations.</returns>
        private static Vector128<byte> FinalizeSecure(Vector128<byte> input)
        {
            input = X86Aes.Encrypt(input, _seedVector);
            input = X86Aes.Encrypt(input, _keys3);
            input = X86Aes.EncryptLast(input, _keys4);
            return input;
        }

        /// <summary>
        /// Compresses the input byte span into a single vector.
        /// </summary>
        /// <param name="source">The input byte span to compress.</param>
        /// <returns>A compressed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<byte> Compress(ReadOnlySpan<byte> source)
        {
            ref var ptr = ref Unsafe.As<byte, Vector128<byte>>(ref MemoryMarshal.GetReference(source));
            int len = source.Length;

            if (len <= 16)
            {
                return GetPartialVector(ref ptr, Unsafe.As<int, byte>(ref len));
            }

            if (len < 32)
            {
                Vector128<byte> hashVector = ptr;
                ptr = ref Unsafe.AddByteOffset(ref ptr, len & 15);
                return CompressBlock(hashVector, ptr);
            }

            if (len < 48)
            {
                Vector128<byte> hashVector = ptr;
                ptr = ref Unsafe.AddByteOffset(ref ptr, 16);
                return CompressBlock(CompressBlock(hashVector, ptr), Unsafe.AddByteOffset(ref ptr, len & 15));
            }

            return CompressMultipleBlocks(source);
        }

        /// <summary>
        /// Compresses two input vectors using AES operations.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>A compressed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<byte> CompressBlock(Vector128<byte> a, Vector128<byte> b)
        {
            return X86Aes.Encrypt(a, b);
        }

        /// <summary>
        /// Compresses multiple 16-byte blocks from the input span.
        /// </summary>
        /// <param name="source">The input byte span to compress.</param>
        /// <returns>A compressed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<byte> CompressMultipleBlocks(ReadOnlySpan<byte> source)
        {
            ref var start = ref MemoryMarshal.GetReference(source);
            ref var end = ref Unsafe.Add(ref start, source.Length);

            Vector128<byte> hashVector = Vector128<byte>.Zero;

            while (Unsafe.IsAddressLessThan(ref start, ref Unsafe.Subtract(ref end, 16)))
            {
                ref var currentBlock = ref Unsafe.As<byte, Vector128<byte>>(ref start);
                hashVector = CompressBlock(hashVector, currentBlock);
                start = ref Unsafe.Add(ref start, 16);
            }

            int remainingBytes = (int)(Unsafe.ByteOffset(ref start, ref end));
            if (remainingBytes > 0)
            {
                ref var remainingBlock = ref Unsafe.As<byte, Vector128<byte>>(ref start);
                Vector128<byte> partial = GetPartialVector(ref remainingBlock, Unsafe.As<int, byte>(ref remainingBytes));
                hashVector = CompressBlock(hashVector, partial);
            }

            return hashVector;
        }

        /// <summary>
        /// Creates a partial vector from the remaining bytes of the input.
        /// </summary>
        /// <param name="start">Reference to the start of the vector.</param>
        /// <param name="remainingBytes">The number of remaining bytes.</param>
        /// <returns>A partial vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<byte> GetPartialVector(ref Vector128<byte> start, byte remainingBytes)
        {
            if (IsReadBeyondSafe(ref start))
            {
                return GetPartialVectorUnsafe(ref start, remainingBytes);
            }

            return GetPartialVectorSafe(ref start, remainingBytes);
        }

        /// <summary>
        /// Safely creates a partial vector from the remaining bytes.
        /// </summary>
        /// <param name="start">Reference to the start of the vector.</param>
        /// <param name="remainingBytes">The number of remaining bytes.</param>
        /// <returns>A partial vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<byte> GetPartialVectorSafe(ref Vector128<byte> start, int remainingBytes)
        {
            Vector128<byte> input = Vector128<byte>.Zero;
            ref byte source = ref Unsafe.As<Vector128<byte>, byte>(ref start);
            ref byte dest = ref Unsafe.As<Vector128<byte>, byte>(ref input);
            Unsafe.CopyBlockUnaligned(ref dest, ref source, (uint)remainingBytes);

            return input;
        }

        /// <summary>
        /// Creates a partial vector using unsafe memory operations.
        /// </summary>
        /// <param name="start">Reference to the start of the vector.</param>
        /// <param name="remainingBytes">The number of remaining bytes.</param>
        /// <returns>A partial vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<byte> GetPartialVectorUnsafe(ref Vector128<byte> start, byte remainingBytes)
        {
            var mask = Vector128.GreaterThan(Vector128.Create(remainingBytes), _indices).AsByte();
            return Vector128.BitwiseAnd(mask, start);
        }

        /// <summary>
        /// Checks if reading the reference vector would exceed the page boundary.
        /// </summary>
        /// <param name="reference">Reference to the vector.</param>
        /// <returns>True if the read is safe; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool IsReadBeyondSafe(ref Vector128<byte> reference)
        {
            const int PAGE_SIZE = 0x1000;
            ulong address = (ulong)Unsafe.AsPointer(ref reference);
            ulong offsetWithinPage = address & (ulong)(PAGE_SIZE - 1);

            return offsetWithinPage <= (ulong)(PAGE_SIZE - Vector128<byte>.Count);
        }
    }
}
