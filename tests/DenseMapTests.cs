using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Faster.Map;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Faster.UnitTest
{
    [TestClass]
    public class DenseMapTests
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
        public void AssertRetrievalFromMap()
        {
            var densemap = new DenseMap<uint, uint>(5, 0.75);
            densemap.Emplace(1, 100);
            densemap.Emplace(2, 200);
            densemap.Emplace(3, 300);

            densemap.Get(3, out var result);

            Assert.IsTrue(result == 300);
        }

        [TestMethod]
        public void AssertAddingEntriesShouldResize()
        {
            var densemap = new DenseMap<uint, uint>(5, 0.75);
            densemap.Emplace(1, 100);
            densemap.Emplace(2, 200);
            densemap.Emplace(3, 300);
            densemap.Emplace(4, 400);
            densemap.Emplace(5, 500);

            Assert.IsTrue(densemap.Size == 19);
        }

        [TestMethod]
        public void AssertRetrievalFromMapAfterResize()
        {
            var densemap = new DenseMap<uint, uint>(5, 0.75);

            densemap.Emplace(1, 100);
            densemap.Emplace(2, 200);
            densemap.Emplace(3, 300);
            densemap.Emplace(4, 400);
            densemap.Emplace(5, 500);

            densemap.Get(3, out var result);

            Assert.IsTrue(result == 300);
        }

        [TestMethod]
        public void AssertUpdate()
        {
            //Assign
            var faster = new DenseMap<ulong, ulong>(16, 0.88);
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
        public void AssertAddingAndRemovingSetsProperOffsetPartOne()
        {
            //assign
            DenseMap<uint, uint> map = new DenseMap<uint, uint>(16);

            map.Emplace(202, 202); //13
            map.Emplace(131, 131); //15
            map.Emplace(597, 597); //15
            map.Emplace(681, 681); //14
            map.Emplace(893, 893); //14
            map.Emplace(516, 516); //14


            //act
            map.Remove(681);
            map.Remove(893);      
        }

        [TestMethod]
        public void AssertbackShiftRemoval()
        {
            var fmap = new DenseMap<uint, uint>(1000000, 0.75);

            foreach (var k in keys.Take(750000))

            {
                if (k == 1809306700) 
                {
                
                }


                if (!fmap.Emplace(k, k))
                {
                    throw new InternalTestFailureException("Error occured while add");
                }
            }

            foreach (var k in keys.Take(750000))
            {
                if (!fmap.Get(k, out var result))
                {

                    var index = fmap.IndexOf(k);

                    throw new InternalTestFailureException("Error occured while get");
                }
            }

            foreach (var k in keys.Take(750000))
            {
                if (!fmap.Remove(k))
                {
                    var index = fmap.IndexOf(k);

                    throw new InternalTestFailureException("Error occured while removing");
                }
            }

            Assert.IsTrue(fmap.Count == 0);
        }
    }
}
