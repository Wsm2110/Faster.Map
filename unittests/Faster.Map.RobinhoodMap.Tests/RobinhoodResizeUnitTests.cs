using Faster.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Map.RobinHoodMap.Tests
{
    public class RobinhoodResizeUnitTests
    {

        [Fact]
        public void Resize_EnsuresCorrectSize()
        {
            // Arrange
            var map = new RobinhoodMap<int, string>(4, 0.5);

            // Act
            map.Emplace(1, "One");
            map.Emplace(2, "Two");
            map.Emplace(3, "Three");
            map.Emplace(4, "Four");

            // Assert
            Assert.Equal(4u + BitOperations.Log2(4), map.Size);
            Assert.Equal(4, map.Count);
        }
    }
}
