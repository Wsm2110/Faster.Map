using Faster.Map.Concurrent;

namespace Faster.Map.CMap.Tests;

public class EntryTests
{
    [Fact]
    public void Metadata_SetAndGet_CorrectlyMasksBits()
    {
        // Arrange
        var entry = new CMap<int, string>.Entry();
        entry.Meta = 0b01111111; // Set all bits, including the lock bit (7th bit)

        // Act
        sbyte metadata = entry.Metadata;

        // Assert
        Assert.Equal(0b00111111, metadata); // Only the lower 6 bits should be considered
    }

    [Fact]
    public void Enter_LocksEntry_Sets7thBit()
    {
        // Arrange
        var entry = new CMap<int, string>.Entry();
        entry.Meta = 0b00000000; // All bits initially 0

        // Act
        entry.Enter();

        // Assert
        Assert.Equal(0b01000000, entry.Meta & 0b01000000); // The 7th bit should be set
    }

    [Fact]
    public void Exit_UnlocksEntry_Clears7thBit()
    {
        // Arrange
        var entry = new CMap<int, string>.Entry();
        entry.Meta = 0b01111111; // 7th bit initially set

        // Act
        entry.Exit();

        // Assert
        Assert.Equal(0b00111111, entry.Meta & 0b01111111); // The 7th bit should be cleared
    }

    [Fact]
    public void Enter_DetectsResizedOrTombstone_ReturnsImmediately()
    {
        // Arrange
        var entry = new CMap<int, string>.Entry();
        entry.Meta = -125; // Simulate the entry being resized

        // Act
        entry.Enter();

        // Assert
        Assert.Equal(-125, entry.Meta); // Meta should remain as _resized
    }
}