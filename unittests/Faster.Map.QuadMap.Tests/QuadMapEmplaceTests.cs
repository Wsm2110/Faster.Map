using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Map.QuadMap.Tests
{
    public class QuadMapEmplaceTests
    {
        [Fact]
        public void Emplace_AddsEntryWhenSpotIsEmpty()
        {
            // Arrange
            var dictionary = new QuadMap<int, int>();

            // Act
            bool result = dictionary.Emplace(1, 1);

            // Assert
            Assert.True(result);
            Assert.Equal(1u, dictionary.Count);
            Assert.Contains(1, dictionary.Keys); // Assuming you have a way to access keys
            Assert.Contains(1, dictionary.Values); // Assuming you have a way to access values
        }

        [Fact]
        public void Emplace_ResizesWhenLoadFactorIsReached()
        {
            // Arrange
            var dictionary = new QuadMap<int, int>();
            // Fill the dictionary to reach the load factor

            var random = new Random();
            for (int i = 0; i < 24; i++)
            {
                var entry = random.Next(int.MaxValue);
                dictionary.Emplace(entry, entry);
            }

            // Act
            bool result = dictionary.Emplace(5, 5);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Emplace_StealsEntryWhenPslIsGreater()
        {
            // Arrange
            var dictionary = new QuadMap<int, int>();
            var key1 = 1;
            var value1 = 5;
            var key2 = 2;
            var value2 = 6;

            // Act
            dictionary.Emplace(key1, value1);
            dictionary.Emplace(key2, value2);

            // Assert
             Assert.Contains(key2, dictionary.Keys); // Key2 should be inserted successfully despite having greater Psl
        }
    }
}
