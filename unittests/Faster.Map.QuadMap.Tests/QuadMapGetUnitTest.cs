using Faster.Map.QuadMap;

namespace Faster.Map.QuadMap.Tests
{
    public class QuadMapGetUnitTest(QuadMapFixture fixture) : IClassFixture<QuadMapFixture>
    {
        [Fact]
        public void Get_ReturnsCorrectValueForKey()
        {
            // Arrange
            var dictionary = new QuadMap<int, int>();
            var key = 500;
            var value = 7;

            dictionary.Emplace(key, value);

            // Act
            var retrievedValue = dictionary.Get(key, out var result);

            // Assert
            Assert.Equal(value, result);
            Assert.True(retrievedValue);
        }

        [Fact]
        public void Get_ReturnsDefaultWhenKeyNotFound()
        {
            // Arrange
            var dictionary = new QuadMap<int, int>();

            // Act
            var result = dictionary.Get(5, out var retrievedValue);

            // Assert
            Assert.Equal(default(int), retrievedValue);
            Assert.False(result);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void Get_RandomEntries(int count)
        {
            // Arrange
            var keys = fixture.GenerateUniqueKeys(count).ToList();
            var map = fixture.CreateMap(keys);

            //Act
            foreach (var key in keys)
            {
                if (!map.Get(key, out var _))
                {
                    //Assert
                    Assert.Fail();
                }
            }
        }

        [Fact]
        public void Get_Unkown_Key_Should_Return_False()
        {
            // Arrange
            var map = fixture.CreateMap(100000);
            // Act
            var result = map.Get(20000010, out var retrievedValue);
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Get_Default_Key_Should_Return_True()
        {
            // Arrange
            var map = new QuadMap<uint, uint>();
            map.Emplace(0, 1);

            // Act
            var result = map.Get(0, out var retrievedValue);

            // Assert
            Assert.Equal(1u, retrievedValue);
            Assert.True(result);
        }

        [Fact]
        public void Entries_ReturnsAllEntries()
        {
            // Arrange
            var map = new QuadMap<int, string>();
            map.Emplace(1, "One");
            map.Emplace(2, "Two");
            map.Emplace(3, "Three");

            // Act
            var entries = map.Entries;

            // Assert
            Assert.Equal(3, entries.Count());
            Assert.Contains(new KeyValuePair<int, string>(1, "One"), entries);
            Assert.Contains(new KeyValuePair<int, string>(2, "Two"), entries);
            Assert.Contains(new KeyValuePair<int, string>(3, "Three"), entries);
        }

        [Fact]
        public void Add_OneMillion_Remove500k_Get500k()
        {
            // Arrange
            var keys = fixture.GenerateUniqueKeys(1000000).ToList();
            var map = fixture.CreateMap(keys);

            // Act
            foreach (var key in keys.Take(500000))
            {
                map.Remove(key);
            }

            // Assert
            foreach (var key in keys.Skip(500000).Take(500000))
            {
                var result = map.Get(key, out var retrievedValue);

                Assert.True(result);
                Assert.Equal(key, retrievedValue);
                Assert.Equal(500000u, map.Count);
            }
        }
    }
}