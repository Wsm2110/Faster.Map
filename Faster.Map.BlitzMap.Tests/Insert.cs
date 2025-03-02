using System.Numerics;
using System.Runtime.CompilerServices;
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

    private static ulong g_lehmer64_state = 3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Lehmer64()
    {
        g_lehmer64_state *= 0xda942042e4dd58b5UL;
        return g_lehmer64_state >> 64;
    }

    [Theory]
    [InlineData(134217728)]

    public void Insert_Entries_And_Retrieve_Same_Buckets(uint length)
    {
        var rnd = new Random(3);

        var uni = new HashSet<uint>((int)length * 2);

        while (uni.Count < (uint)(length * 0.5))
        {
            uni.Add((uint)Lehmer64());
        }

       var keys = uni.ToArray();


        // Arrange
        var map = new BlitzMap<uint, uint>((int)BitOperations.RoundUpToPowerOf2(length), 0.5);
           
        // Act - Insert all keys
        for (int i = 0; i < keys.Length; i++)
        {
            bool inserted = map.Insert(keys[i], 1);
            Assert.True(inserted, $"Insert failed for key {keys[i]}");
        }

        // Assert - Ensure all keys are retrievable with correct values
        for (int i = 0; i < keys.Length; i++)
        {
            bool found = map.Get(keys[i], out var retrievedValue);
            if (found == false) 
            {
            
            }

            Assert.True(found, $"Key {keys[i]} was not found.");
            Assert.Equal(1u, retrievedValue);
        }

        // Verify total count
        Assert.Equal(keys.Length, map.Count);
    }
}