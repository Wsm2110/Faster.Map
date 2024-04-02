﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Map.QuadMap.Tests
{
    public class QuadMapUpdateTests
    {
        [Fact]
        public void Update_ExistingKey_ReturnsTrue()
        {
            // Arrange
            var map = new QuadMap<int, string>();
            map.Emplace(1, "Value");

            // Act
            var result = map.Update(1, "UpdatedValue");

            // Assert
            Assert.True(result);
            Assert.Equal("UpdatedValue", map[1]);
        }

        [Fact]
        public void Update_NonExistingKey_ReturnsFalse()
        {
            // Arrange
            var map = new QuadMap<int, string>();

            // Act
            var result = map.Update(1, "Value");

            // Assert
            Assert.False(result);
            Assert.Throws<KeyNotFoundException>(() => map[1]);
        }
    }
}
