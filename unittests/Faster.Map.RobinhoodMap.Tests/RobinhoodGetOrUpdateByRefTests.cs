using Faster.Map.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Map.RobinHoodMap.Tests
{
    public class RobinhoodGetOrUpdateByRefTests
    {
        [Fact]
        public void EmplaceOrUpdate_ReturnsCorrectValue_WhenKeyExists()
        {
            // Arrange
            var dictionary = new RobinhoodMap<uint, uint>(); // Initialize your dictionary
            var existingKey = 2u/* Create an existing key */;
            var existingValue = 4u /* Create an existing value corresponding to the existing key */;

            dictionary.Emplace(existingKey, existingValue);

            // Act
            ref var updatedValue = ref dictionary.GetOrUpdate(existingKey);

            // Assert         
            Assert.Equal(existingValue, updatedValue);
        }

        [Fact]
        public void EmplaceOrUpdate_ReturnsDefaultValue_WhenKeyDoesNotExist()
        {
            // Arrange
            var dictionary = new RobinhoodMap<uint, uint>(); // Initialize your dictionary

            uint nonExistingKey = 50030 /* Create a key that doesn't exist in the dictionary */;

            // Act
            ref var updatedValue = ref dictionary.GetOrUpdate(nonExistingKey);

            // Assert          
            Assert.Equal(0u, updatedValue);
        }

        [Fact]
        public void EmplaceOrUpdate_ByRefValue()
        {
            // Arrange
            var dictionary = new RobinhoodMap<uint, uint>(); // Initialize your dictionary
            var existingKey = 2u/* Create an existing key */;
            var existingValue = 4u /* Create an existing value corresponding to the existing key */;

            dictionary.Emplace(existingKey, existingValue);

            // Act
            ref var updatedValue = ref dictionary.GetOrUpdate(existingKey);

            ++updatedValue;

            var result = dictionary.Get(existingKey, out var retrievedResult);
            // Assert
            Assert.True(result);
            Assert.Equal(5u, retrievedResult);
        }

        [Fact]
        public void EmplaceOrUpdate_ByRefValue_Using_string_values()
        {
            // Arrange
            var dictionary = new RobinhoodMap<uint, string>(); // Initialize your dictionary

            var existingKey = 2u/* Create an existing key */;
            string existingValue = null /* Create an existing value corresponding to the existing key */;

            dictionary.Emplace(existingKey, existingValue);

            // Act
            ref var updatedValue = ref dictionary.GetOrUpdate(existingKey);

            updatedValue += "test";

            var result = dictionary.Get(existingKey, out var retrievedResult);
            // Assert
            Assert.True(result);
            Assert.Equal("test", retrievedResult);
        }

        [Fact]
        public void GetOrUpdate_ByRefValue_Using_string_values()
        {
            // Arrange
            var dictionary = new RobinhoodMap<uint, string>(); // Initialize your dictionary

            var existingKey = 2u/* Create an existing key */;

            // Act
            ref var updatedValue = ref dictionary.GetOrUpdate(existingKey);

            updatedValue += "test";

            var result = dictionary.Get(existingKey, out var retrievedResult);
            // Assert
            Assert.True(result);
            Assert.Equal("test", retrievedResult);
        }
    }
}
