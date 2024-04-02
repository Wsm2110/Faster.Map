using Faster.Map.QuadMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Map.RobinhoodMap.Tests
{
    public class QuadMapStringTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void Get_Strings_Return_Succesful(uint amount)
        {
            // Arrange
            var map = new QuadMap<string, string>();

            for (uint i = 0; i < amount; i++)
            {
                map.Emplace(i.ToString(), Guid.NewGuid().ToString());
            }

            for (uint i = 0; i < amount; i++)
            {
                // Act
                var result = map.Get(i.ToString(), out var _);
                if (!result)
                {
                    // Assert
                    Assert.Fail();
                }
            }
        }

    }
}
