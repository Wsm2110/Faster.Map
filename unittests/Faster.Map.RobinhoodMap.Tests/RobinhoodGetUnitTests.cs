using Faster.Map;
using Faster.Map.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Map.RobinHoodMap.Tests
{
    public class RobinhoodGetUnitTests(RobinhoodFixture fixture) : IClassFixture<RobinhoodFixture>
    {

        [Fact]
        public void Get_ReturnsCorrectValueForKey()
        {
            // Arrange
            var dictionary = new RobinhoodMap<int, int>();
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
            var dictionary = new RobinhoodMap<int, int>();

            // Act
            var result = dictionary.Get(5, out var retrievedValue);

            // Assert
            Assert.Equal(default, retrievedValue);
            Assert.False(result);
        }

        [Fact]
        public void Get_MillionRandomEntries()
        {
            // Arrange
            var keys = fixture.GenerateUniqueKeys(1000000).ToList();
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
            var map = new RobinhoodMap<uint, uint>();
            map.Emplace(0, 1);

            // Act
            var result = map.Get(0, out var retrievedValue);

            // Assert
            Assert.Equal(1u, retrievedValue);
            Assert.True(result);
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
                Assert.Equal(500000, map.Count);
            }
        }

        [Fact]
        public void Assert_Keys_From_File_Is_OneMillion()
        {
            // Arrange
            var keys = fixture.LoadKeysFromFile().ToList();
            var map = fixture.CreateMap(keys);

            Assert.Equal(1000000, map.Count);

        }

        [Fact]
        public void Assert_Get_Keys_From_File()
        {
            // Arrange
            var keys = fixture.LoadKeysFromFile().ToList();
            var map = fixture.CreateMap(keys);

            for (var i = 0; i < keys.Count; i++)
            {
                Assert.True(map.Get(keys[i], out var retrievedValue));
            }
        }

        [Fact]
        public void Assert_Get_1000_Keys_From_File()
        {
            // Arrange
            var keys = fixture.LoadKeysFromFile().Take(1000).ToList();
            var map = fixture.CreateMap(keys);

            for (var i = 0; i < 1000; i++)
            {
                Assert.True(map.Get(keys[i], out var retrievedValue));
            }
        }

    }
}