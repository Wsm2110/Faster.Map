using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Faster.Map.Core.Tests
{
    [TestClass]
    public class FastMapTests
    {
        // FastMap<uint, uint> //_fastMap = new FastMap<uint, uint>(16, 0.5);
        private uint[] keys;

        [TestInitialize]
        public void Setup()
        {
            var output = File.ReadAllText("Numbers.txt");
            var splittedOutput = output.Split(',');

            keys = new uint[splittedOutput.Length];

            for (var index = 0; index < splittedOutput.Length; index++)
            {
                keys[index] = uint.Parse(splittedOutput[index]);
            }

            foreach (var key in keys)
            {
                //  _fastMap.Emplace(key, key);
            }

            // Shuffle(new Random(), keys);
        }

        private static void Shuffle<T>(Random rng, T[] a)
        {
            int n = a.Length;
            while (n > 1)
            {
                int k = rng.Next(--n);
                T temp = a[n];
                a[n] = a[k];
                a[k] = temp;
            }

        }

        [TestMethod]
        public void AssertUpdate()
        {
            //Assign
            var faster = new FastMap<ulong, ulong>(16, 0.88);
            faster.Emplace(454, 454);
            faster.Emplace(438, 438);
            faster.Emplace(422, 422);
            faster.Emplace(406, 406);
            faster.Emplace(390, 390);
            faster.Emplace(3912340, 390);

            //Act
            faster.Update(390, 2345);

            //Assert
            faster.Get(390, out var result);
            Assert.IsTrue(result == 2345);
        }

        [TestMethod]
        public void AssertContainsKey()
        {
            //Assign
            var faster = new FastMap<ulong, ulong>(16, 0.88);
            faster.Emplace(454, 454);
            faster.Emplace(438, 438);
            faster.Emplace(422, 422);
            faster.Emplace(406, 406);
            faster.Emplace(390, 390);
            faster.Emplace(3912340, 390);

            //Act
            faster.Update(390, 2345);

            //Assert
            Assert.IsTrue(faster.Contains(390));
        }

        [TestMethod]
        public void Get()
        {
            FastMap<uint, uint> map = new FastMap<uint, uint>();
            map.Emplace(42, 42);
            map.Emplace(144, 144);
            map.Emplace(160, 160);
            map.Emplace(175, 175);
            map.Emplace(192, 192);
            map.Emplace(194, 194);
            map.Emplace(199, 199);
            map.Emplace(207, 207);
            map.Emplace(218, 218);
            map.Emplace(220, 220);
            map.Emplace(231, 231);
            map.Emplace(2, 2);

            map.Get(220, out var result);

            Assert.IsTrue(result == 220);
        }


        [TestMethod]
        public void AssertCustomEnumerators()
        {
            var map = new FastMap<uint, uint>();
            map.Emplace(202, 202); //13
            map.Emplace(131, 131); //15
            map.Emplace(597, 597); //15
            map.Emplace(681, 681); //14
            map.Emplace(893, 893); //14
            map.Emplace(516, 516); //14

            var count = 0;
            var count2 = 0;
            foreach (uint unused in map.Keys)
            {
                count++;
            }

            Assert.IsTrue(count == 6);

            foreach (uint unused in map.Values)
            {
                count2++;
            }

            Assert.IsTrue(count2 == 6);
        }

        [TestMethod]
        public void CreateMap()
        {
            FastMap<uint, uint> map = new FastMap<uint, uint>();
            map.Emplace(42, 42);
            map.Emplace(144, 144);
            map.Emplace(160, 160);
            map.Emplace(194, 194);
            map.Emplace(207, 207);
            map.Emplace(220, 220);

            Assert.IsTrue(map.Count == 6);
        }

        [TestMethod]
        public void CreateMapPartTwo()
        {
            FastMap<uint, uint> map = new FastMap<uint, uint>();
            map.Emplace(202, 202); //13
            map.Emplace(131, 131); //15
            map.Emplace(597, 597); //15
            map.Emplace(681, 681); //14
            map.Emplace(893, 893); //14
            map.Emplace(516, 516); //14

            foreach (var key in map.Keys)
            {
                map.Get(key, out var result);
                if (result == 0)
                {
                    Assert.Fail("result cannot be null");
                }
            }
        }

        [TestMethod]
        public void CopyMap()
        {
            FastMap<uint, uint> map1 = new FastMap<uint, uint>();

            map1.Emplace(1, 1);
            map1.Emplace(11, 11);
            map1.Emplace(112, 112);
            map1.Emplace(211, 211);

            FastMap<uint, uint> map2 = new FastMap<uint, uint>();

            map2.Emplace(41, 41);
            map2.Emplace(410, 410);
            map2.Emplace(4300, 4300);

            map2.Copy(map1);

            Assert.AreEqual(7, map2.Count);
        }

        [TestMethod]
        public void AssertIndexorGet()
        {
            var faster = new FastMap<uint, uint>(16, 0.88);
            faster.Emplace(454, 454);
            faster.Emplace(438, 438);
            faster.Emplace(422, 422);
            faster.Emplace(406, 406);
            faster.Emplace(390, 390);
            faster.Emplace(3912340, 390);
            faster.Update(390, 2345);
            Assert.IsTrue(faster[390] == 2345);
        }

        [TestMethod]
        public void AssertIndexorSet()
        {
            var faster = new FastMap<uint, uint>(16, 0.88);
            faster.Emplace(454, 454);
            faster.Emplace(438, 438);
            faster.Emplace(422, 422);
            faster.Emplace(406, 406);
            faster.Emplace(390, 390);
            faster.Emplace(3912340, 390);
            faster[390] = 1;

            Assert.IsTrue(faster[390] == 1);
        }

        [TestMethod]
        public void AssertGetMisses()
        {
            var faster = new FastMap<uint, uint>(16, 0.88);

            faster.Emplace(454, 454);
            faster.Emplace(438, 438);
            faster.Emplace(422, 422);
            faster.Emplace(406, 406);
            faster.Emplace(390, 390);
            faster.Get(372, out var result);

            Assert.IsTrue(result != 372);
        }

        [TestMethod]
        public void AssertGetAfterResize()
        {
            var faster = new FastMap<uint, uint>(16, 0.9);

            faster.Emplace(454, 454);
            faster.Emplace(438, 438);
            faster.Emplace(422, 422);
            faster.Emplace(406, 406);
            faster.Emplace(390, 390);
            faster.Emplace(356, 390);
            faster.Emplace(391, 390);
            faster.Emplace(392, 390);
            faster.Emplace(393, 390);
            faster.Emplace(394, 390);
            faster.Emplace(395, 390);
            faster.Emplace(396, 390);
            faster.Emplace(397, 390);
            faster.Emplace(398, 390);
            faster.Emplace(399, 390);

            faster.Emplace(330, 390);
            faster.Emplace(310, 390);
            faster.Emplace(311, 390);

            faster.Get(390, out var result);

            Assert.IsTrue(result == 390);
        }

        [TestMethod]
        public void AssertRemoval()
        {
            var faster = new FastMap<uint, uint>(16, 0.88);

            faster.Emplace(454, 454);
            faster.Emplace(33, 33);
            faster.Emplace(438, 438);
            faster.Emplace(422, 422);
            faster.Emplace(406, 406);
            faster.Emplace(390, 390);
            faster.Emplace(380, 380);
            faster.Emplace(340, 340);
            faster.Remove(422); // remove 422

            Assert.IsTrue(faster.Count == 7);
        }

        [TestMethod]
        public void Assertso()
        {
            var faster = new FastMap<uint, uint>(16, 0.88);

            faster.Emplace(454, 454);
            faster.Emplace(33, 33);
            faster.Emplace(438, 438);
            faster.Emplace(422, 422);
            faster.Emplace(406, 406);
            faster.Emplace(390, 390);
            faster.Emplace(380, 380);
            faster.Emplace(340, 340);

            var xfaster = new FastMap<uint, uint>(32, 0.88);

            xfaster.Emplace(454, 454);
            xfaster.Emplace(33, 33);
            xfaster.Emplace(438, 438);
            xfaster.Emplace(422, 422);
            xfaster.Emplace(406, 406);
            xfaster.Emplace(390, 390);
            xfaster.Emplace(380, 380);
            xfaster.Emplace(340, 340);


        }

        [TestMethod]
        public void AssertbackShiftRemoval()
        {
            var fmap = new FastMap<uint, uint>(1000000, 0.9);

            foreach (var k in keys)
            {
                if (!fmap.Emplace(k, k))
                {
                    throw new InternalTestFailureException("Error occured while add");
                }
            }

            foreach (var k in keys)
            {
                fmap.Get(k, out var result);
                if (result == 0)
                {
                    var index = fmap.IndexOf(k);
                    throw new InternalTestFailureException("Error occured while get");
                }
            }

            foreach (var k in keys)
            {
                if (!fmap.Remove(k))
                {
                    var index = fmap.IndexOf(k);

                    throw new InternalTestFailureException("Error occured while removing");
                }
            }

            Assert.IsTrue(fmap.Count == 0);
        }

        [TestMethod]
        public void AssertDuplicateKey()
        {
            var faster = new FastMap<uint, uint>(16, 0.90);

            faster.Emplace(454, 454);
            faster.Emplace(454, 454);

            Assert.IsTrue(faster.Count == 1);
        }

        [TestMethod]
        public void AsserClear()
        {
            var faster = new FastMap<uint, uint>(16, 0.90);

            faster.Emplace(454, 454);
            faster.Emplace(454, 454);

            faster.Clear();

            Assert.IsTrue(faster.Count == 0);
        }

        [TestMethod]
        public void AssertEmplaceOrUdateReturnsUpdatedValue()
        {
            //assign
            var map = new FastMap<int, int>(16, 0.5);

            //act
            map.EmplaceOrUpdate(1, 1);
            map.EmplaceOrUpdate(1, 2);

            map.Get(1, out var result);

            //assert
            Assert.AreEqual(2, result);
        }


    }
}
