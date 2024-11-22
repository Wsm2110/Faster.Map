using System;
using System.Linq;
using System.Text;
using Xunit;

namespace Faster.Map.Hash;
public class FastHashLargeStringTests
{
    private const int LargeSize = 10_000_000; // 10 million characters


    [Fact]
    public void HashU64_LargeString_AllSameCharacters()
    {
        // Arrange
        string input = new string('A', LargeSize);
        ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes(input);

        // Act
        ulong hash = FastHash.HashU64(bytes);

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
        ulong hash = FastHash.HashU64(bytes);

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
        ulong hash = FastHash.HashU64(bytes);

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
        ulong hash1 = FastHash.HashU64(bytes1);
        ulong hash2 = FastHash.HashU64(bytes2);

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
        ulong fullHash = FastHash.HashU64(fullBytes);
        ulong substringHash = FastHash.HashU64(substringBytes);

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