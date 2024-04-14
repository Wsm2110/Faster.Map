using Faster.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Map.RobinhoodMap.Tests
{
    public class RobinhoodRemoveUnitTests(RobinhoodFixture fixture) : IClassFixture<RobinhoodFixture>
    {
        [Fact]
        public void Clear_RemovesAllElements()
        {
            // Arrange
            var map = new RobinhoodMap<int, string>();
            map.Emplace(1, "One");
            map.Emplace(2, "Two");
            map.Emplace(3, "Three");

            // Act
            map.Clear();

            // Assert
            Assert.Equal(0, map.Count);
            Assert.Empty(map.Entries);
        }

        [Fact]
        public void Remove_RemovesExistingKey_ReturnsTrue()
        {
            // Arrange
            var dictionary = new RobinhoodMap<int, string>();
            dictionary.Emplace(1, "One");

            // Act
            var result = dictionary.Remove(1);

            // Assert
            Assert.True(result);
            Assert.Empty(dictionary.Entries); // Ensure the dictionary is empty after removal
        }

        [Fact]
        public void Remove_AttemptToRemoveNonExistingKey_ReturnsFalse()
        {
            // Arrange
            var dictionary = new RobinhoodMap<int, string>();
            dictionary.Emplace(1, "One");

            // Act
            var result = dictionary.Remove(2); // 2 does not exist in the dictionary

            // Assert
            Assert.False(result);
            Assert.Single(dictionary.Entries); // Ensure the dictionary is unchanged
        }

        [Fact]
        public void ShiftRemove_RemovesEntryAtGivenIndex_ShiftsEntriesDown()
        {
            // Arrange
            var dictionary = new RobinhoodMap<int, string>();
            dictionary.Emplace(1, "One");
            dictionary.Emplace(2, "Two");
            dictionary.Emplace(3, "Three");

            // Act
            dictionary.Remove(2); // Removing entry with key 2

            // Assert
            Assert.Equal(2, dictionary.Count);
            Assert.True(dictionary.Contains(1));
            Assert.True(dictionary.Contains(3));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        [InlineData(1000000)]
        [InlineData(10000000)]
        [InlineData(100000000)]
        public void ShiftRemove_ShiftsOneMillionEntriesDown(int amount)
        {
            // Arrange
            var keys = fixture.GenerateUniqueKeys(amount).ToList();
            var map = fixture.CreateMap(keys);

            // Act
            foreach (var key in keys)
            {
                map.Remove(key);
            }

            // Assert
            Assert.Equal(0, map.Count);           
        }

        [Fact]
        public void ShiftRemove_RemoveOnlyOne()
        {
            // Arrange
            var dictionary = new RobinhoodMap<int, string>();
            dictionary.Emplace(1, "One");

            // Act
            dictionary.Remove(1); // Removing entry with key 2

            // Assert
            Assert.Equal(0, dictionary.Count);
        }
    }
}
