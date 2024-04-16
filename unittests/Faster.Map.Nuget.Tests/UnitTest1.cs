using Faster.Map.DenseMap;
using Faster.Map.QuadMap;
using Faster.Map.RobinHoodMap;

namespace Faster.Nuget.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Assert_Ctor_DenseMap()
        {
            var map = new DenseMap<int, int>();
            Assert.NotNull(map);
        }

        [Fact]
        public void Assert_Ctor_QuadMap()
        {
            var map = new QuadMap<int, int>();
            Assert.NotNull(map);
        }

        [Fact]
        public void Assert_Ctor_RobinhoodMap()
        {
            var map = new RobinhoodMap<int, int>();
            Assert.NotNull(map);
        }
    }
}