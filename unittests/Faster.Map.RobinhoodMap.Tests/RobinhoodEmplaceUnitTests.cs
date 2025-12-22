using Faster.Map.Core;
using Newtonsoft.Json.Linq;

namespace Faster.Map.RobinHoodMap.Tests
{
    public class RobinhoodAddTests
    {

        [Fact]
        public void Emplace_AddsEntryWhenSpotIsEmpty()
        {
            // Arrange
            var dictionary = new RobinhoodMap<int, int>();

            // Act
            bool result = dictionary.Emplace(1, 1);

            // Assert
            Assert.True(result);
            Assert.Equal(1, dictionary.Count);
            Assert.Contains(1, dictionary.Keys); // Assuming you have a way to access keys
            Assert.Contains(1, dictionary.Values); // Assuming you have a way to access values
        }

        [Fact]
        public void Emplace_ResizesWhenLoadFactorIsReached()
        {
            // Arrange
            var dictionary = new RobinhoodMap<int, int>();
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
            var dictionary = new RobinhoodMap<int, int>();
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

        [Fact]
        public void Emplace_AddKeysFromKeysEnumeratorToAnotherHashmap()
        {
            var robinhoodMap = new RobinhoodMap<uint, uint>(12_000_000);
            var rnd0 = new Random(3);

            for (int j = 0; j < 50_000_000; j++)
            {
                robinhoodMap.Emplace((uint)rnd0.Next(), 0);
                if (robinhoodMap.Count == 100000) break;
            }

            var rndInts = robinhoodMap.Keys.ToArray();

            RobinhoodMap<uint, uint> map = new(64);

            for (uint i = 0; i < rndInts.Length; i++)
            {
                var v = rndInts[i];

                map.Emplace(v, v);
            }
        }
    }
}