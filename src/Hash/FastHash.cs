using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using X86Aes = System.Runtime.Intrinsics.X86.Aes;

namespace Faster.Map.Hash
{
    /// <summary>
    /// This hash is based on gxHash https://github.com/ogxd/gxhash-csharp/blob/main/GxHash
    /// </summary>
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

        private static Vector128<byte> _seedVector = Vector128<byte>.Zero;

        private static Vector128<byte> _keys1 = Unsafe.As<UInt128, Vector128<byte>>(ref _key1);
        private static Vector128<byte> _keys2 = Unsafe.As<UInt128, Vector128<byte>>(ref _key2);
        private static Vector128<byte> _keys3 = Unsafe.As<UInt128, Vector128<byte>>(ref _key3);
        private static Vector128<byte> _keys4 = Unsafe.As<UInt128, Vector128<byte>>(ref _key4);
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

        /// <summary>
        /// Creates a seed vector from a given source value using AES encryption.
        /// </summary>
        /// <param name="source">The initial seed source value of type UInt128.</param>
        /// <remarks>
        /// This method initializes the seed vector by converting the provided source value into a 
        /// SIMD vector (`Vector128<byte>`) for efficient processing. The seed vector is then 
        /// transformed through two rounds of AES encryption using predefined keys to enhance its 
        /// randomness and security. This is suitable for use in cryptographic or random number 
        /// generation scenarios.
        /// </remarks>
        public static void CreateSeed(UInt128 source)
        {
            // Convert the source UInt128 value to a Vector128<byte>
            _seedVector = Unsafe.As<UInt128, Vector128<byte>>(ref source);

            // Apply AES encryption twice using predefined keys for randomness and security
            _seedVector = X86Aes.Encrypt(_seedVector, _keys1); // First round of AES encryption
            _seedVector = X86Aes.Encrypt(_seedVector, _keys2); // Second round of AES encryption
        }

        /// <summary>
        /// Generates a seed value for cryptographic or random number generation purposes.
        /// </summary>
        /// <remarks>
        /// This method combines system-based entropy sources such as the current environment tick count 
        /// and UTC timestamp to produce an initial seed. The seed is then transformed using custom multipliers 
        /// for additional randomness. Finally, AES encryption is applied twice with predefined keys to further 
        /// enhance the randomness and security of the generated seed vector.
        /// </remarks>
        public static void CreateSeed()
        {
            // Generate a seed using system-based values
            UInt128 seed = (UInt128)Environment.TickCount64; // Use lower 64 bits
            seed ^= (UInt128)DateTime.UtcNow.Ticks;          // XOR with current ticks
            seed *= Multiplier9;       // Apply arbitrary folding
            seed *= Multiplier8;       // Fold again for better randomness

            _seedVector = Unsafe.As<UInt128, Vector128<byte>>(ref seed);
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
        public static Vector128<byte> HashU64(ReadOnlySpan<byte> bytes)
        {
            return Finalize(Compress(bytes));
        }

        /// <summary>
        /// Computes a 64-bit hash value for a given 32-bit unsigned integer using AES encryption.
        /// </summary>
        /// <param name="source">The 32-bit unsigned integer to hash.</param>
        /// /// <returns>A <see cref="Vector128{Byte}"/> containing the hashed value.
        /// Use <c>.AsUInt32().GetElement(0)</c> to extract the 32-bit result from the hash.</returns>
        /// <example>
        /// Example usage:
        /// <code>
        /// uint input = 123456;
        /// uint hash = HashU64(input).AsUInt32().GetElement(0);
        /// Console.WriteLine($"Hash: {hash}");
        /// </code>
        /// </example>
        /// <remarks>
        /// This method leverages hardware-accelerated AES encryption (via `X86Aes.Encrypt`) to generate
        /// a secure and efficient hash from the input. The 32-bit input is first expanded to a `Vector128<byte>`,
        /// encrypted with a predefined empty vector, and the resulting 64-bit value is extracted.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<byte> HashU64(uint source)
        {
            // Create a Vector128<byte> containing the source value as bytes
            var result = Vector128.Create(source);
            return X86Aes.EncryptLast(Unsafe.As<Vector128<uint>, Vector128<byte>>(ref result), _emptyVector);
        }

        /// <summary>
        /// Computes a hash for a 64-bit unsigned integer (ulong) using AES encryption.
        /// </summary>
        /// <param name="source">The 64-bit unsigned integer to hash.</param>
        /// <returns>A <see cref="Vector128{Byte}"/> containing the hashed value.
        /// Use <c>.AsUInt64().GetElement(0)</c> to extract the 64-bit result from the hash.</returns>
        /// <example>
        /// Example usage:
        /// <code>
        /// ulong input = 123456789;
        /// ulong hash = HashU64(input).AsUInt64().GetElement(0);
        /// Console.WriteLine($"Hash: {hash}");
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<byte> HashU64(ulong source)
        {
            // Create a Vector128<byte> containing the source value as bytes
            var result = Vector128.Create(source);
            return X86Aes.EncryptLast(Unsafe.As<Vector128<ulong>, Vector128<byte>>(ref result), _emptyVector);
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
        /// <summary>
        /// Compresses a byte span into a `Vector128<byte>` using optimized SIMD operations.
        /// </summary>
        /// <param name="source">The input span of bytes to compress.</param>
        /// <returns>
        /// A `Vector128<byte>` representing the compressed value of the input span.
        /// </returns>
        /// <remarks>
        /// This method uses different strategies depending on the length of the input span:
        /// - For spans less than or equal to 16 bytes, it processes them as a partial vector.
        /// - For spans between 17 and 31 bytes, it compresses two partial blocks.
        /// - For spans between 32 and 47 bytes, it compresses three blocks.
        /// - For spans of 48 bytes or more, it delegates to `CompressMultipleBlocks` for efficient handling of larger data.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<byte> Compress(ReadOnlySpan<byte> source)
        {
            // Obtain a reference to the span's first byte and reinterpret it as a Vector128<byte>
            ref var start = ref Unsafe.As<byte, Vector128<byte>>(ref MemoryMarshal.GetReference(source));
            int len = source.Length; // Get the length of the input span

            // Handle short spans (<= 16 bytes) as a single partial vector
            if (len <= 16)
            {
                // Create a mask where bytes at positions less than `remainingBytes` are set to 0xFF and others to 0x00
                var mask = Vector128.GreaterThan(Vector128.Create(Unsafe.As<int, byte>(ref len)), _indices);
                // Apply the mask to the source block to zero out bytes beyond `remainingBytes`
                return Vector128.BitwiseAnd(mask, start);
            }

            // Handle medium spans (17 to 31 bytes) by processing two partial blocks
            if (len < 32)
            {
                // Load the first block of data into the hash vector
                // `ptr` is assumed to reference the beginning of the first block
                Vector128<byte> hashVector = start;

                // Adjust the pointer to point to the second partial block
                // `16 - len & 15` ensures proper alignment for the remaining data:
                //   - `(len & 15)` gives the remainder of `len` divided by 16 (bytes left in the last incomplete block)
                //   - `16 - (len & 15)` computes the offset needed to reach the next aligned block boundary
                start = ref Unsafe.AddByteOffset(ref start, 16 - (len & 15));

                // Combine the two blocks: the first block and the second partial block
                // The `CompressBlock` function integrates the data into a final hash
                return CombineBlock(hashVector, start);
            }

            // Handle larger spans (32 to 47 bytes) by processing three blocks
            if (len < 48)
            {
                // Load the first block
                Vector128<byte> hashVector = start;

                // Move to the second block
                ref Vector128<byte> secondBlock = ref Unsafe.AddByteOffset(ref start, 16);

                // Compute the offset for the partial third block
                ref Vector128<byte> partialBlock = ref Unsafe.AddByteOffset(ref secondBlock, 32 - (len & 15));

                // Compress the blocks and return the result
                return CombineBlock(CombineBlock(hashVector, secondBlock), partialBlock);
            }

            // Delegate to CompressMultipleBlocks for spans of 48 bytes or more
            return CombineMultipleBlocks(ref start, source);
        }

        /// <summary>
        /// Compresses two `Vector128<byte>` inputs into a single `Vector128<byte>`
        /// using AES encryption.
        /// </summary>
        /// <param name="a">The first input vector to compress.</param>
        /// <param name="b">The second input vector to compress.</param>
        /// <returns>A `Vector128<byte>` representing the compressed result of the two input vectors.</returns>
        /// <remarks>
        /// This method utilizes the AES instruction set (via `X86Aes.Encrypt`) to combine
        /// the two input vectors. It assumes that the underlying hardware supports AES-NI
        /// instructions for optimal performance.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<byte> CombineBlock(Vector128<byte> a, Vector128<byte> b)
        {
            // Use AES encryption to combine the two input vectors
            return X86Aes.Encrypt(a, b);
        }

        /// <summary>
        /// Compresses multiple 16-byte blocks from the input span.
        /// </summary>
        /// <param name="source">The input byte span to compress.</param>
        /// <returns>A compressed vector.</returns>
        /// <summary>
        /// Compresses a span of data into a single `Vector128<byte>` value by processing multiple blocks using SIMD operations.
        /// </summary>
        /// <param name="start">
        /// A reference to the starting position of the data as a `Vector128<byte>`.
        /// </param>
        /// <param name="source">
        /// The input span of bytes to compress. Its length determines the number of blocks to process.
        /// </param>
        /// <returns>
        /// A `Vector128<byte>` representing the compressed result of all processed blocks in the input span.
        /// </returns>
        /// <remarks>
        /// This method processes the input data in chunks of 64 bytes at a time, where each chunk is divided
        /// into four 16-byte blocks (processed using `CompressBlock`). Any remaining data is handled separately:
        /// - Full blocks are processed individually.
        /// - Partial blocks (less than 16 bytes) are processed by creating a partial vector.
        ///
        /// The method is optimized for performance using SIMD and unsafe memory operations. It assumes the input
        /// span is appropriately aligned and uses low-level intrinsics for fast memory manipulation.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<byte> CombineMultipleBlocks(ref Vector128<byte> start, ReadOnlySpan<byte> source)
        {
            // Calculate the total number of blocks
            int totalBlocks = source.Length >> 4; // Each block is 16 bytes

            // Calculate the number of full groups
            int fullGroups = totalBlocks >> 2; // divide by 4

            // Calculate the end pointer for full groups
            ref var end = ref Unsafe.Add(ref start, fullGroups << 2); // multiply by 4

            Vector128<byte> hashVector = Vector128<byte>.Zero;

            // Loop until the pointer 'start' reaches the 'end' pointer
            while (Unsafe.IsAddressLessThan(ref start, ref end))
            {
                // Load the first block of data to initialize the blockHash
                Vector128<byte> blockHash = start;

                // Combine the next three blocks into the blockHash
                // CompressBlock applies a hashing/compression function on blockHash and the next block
                blockHash = CombineBlock(blockHash, Unsafe.Add(ref start, 1)); // Combine with the second block
                blockHash = CombineBlock(blockHash, Unsafe.Add(ref start, 2)); // Combine with the third block
                blockHash = CombineBlock(blockHash, Unsafe.Add(ref start, 3)); // Combine with the fourth block

                // Merge the blockHash result into the final hashVector
                // hashVector accumulates the combined results of all processed blocks
                hashVector = CombineBlock(hashVector, blockHash);

                // Advance the start pointer by 4 blocks (each block is 16 bytes)
                // This skips the 4 blocks just processed and prepares for the next iteration
                start = ref Unsafe.Add(ref start, 4);
            }

            // Adjust the `end` pointer to process remaining blocks (less than 4 blocks)
            // `totalBlocks & 3` computes the number of remaining blocks after dividing totalBlocks by 4
            end = ref Unsafe.Add(ref end, totalBlocks & 3);

            // Process the remaining full 16-byte blocks
            while (Unsafe.IsAddressLessThan(ref start, ref end))
            {
                // Compress the current block with a predefined key (_keys4)
                hashVector = CombineBlock(start, _keys4);

                // Move to the next block (advance by 16 bytes)
                start = ref Unsafe.Add(ref start, 1);
            }

            // Process remaining bytes (less than 16 bytes, not forming a full block)
            // `remainingBytes` computes the leftover bytes by performing a bitwise AND with 15 (equivalent to mod 16)
            int remainingBytes = source.Length & 15; // Equivalent to source.Length % 16
            if (remainingBytes > 0)
            {
                // Adjust the `start` pointer backward by the difference (16 - remainingBytes) to align the remaining bytes
                // This creates a 16-byte region for padding or alignment
                ref var alignedStart = ref Unsafe.SubtractByteOffset(ref start, 16 - remainingBytes);

                // Compress the remaining bytes (padded or aligned to 16 bytes) with the predefined key (_keys4)
                hashVector = CombineBlock(alignedStart, _keys4);
            }

            return hashVector;
        }
    }
}
