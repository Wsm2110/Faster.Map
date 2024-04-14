using Faster.Map.QuadMap;

namespace Faster.Map.QuadMap.Tests
{
    public class RobinhoodContainsTests
    {

        [Fact]
        public void Contains_ExistingKey_ReturnsTrue()
        {
            // Arrange
            var map = new QuadMap<int, string>();
            map.Emplace(1, "One");
            map.Emplace(2, "Two");
            map.Emplace(3, "Three");

            // Act
            bool result = map.Contains(2);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Contains_NonExistingKey_ReturnsFalse()
        {
            // Arrange
            var map = new QuadMap<int, string>();
            map.Emplace(1, "One");
            map.Emplace(2, "Two");
            map.Emplace(3, "Three");

            // Act
            bool result = map.Contains(4);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Contains_EmptyMap_ReturnsFalse()
        {
            // Arrange
            var map = new QuadMap<int, string>();
            // Act
            bool result = map.Contains(1);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Contains_NullKey_ThrowsArgumentNullException()
        {
            // Arrange
            var map = new QuadMap<string, string>();

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => map.Contains(null));
        }

    }
}
