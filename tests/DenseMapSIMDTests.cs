using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Faster.Map.Core.Tests
{
    [TestClass]
    public class DenseMapSIMDTests
    {
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

            //     Shuffle(new Random(), keys);
        }

        [TestMethod]
        public void AssertDailyUseCaseWithoutResize()
        {
            var fmap = new DenseMapSIMD<uint, uint>(1000000);

            foreach (var k in keys.Take(900000))
            {
                if (!fmap.Emplace(k, k))
                {
                    throw new InternalTestFailureException("Error occured while add");
                }
            }

            Assert.AreEqual(900000, fmap.Count);

            //find all entries from map

            //var offset = 0;
            foreach (var k in keys.Take(900000))
            {
                if (!fmap.Get(k, out var result))
                {

                    var index = fmap.IndexOf(k);

                    throw new InternalTestFailureException("Error occured while get");
                }
            }

            //remove all entries from map

            foreach (var k in keys.Take(900000))
            {
                if (!fmap.Remove(k))
                {
                    var index = fmap.IndexOf(k);

                    throw new InternalTestFailureException("Error occured while removing");
                }
            }

            Assert.IsTrue(fmap.Count == 0);

            //map full of tombstones, try inserting again

            foreach (var k in keys.Take(900000))
            {
                if (!fmap.Emplace(k, k))
                {
                    var index = fmap.IndexOf(k);

                    throw new InternalTestFailureException("Error occured while removing");
                }
            }

            Assert.AreEqual(900000, fmap.Count);
        }

        [TestMethod]
        public void AssertDailyUseCaseWithResize()
        {
            var fmap = new DenseMapSIMD<uint, uint>(16);

            foreach (var k in keys.Take(900000))
            {   
                if (!fmap.Emplace(k, k))
                {
                    throw new InternalTestFailureException("Error occured while add");
                }
            }

            Assert.AreEqual(900000, fmap.Count);

            //find all entries from map

            //var offset = 0;
            foreach (var k in keys.Take(900000))
            {
                if (!fmap.Get(k, out var result))
                {
                    var index = fmap.IndexOf(k);

                    throw new InternalTestFailureException("Error occured while get");
                }
            }

            //remove all entries from map

            foreach (var k in keys.Take(900000))
            {
                if (!fmap.Remove(k))
                {
                    var index = fmap.IndexOf(k);

                    throw new InternalTestFailureException("Error occured while removing");
                }
            }

            Assert.IsTrue(fmap.Count == 0);

            //map full of tombstones, try inserting again

            foreach (var k in keys.Take(900000))
            {
                if (!fmap.Emplace(k, k))
                {
                    var index = fmap.IndexOf(k);

                    throw new InternalTestFailureException("Error occured while removing");
                }
            }

            Assert.AreEqual(900000, fmap.Count);
        }

        [TestMethod]
        public void AssertUnsuccesfulLookupReturnsFalse()
        {
            DenseMapSIMD<uint, uint> map = new DenseMapSIMD<uint, uint>(1000000);

            var found = map.Get(345, out var result);
            result = 0;

            Assert.AreEqual(false, found);
            Assert.IsTrue(result == 0);
        }

        [TestMethod]
        public void AssertCopyMapToAnother()
        {
            DenseMapSIMD<uint, uint> map = new DenseMapSIMD<uint, uint>(16);
            DenseMapSIMD<uint, uint> map2 = new DenseMapSIMD<uint, uint>(16);

            map.Emplace(1, 1);
            map.Emplace(2, 1);

            map2.Emplace(3, 1);
            map2.Emplace(4, 1);

            map.Copy(map2);

            Assert.IsTrue(4 == map.Count);
        }
    }
}
