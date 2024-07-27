using Faster.Map.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Map.CMap.Tests
{
    public class LargeStructTests
    {
        [Fact]
        public void EmplaceAndGet_LargeStructKey_ShouldWorkCorrectly()
        {
            // Arrange
            var map = new CMap<LargeStruct, string>();
            var key = new LargeStruct { Part1 = 1, Part2 = 2, Part3 = 3, Part4 = 4 };
            var value = "test value";

            // Act
            var emplaceResult = map.Emplace(key, value);
            var getResult = map.Get(key, out var retrievedValue);

            // Assert
            Assert.True(emplaceResult);
            Assert.True(getResult);
            Assert.Equal(value, retrievedValue);
        }

        [Fact]
        public void Update_LargeStructKey_ShouldWorkCorrectly()
        {
            // Arrange
            var map = new CMap<LargeStruct, string>();
            var key = new LargeStruct { Part1 = 1, Part2 = 2, Part3 = 3, Part4 = 4 };
            var initialValue = "initial value";
            var updatedValue = "updated value";

            map.Emplace(key, initialValue);

            // Act
            var updateResult = map.Update(key, updatedValue);
            var getResult = map.Get(key, out var retrievedValue);

            // Assert
            Assert.True(updateResult);
            Assert.True(getResult);
            Assert.Equal(updatedValue, retrievedValue);
        }

        [Fact]
        public void Remove_LargeStructKey_ShouldWorkCorrectly()
        {
            // Arrange
            var map = new CMap<LargeStruct, string>();
            var key = new LargeStruct { Part1 = 1, Part2 = 2, Part3 = 3, Part4 = 4 };
            var value = "test value";

            map.Emplace(key, value);

            // Act
            var removeResult = map.Remove(key);
            var getResult = map.Get(key, out var retrievedValue);

            // Assert
            Assert.True(removeResult);
            Assert.False(getResult);
            Assert.Null(retrievedValue);
        }

        [Fact]
        public void ConcurrentOperations_LargeStructKey_ShouldBeThreadSafe()
        {
            // Arrange
            var map = new CMap<LargeStruct, string>();
       
            var value = "test value";

            // Act
            var tasks = new List<Task>();
            for (int i = 0; i < 1000; i++)
            {
                var key = new LargeStruct { Part1 = i, Part2 = i + 1, Part3 = 3, Part4 = 4 };
                tasks.Add(Task.Run(() => map.Emplace(key, value)));
            }

            Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(10));


            // Assert
            // Verify that the map is in a consistent state
            Assert.True(map.Count == 1000);
        }

        [Fact]
        public void ConcurrentOperations_RemovingLargeStructKey_ShouldBeThreadSafe()
        {
            // Arrange
            var map = new CMap<LargeStruct, string>();

            var value = "test value";

            // Act
            var tasks = new List<Task>();
            for (int i = 0; i < 1000000; i++)
            {
                var key = new LargeStruct { Part1 = i, Part2 = i + 1, Part3 = 3, Part4 = 4 };
                tasks.Add(Task.Run(() => map.Emplace(key, value)));
            }

            Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(10));


            // Assert
            // Verify that the map is in a consistent state
            Assert.True(map.Count == 1000000);
        }

    }
}
