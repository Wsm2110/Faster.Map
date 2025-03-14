using System;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Text;
using Xunit;
using Faster.Map.Hash;

namespace Faster.Map.Hash.Tests;

public class FastHashTests
{
    #region Utility Methods

    /// <summary>
    /// Generates a byte array with repeating patterns of a given size.
    /// </summary>
    private static byte[] GeneratePattern(int size, byte pattern = 0xAA)
    {
        byte[] data = new byte[size];
        for (int i = 0; i < size; i++) data[i] = pattern;
        return data;
    }

    #endregion

    #region Tests for HashU64(uint)

    [Fact]
    public void HashU64_Uint_SameInput_SameHash()
    {
        uint input = 123456;
        uint hash1 = FastHash.HashU64(input).AsUInt32().GetElement(0);
        uint hash2 = FastHash.HashU64(input).AsUInt32().GetElement(0);
        Assert.Equal(hash1, hash2); // Deterministic output
    }

    [Fact]
    public void HashU64_Uint_DifferentInputs_DifferentHashes()
    {
        uint input1 = 123456;
        uint input2 = 654321;
        uint hash1 = FastHash.HashU64(input1).AsUInt32().GetElement(0);
        uint hash2 = FastHash.HashU64(input2).AsUInt32().GetElement(0);
        Assert.NotEqual(hash1, hash2); // Distinct hashes for different inputs
    }

    [Theory]
    [InlineData(uint.MinValue)]
    [InlineData(uint.MaxValue)]
    public void HashU64_Uint_EdgeCases(uint input)
    {
        uint hash = FastHash.HashU64(input).AsUInt32().GetElement(0);
        Assert.True(hash > 0); // Ensure hash is generated
    }

    #endregion

    #region Tests for HashU64(ulong)

    [Fact]
    public void HashU64_Ulong_SameInput_SameHash()
    {
        ulong input = 987654321;
        ulong hash1 = FastHash.HashU64(input).AsUInt64().GetElement(0);
        ulong hash2 = FastHash.HashU64(input).AsUInt64().GetElement(0);
        Assert.Equal(hash1, hash2); // Deterministic output
    }

    [Fact]
    public void HashU64_Ulong_DifferentInputs_DifferentHashes()
    {
        ulong input1 = 123456789;
        ulong input2 = 987654321;
        ulong hash1 = FastHash.HashU64(input1).AsUInt64().GetElement(0);
        ulong hash2 = FastHash.HashU64(input2).AsUInt64().GetElement(0);
        Assert.NotEqual(hash1, hash2); // Distinct hashes for different inputs
    }

    [Theory]
    [InlineData(ulong.MinValue)]
    [InlineData(ulong.MaxValue)]
    public void HashU64_Ulong_EdgeCases(ulong input)
    {
        ulong hash = FastHash.HashU64(input).AsUInt64().GetElement(0);
        Assert.True(hash > 0); // Ensure hash is generated
    }

    #endregion

    #region Tests for HashU64(ReadOnlySpan<byte>)

    [Fact]
    public void HashU64_ShortSpan_ProducesHash()
    {
        ReadOnlySpan<byte> input = stackalloc byte[] { 1, 2, 3 };
        ulong hash = FastHash.HashU64(input).AsUInt64().GetElement(0);
        Assert.True(hash > 0); // Valid hash for short input
    }

    [Fact]
    public void HashU64_LongSpan_ProducesHash()
    {
        byte[] input = GeneratePattern(1024); // 1024 bytes of repeating pattern
        ulong hash = FastHash.HashU64(input).AsUInt64().GetElement(0);
        Assert.True(hash > 0); // Valid hash for long input
    }


    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(31)]
    [InlineData(32)]
    public void HashU64_VariousLengths_ProducesHash(int length)
    {
        byte[] input = GeneratePattern(length);
        ulong hash = FastHash.HashU64(input).AsUInt64().GetElement(0);
        Assert.True(hash > 0); // Ensure hash for various lengths
    }

    #endregion

    #region Tests for HashU64Secure(ReadOnlySpan<byte>)

    [Fact]
    public void HashU64Secure_ShortInput_ProducesSecureHash()
    {
        ReadOnlySpan<byte> input = stackalloc byte[] { 42 };
        ulong hash = FastHash.HashU64Secure(input);
        Assert.True(hash > 0); // Ensure secure hash for short input
    }

    [Fact]
    public void HashU64Secure_LongInput_ProducesSecureHash()
    {
        byte[] input = GeneratePattern(2048); // Large input for secure hash
        ulong hash = FastHash.HashU64Secure(input);
        Assert.True(hash > 0); // Ensure secure hash for long input
    }

    #endregion

    #region Performance and Collision Tests

    [Fact]
    public void HashU64_CollisionTest_DifferentInputs_NoCollisions()
    {
        ulong[] inputs = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        HashSet<ulong> hashes = new();
        foreach (var input in inputs)
        {
            ulong hash = FastHash.HashU64(input).AsUInt64().GetElement(0);
            Assert.DoesNotContain(hash, hashes);
            hashes.Add(hash);
        }
    }

    [Fact]
    public void HashU64Secure_PerformanceTest()
    {
        byte[] input = GeneratePattern(1_000_000); // 1 MB of data
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        ulong hash = FastHash.HashU64Secure(input);
        stopwatch.Stop();
        Assert.True(stopwatch.ElapsedMilliseconds < 100); // Ensure performance within acceptable range
    }

    #endregion

    private const int LargeSize = 10_000_000; // 10 million characters

    [Fact]
    public void HashU64_Uint_ValidInput_ReturnsDeterministicHash()
    {
        // Arrange
        uint input = 123456;

        // Act
        uint hash1 = FastHash.HashU64(input).AsUInt32().GetElement(0);
        uint hash2 = FastHash.HashU64(input).AsUInt32().GetElement(0);

        // Assert
        Assert.Equal(hash1, hash2); // Ensure deterministic hashing
    }

    [Fact]
    public void HashU64_Ulong_ValidInput_ReturnsDeterministicHash()
    {
        // Arrange
        ulong input = 123456789;

        // Act
        ulong hash1 = FastHash.HashU64(input).AsUInt64().GetElement(0);
        ulong hash2 = FastHash.HashU64(input).AsUInt64().GetElement(0);

        // Assert
        Assert.Equal(hash1, hash2); // Ensure deterministic hashing
    }

    [Fact]
    public void HashU64_Uint_DifferentInputs_ProduceDifferentHashes()
    {
        // Arrange
        uint input1 = 123456;
        uint input2 = 654321;

        // Act
        uint hash1 = FastHash.HashU64(input1).AsUInt32().GetElement(0);
        uint hash2 = FastHash.HashU64(input2).AsUInt32().GetElement(0);

        // Assert
        Assert.NotEqual(hash1, hash2); // Ensure different inputs produce different hashes
    }

    [Fact]
    public void HashU64_Ulong_DifferentInputs_ProduceDifferentHashes()
    {
        // Arrange
        ulong input1 = 123456789;
        ulong input2 = 987654321;

        // Act
        ulong hash1 = FastHash.HashU64(input1).AsUInt64().GetElement(0);
        ulong hash2 = FastHash.HashU64(input2).AsUInt64().GetElement(0);

        // Assert
        Assert.NotEqual(hash1, hash2); // Ensure different inputs produce different hashes
    }

    [Fact]
    public void HashU64_Uint_EdgeCase_MaxValue()
    {
        // Arrange
        uint input = uint.MaxValue;

        // Act
        uint hash = FastHash.HashU64(input).AsUInt32().GetElement(0);

        // Assert
        Assert.True(hash > 0); // Ensure hash is generated
    }

    [Fact]
    public void HashU64_Ulong_EdgeCase_MaxValue()
    {
        // Arrange
        ulong input = ulong.MaxValue;

        // Act
        ulong hash = FastHash.HashU64(input).AsUInt64().GetElement(0);

        // Assert
        Assert.True(hash > 0); // Ensure hash is generated
    }

    [Fact]
    public void HashU64_ShortInput_ReturnsHash()
    {
        // Arrange
        ReadOnlySpan<byte> input = stackalloc byte[] { 1, 2, 3 };

        // Act
        ulong hash = FastHash.HashU64(input).AsUInt64().GetElement(0);

        // Assert
        Assert.True(hash > 0); // Ensure a hash is generated for short input
    }

    [Fact]
    public void HashU64_LongInput_ReturnsHash()
    {
        // Arrange
        byte[] input = new byte[128];
        for (int i = 0; i < input.Length; i++) input[i] = (byte)(i & 0xFF);

        // Act
        ulong hash = FastHash.HashU64(input).AsUInt64().GetElement(0);

        // Assert
        Assert.True(hash > 0); // Ensure a hash is generated for long input
    }

    [Fact]
    public void HashU64Secure_ShortInput_ProducesHash()
    {
        // Arrange
        ReadOnlySpan<byte> input = stackalloc byte[] { 42 };

        // Act
        ulong hash = FastHash.HashU64Secure(input);

        // Assert
        Assert.True(hash > 0); // Ensure a secure hash is generated for short input
    }

    [Fact]
    public void HashU64_LargeString_AllSameCharacters()
    {
        // Arrange
        string input = new string('A', LargeSize);
        ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes(input);

        // Act
        ulong hash = FastHash.HashU64(bytes).AsUInt64().GetElement(0);

        // Assert
        Assert.NotEqual(0UL, hash);
    }

    [Fact]
    public void HashU64_LargeString_RepeatingPattern()
    {
        // Arrange
        string pattern = "1234567890";
        int repeatCount = LargeSize / pattern.Length;
        string input = string.Concat(Enumerable.Repeat(pattern, repeatCount));
        ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes(input);

        // Act
        ulong hash = FastHash.HashU64(bytes).AsUInt64().GetElement(0);

        // Assert
        Assert.NotEqual(0UL, hash);
    }

    [Fact]
    public void HashU64_LargeString_RandomCharacters()
    {
        // Arrange
        string input = GenerateRandomString(LargeSize);
        ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes(input);

        // Act
        ulong hash = FastHash.HashU64(bytes).AsUInt64().GetElement(0); ;

        // Assert
        Assert.NotEqual(0UL, hash);
    }

    [Fact]
    public void HashU64Secure_LargeString_AllSameCharacters()
    {
        // Arrange
        string input = new string('Z', LargeSize);
        ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes(input);

        // Act
        ulong hash = FastHash.HashU64Secure(bytes);

        // Assert
        Assert.NotEqual(0UL, hash);
    }

    [Fact]
    public void HashU64Secure_LargeString_RepeatingPattern()
    {
        // Arrange
        string pattern = "abcdefghij";
        int repeatCount = LargeSize / pattern.Length;
        string input = string.Concat(Enumerable.Repeat(pattern, repeatCount));
        ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes(input);

        // Act
        ulong hash = FastHash.HashU64Secure(bytes);

        // Assert
        Assert.NotEqual(0UL, hash);
    }

    [Fact]
    public void HashU64Secure_LargeString_RandomCharacters()
    {
        // Arrange
        string input = GenerateRandomString(LargeSize);
        ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes(input);

        // Act
        ulong hash = FastHash.HashU64Secure(bytes);

        // Assert
        Assert.NotEqual(0UL, hash);
    }

    [Fact]
    public void HashU64_LargeString_DifferentStrings_GenerateDifferentHashes()
    {
        // Arrange
        string input1 = new string('X', LargeSize);
        string input2 = new string('Y', LargeSize);
        ReadOnlySpan<byte> bytes1 = Encoding.UTF8.GetBytes(input1);
        ReadOnlySpan<byte> bytes2 = Encoding.UTF8.GetBytes(input2);

        // Act
        ulong hash1 = FastHash.HashU64(bytes1).AsUInt64().GetElement(0); ;
        ulong hash2 = FastHash.HashU64(bytes2).AsUInt64().GetElement(0); ;

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashU64Secure_LargeString_DifferentStrings_GenerateDifferentHashes()
    {
        // Arrange
        string input1 = GenerateRandomString(LargeSize);
        string input2 = GenerateRandomString(LargeSize);
        ReadOnlySpan<byte> bytes1 = Encoding.UTF8.GetBytes(input1);
        ReadOnlySpan<byte> bytes2 = Encoding.UTF8.GetBytes(input2);

        // Act
        ulong hash1 = FastHash.HashU64Secure(bytes1);
        ulong hash2 = FastHash.HashU64Secure(bytes2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashU64_LargeString_SubstringHashesDiffer()
    {
        // Arrange
        string input = GenerateRandomString(LargeSize);
        ReadOnlySpan<byte> fullBytes = Encoding.UTF8.GetBytes(input);
        ReadOnlySpan<byte> substringBytes = Encoding.UTF8.GetBytes(input.Substring(0, LargeSize / 2));

        // Act
        ulong fullHash = FastHash.HashU64(fullBytes).AsUInt64().GetElement(0); ;
        ulong substringHash = FastHash.HashU64(substringBytes).AsUInt64().GetElement(0); ;

        // Assert
        Assert.NotEqual(fullHash, substringHash);
    }

    [Fact]
    public void HashU64Secure_LargeString_SubstringHashesDiffer()
    {
        // Arrange
        string input = new string('A', LargeSize);
        ReadOnlySpan<byte> fullBytes = Encoding.UTF8.GetBytes(input);
        ReadOnlySpan<byte> substringBytes = Encoding.UTF8.GetBytes(input.Substring(0, LargeSize / 2));

        // Act
        ulong fullHash = FastHash.HashU64Secure(fullBytes);
        ulong substringHash = FastHash.HashU64Secure(substringBytes);

        // Assert
        Assert.NotEqual(fullHash, substringHash);
    }

    private string GenerateRandomString(int length)
    {
        Random random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        char[] result = new char[length];

        for (int i = 0; i < length; i++)
        {
            result[i] = chars[random.Next(chars.Length)];
        }

        return new string(result);
    }
}