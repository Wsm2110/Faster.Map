using System;
using System.Linq;
using Xunit;
using Faster.Map.Core;

namespace Faster.Map.RobinhoodMap.Tests
{
    public class RobinhoodMapGetTests
    {
        [Fact]
        public void Get_Finds_All_Inserted()
        {
            var map = new RobinhoodMap<int, int>(16);

            for (int i = 0; i < 10_000; i++)
                map.Emplace(i, i);

            for (int i = 0; i < 10_000; i++)
            {
                Assert.True(map.Get(i, out var v));
                Assert.Equal(i, v);
            }
        }

        [Fact]
        public void Get_LongProbeChains()
        {
            var map = new RobinhoodMap<int, int>(8);

            for (int i = 0; i < 5_000; i++)
                map.Emplace(i * 8, i);

            for (int i = 0; i < 5_000; i++)
            {
                Assert.True(map.Get(i * 8, out var v));
                Assert.Equal(i, v);
            }
        }

        [Fact]
        public void Get_WrapAround()
        {
            var map = new RobinhoodMap<int, int>(16);

            for (int i = 0; i < 100; i++)
                map.Emplace(i * 16 + 7, i);

            for (int i = 0; i < 100; i++)
                Assert.True(map.Get(i * 16 + 7, out _));
        }

        [Fact]
        public void Get_StopsWhenDistanceBreaks()
        {
            var map = new RobinhoodMap<int, int>(32);

            for (int i = 0; i < 200; i++)
                map.Emplace(i, i);

            Assert.False(map.Get(999_999, out _));
        }

        [Fact]
        public void Get_WorksAfterDeleteShift()
        {
            var map = new RobinhoodMap<int, int>(64);

            for (int i = 0; i < 50_000; i++)
                map.Emplace(i, i);

            for (int i = 0; i < 25_000; i++)
                map.Remove(i);

            for (int i = 25_000; i < 50_000; i++)
            {
                Assert.True(map.Get(i, out var v));
                Assert.Equal(i, v);
            }
        }

        [Fact]
        public void Get_MillionRandom()
        {
            var rnd = new Random(123);
            var keys = Enumerable.Range(0, 1_000_000)
                                 .OrderBy(_ => rnd.Next())
                                 .ToArray();

            var map = new RobinhoodMap<int, int>(1_048_576);

            foreach (var k in keys)
                map.Emplace(k, k);

            foreach (var k in keys)
            {
                Assert.True(map.Get(k, out var v));
                Assert.Equal(k, v);
            }
        }

        [Fact]
        public void EmptyMap_GetReturnsFalse()
        {
            var map = new RobinhoodMap<int, int>(8);
            Assert.False(map.Get(1, out _));
        }

        [Fact]
        public void InsertSingle_GetReturnsValue()
        {
            var map = new RobinhoodMap<int, int>(8);
            map.Emplace(10, 20);

            Assert.True(map.Get(10, out var v));
            Assert.Equal(20, v);
        }

        [Fact]
        public void InsertTwoDifferentKeys()
        {
            var map = new RobinhoodMap<int, int>(8);
            map.Emplace(1, 11);
            map.Emplace(2, 22);

            Assert.True(map.Get(1, out var v1));
            Assert.Equal(11, v1);
            Assert.True(map.Get(2, out var v2));
            Assert.Equal(22, v2);
        }

        [Fact]
        public void InsertSameKeyTwice_ReturnsFalse()
        {
            var map = new RobinhoodMap<int, int>(8);
            Assert.True(map.Emplace(5, 50));
            Assert.False(map.Emplace(5, 60));
        }

        [Fact]
        public void RemoveExistingKey()
        {
            var map = new RobinhoodMap<int, int>(8);
            map.Emplace(1, 100);
            Assert.True(map.Remove(1));
            Assert.False(map.Get(1, out _));
        }

        [Fact]
        public void RemoveNonExistingKey_ReturnsFalse()
        {
            var map = new RobinhoodMap<int, int>(8);
            Assert.False(map.Remove(123));
        }

        [Fact]
        public void RemoveOne_DoesNotAffectOthers()
        {
            var map = new RobinhoodMap<int, int>(8);
            map.Emplace(1, 10);
            map.Emplace(2, 20);
            map.Emplace(3, 30);

            map.Remove(2);

            Assert.True(map.Get(1, out var v1));
            Assert.Equal(10, v1);
            Assert.False(map.Get(2, out _));
            Assert.True(map.Get(3, out var v3));
            Assert.Equal(30, v3);
        }

        [Fact]
        public void CountTracksInsertAndRemove()
        {
            var map = new RobinhoodMap<int, int>(8);

            map.Emplace(1, 1);
            map.Emplace(2, 2);
            Assert.Equal(2, map.Count);

            map.Remove(1);
            Assert.Equal(1, map.Count);
        }

    }
}
