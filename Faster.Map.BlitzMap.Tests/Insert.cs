using System.Numerics;
using Xunit;

namespace Faster.Map.BlitzMap.Tests;

public class InsertTests
{
    [Fact]
    public void Assert_HashTotheSameBucket_ShouldResolveAccordingly()
    {
        var map = new BlitzMap<uint, uint>(16, 0.8);
        map.Insert(0xABCD0005, 0);
        map.Insert(0xABCD0015, 0);
        map.Insert(0xABCD0025, 0);
        map.Insert(0xABCD0035, 0);

        Assert.True(map.Count == 4);
    }


    [Fact]
    public void Assert_HashToBucketWHileHomeBucketIsTaken_ShouldKickoutBucket()
    {
        var map = new BlitzMap<uint, uint>(16, 0.8);
        map.Insert(0xABCD0005, 0);
        map.Insert(0xABCD0015, 0);
        map.Insert(0xABCD0025, 0);
        map.Insert(0xABCD0035, 0);

        Assert.True(map.Count == 4);

        map.Insert(0xABCD0000, 0);
    }

    [Fact]
    public void Insert_Should_Add_New_Entry()
    {
        // Arrange
        var map = new BlitzMap<uint, uint>(16, 0.8);

        // Act
        bool inserted = map.Insert(0xABCD0005, 100);

        // Assert
        Assert.True(inserted);
        Assert.Equal(1, map.Count);
    }

    [Fact]
    public void Insert_Should_Reject_Duplicate_Key()
    {
        // Arrange
        var map = new BlitzMap<uint, uint>(16, 0.8);
        map.Insert(0xABCD0005, 100);

        // Act
        bool insertedAgain = map.Insert(0xABCD0005, 200);

        // Assert
        Assert.False(insertedAgain); // Should return false because key exists
        Assert.Equal(1, map.Count); // Count should remain unchanged
    }

    [Fact]
    public void Insert_Should_Handle_Collisions_Properly()
    {
        // Arrange
        var map = new BlitzMap<uint, uint>(16, 0.8);

        // Insert 5 values that hash to the same bucket (index = 5)
        map.Insert(0xABCD0005, 100);
        map.Insert(0xABCD0015, 200);
        map.Insert(0xABCD0025, 300);
        map.Insert(0xABCD0035, 400);
        map.Insert(0xABCD0045, 500);

        // Assert
        Assert.Equal(5, map.Count);
    }

    [Theory]
    [InlineData(90_000_000)]

    public void Insert_Entries_And_Retrieve_Same_Buckets(uint length)
    {
        // Arrange
        var map = new BlitzMap<uint, uint>((int)BitOperations.RoundUpToPowerOf2(length), 0.8);
        uint[] keys = new uint[length];
        uint[] values = new uint[length];

        // Generate 100 unique keys
        for (int i = 0; i < length; i++)
        {
            keys[i] = (uint)(i); // Ensures they hash into different buckets
            values[i] = (uint)(i * 10); // Arbitrary values
        }

        // Act - Insert all keys
        for (int i = 0; i < length; i++)
        {
            bool inserted = map.Insert(keys[i], values[i]);
            Assert.True(inserted, $"Insert failed for key {keys[i]:X}");
        }

        // Assert - Ensure all keys are retrievable with correct values
        for (int i = 0; i < length; i++)
        {
            bool found = map.Get(keys[i], out var retrievedValue);

            Assert.True(found, $"Key {keys[i]:X} was not found.");
            Assert.Equal(values[i], retrievedValue);
        }

        // Verify total count
        Assert.Equal(length, (uint)map.Count);
    }
}