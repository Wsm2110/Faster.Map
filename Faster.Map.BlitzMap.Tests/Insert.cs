using Xunit;

namespace Faster.Map.BlitzMap.Tests
{
    public class InsertTests
    {
        [Fact]
        public void Insert_duplice_map_should_fail()
        {
            var map = new BlitzMap<uint, uint>(16, 0.9, null);

            map.Insert(1, 1000);

            var result = map.Insert(1, 1000);

            Assert.False(result);
        }

        [Fact]
        public void ConflictResolution_should()
        {
            // 5,21,37,53,69
            var map = new BlitzMap<uint, uint>(16, 0.9, null);
            map.Insert(5, 5 * 1000);
            map.Insert(21, 21 * 1000);
            map.Insert(37, 37 * 1000);
            map.Insert(53, 53 * 1000);
            map.Insert(69, 69 * 1000);

            Assert.True(map.Count == 5);
        }

        [Fact]
        public void RandomTest()
        {
            var length = 80000000;
            var map = new BlitzMap<long, long>(length, 0.80, null);
            var random = new Random(3);

            for (int i = 0; i < length; i++)
            {
                map.Insert(random.NextInt64(), i);
            }
            Assert.True(map.Count == length);
        }
    }
}
