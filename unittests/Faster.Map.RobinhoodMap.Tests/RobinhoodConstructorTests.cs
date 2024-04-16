using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Map.RobinHoodMap.Tests
{
    public class RobinhoodConstructorTests
    {

        [Fact]
        public void AssertRoundToPowerOfTwo()
        {
            //act, assign
            var map = new RobinhoodMap<uint, uint>(10);

            //next power of 10 is 16 + log2(16) = 20
            Assert.Equal(20u, map.Size);
        }
    }
}
