using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Map.QuadMap.Tests
{
    public class QuadMapResizeTests
    {

        [Fact]
        public void Resize_EnsuresCorrectSize()
        {
            // Arrange
            var map = new QuadMap<int, string>(4, 0.5);

            // Act
            map.Emplace(1, "One");
            map.Emplace(2, "Two");
            map.Emplace(3, "Three");
            map.Emplace(4, "Four");

            // Assert
            Assert.Equal(8u, map.Size);
            Assert.Equal(4u, map.Count);
        }
    }
}
