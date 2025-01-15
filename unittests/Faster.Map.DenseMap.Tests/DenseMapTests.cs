using Faster.Map.DenseMap;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Faster.Map.DenseMap.Tests
{
    [TestClass]
    public class DenseMapTests
    {
        private uint[] keys;

        [TestInitialize]
        public void Setup()
        {          
            keys = new uint[1000000];
            var random = new Random(3);
            for (var index = 0; index < 1000000; index++)
            {
                keys[index] = (uint)random.Next();
            }

            //     Shuffle(new Random(), keys);
        }

        [TestMethod]
        public void AssertAddingDuplicateKeysShouldFail()
        {
            //arrange
            var map = new DenseMap<uint, uint>(16);

            //act
            map.Emplace(1, 1);
            map.Emplace(1, 2);

            Assert.IsTrue(map[1] == 2);
        }

        [TestMethod]
        public void AssertUpdateEntryInMapReturnsProperValue()
        {
            //arrange
            var map = new DenseMap<uint, uint>(16);
            map.Emplace(1, 1);

            //act
            map.Update(1, 100);

            //assert
            map.Get(1, out var result);

            Assert.AreEqual(result, (uint)100);
        }

        [TestMethod]
        public void AssertUpdateEntryWhileKeyIsNotInMap()
        {
            //arrange
            var map = new DenseMap<uint, uint>(16);

            //act
            map.Update(1, 100);

            //assert
            var result2 = map.Get(1, out var result);

            Assert.AreEqual(result, (uint)0);
            Assert.AreEqual(result2, false);
        }

        [TestMethod]
        public void AssertContainsEntryInMapReturnsProperValue()
        {
            //arrange
            var map = new DenseMap<uint, uint>(16);
            map.Emplace(1, 1);

            //act
            var result = map.Contains(1);

            //assert         
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void AssertContainsEntryWhileKeyIsNotInMap()
        {
            //arrange
            var map = new DenseMap<uint, uint>(16);

            //act
            var result = map.Contains(1);

            //assert        
            Assert.AreEqual(result, false);
        }


        [TestMethod]
        public void AssertClearShouldResetMap()
        {
            //arrange
            var map = new DenseMap<uint, uint>(16);

            //act
            map.Emplace(1, 1);
            map.Clear();

            //assert        
            Assert.AreEqual(0, map.Count);
        }

        [TestMethod]
        public void AssertRemovingEntryWhileKeyIsNotInMap()
        {
            //arrange
            var map = new DenseMap<uint, uint>(16);

            //act
            var result = map.Remove(1);

            //assert
            Assert.AreEqual(result, false);
        }

        [TestMethod]
        public void AssertRemovingEntryShouldReduceCount()
        {
            //arrange
            var map = new DenseMap<uint, uint>(16);
            map.Emplace(1, 1);

            //act
            var result = map.Remove(1);

            //assert  
            Assert.AreEqual(result, true);
            Assert.AreEqual(0, map.Count);
        }

        [TestMethod]
        public void AssertResizingShouldDoubleInSize()
        {
            //arrange
            var map = new DenseMap<uint, uint>(16);

            //act    
            map.Emplace(1, 1);
            map.Emplace(2, 1);
            map.Emplace(3, 1);
            map.Emplace(4, 1);
            map.Emplace(5, 1);
            map.Emplace(6, 1);
            map.Emplace(7, 1);
            map.Emplace(8, 1);
            map.Emplace(9, 1);
            map.Emplace(10, 1);
            map.Emplace(11, 1);
            map.Emplace(12, 1);
            map.Emplace(13, 1);
            map.Emplace(14, 2);
            map.Emplace(15, 1);
            map.Emplace(16, 1);
            map.Emplace(17, 1);
            map.Emplace(18, 1);
            map.Emplace(19, 1);

            //assert  
            // 16 * 2) + 16

            Assert.AreEqual(32, (int)map.Size);
        }

        [TestMethod]
        public void AssertResizingShouldSetProperCount()
        {
            //arrange
            var map = new DenseMap<uint, uint>(16);

            //act    
            map.Emplace(1, 1);
            map.Emplace(2, 1);
            map.Emplace(3, 1);
            map.Emplace(4, 1);
            map.Emplace(5, 1);
            map.Emplace(6, 1);
            map.Emplace(7, 1);
            map.Emplace(8, 1);
            map.Emplace(9, 1);
            map.Emplace(10, 1);
            map.Emplace(11, 1);
            map.Emplace(12, 1);
            map.Emplace(13, 1);
            map.Emplace(14, 2);
            map.Emplace(15, 1);
            map.Emplace(16, 1);
            map.Emplace(17, 1);
            map.Emplace(18, 1);
            map.Emplace(19, 1);

            //assert  
            // 16 * 2) + 16

            Assert.AreEqual(19, map.Count);
        }

        [TestMethod]
        public void AssertRetrieveAfterResize()
        {
            //arrange
            var map = new DenseMap<uint, uint>(16);

            //act    
            map.Emplace(1, 1);
            map.Emplace(2, 1);
            map.Emplace(3, 1);
            map.Emplace(4, 1);
            map.Emplace(5, 1);
            map.Emplace(6, 1);
            map.Emplace(7, 1);
            map.Emplace(8, 1);
            map.Emplace(9, 1);
            map.Emplace(10, 1);
            map.Emplace(11, 1);
            map.Emplace(12, 1);
            map.Emplace(13, 1);
            map.Emplace(14, 2);
            map.Emplace(15, 1);
            map.Emplace(16, 1);
            map.Emplace(17, 1);
            map.Emplace(18, 1);
            map.Emplace(19, 1);

            var result = map.Get(19, out var r1);
            //assert  

            Assert.AreEqual(19, map.Count);
            Assert.AreEqual(result, true);
            Assert.AreEqual(1, (int)r1);
        }

        [TestMethod]
        public void AssertRetrievalByIndexor()
        {
            //arrange
            var map = new DenseMap<uint, uint>(16);

            //act    
            map.Emplace(1, 5);

            var result = map[1];

            //assert          
            Assert.AreEqual(5, (int)result);
        }

        [TestMethod]
        public void AssertUpdatingByIndexor()
        {
            //arrange
            var map = new DenseMap<uint, uint>(16);

            //act    
            map.Emplace(1, 5);

            map[1] = 10;

            map.Get(1, out var r1);

            //assert          
            Assert.AreEqual(10, (int)r1);
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void AssertUpdatingByIndexorWhileKeyNotFoundShouldThrow()
        {
            //arrange
            var map = new DenseMap<uint, uint>(16);

            //act    
            map.Emplace(1, 5);

            //throws
            map[5] = 10;
        }


        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void AssertRetrievingByIndexorWhileKeyNotFoundShouldThrow()
        {
            //arrange
            var map = new DenseMap<uint, uint>(16);

            //act    
            map.Emplace(1, 5);

            //throws
            var x = map[5];
        }

        [TestMethod]
        public void AssertEmplaceShouldIncreaseCount()
        {
            //arrange
            var map = new DenseMap<uint, uint>(16);

            //act
            map.Emplace(1, 1);

            //assert
            Assert.AreEqual(1, map.Count);
        }

        [TestMethod]
        public void AssertUnsuccesfulLookupReturnsFalse()
        {
            //arrange
            DenseMap<uint, uint> map = new DenseMap<uint, uint>(1000000);

            //act
            var found = map.Get(345, out var result);

            //assert
            Assert.AreEqual(false, found);
            Assert.IsTrue(result == 0);
        }

        [TestMethod]
        public void AssertDeletedEntriesShouldLeaveTombstones()
        {
            var fmap = new DenseMap<int, int>();
            fmap.Emplace(1, 1);
            fmap.Emplace(2, 1);
            fmap.Emplace(3, 1);
            fmap.Emplace(4, 1);
            fmap.Emplace(5, 1);
            fmap.Emplace(6, 1);
            fmap.Emplace(7, 1);
            fmap.Emplace(8, 1);
            fmap.Emplace(9, 1);
            fmap.Emplace(10, 1);
            fmap.Emplace(11, 1);



            fmap.Emplace(1, 1);




            // Assert.IsFalse(r);
        }

        [TestMethod]
        public void AssertEntryNotFound()
        {
            var fmap = new DenseMap<long, long>(1);
            fmap.Emplace(0L, 0L);

            var r = fmap.Get(1, out _);

            Assert.IsFalse(r);
        }


        [TestMethod]
        public void AssertCopyMapToAnother()
        {
            DenseMap<uint, uint> map = new DenseMap<uint, uint>(16);
            DenseMap<uint, uint> map2 = new DenseMap<uint, uint>(16);

            map.Emplace(1, 1);
            map.Emplace(2, 1);

            map2.Emplace(3, 1);
            map2.Emplace(4, 1);

            map.Copy(map2);

            Assert.IsTrue(4 == map.Count);
        }

        [TestMethod]
        public void AssertRemovingMultipleEntries()
        {
            //assign
            DenseMap<uint, uint> map = new DenseMap<uint, uint>(16);

            map.Emplace(1, 1);
            map.Emplace(2, 2);
            map.Emplace(3, 3);
            map.Emplace(4, 4);

            //act

            map.Remove(2);
            map.Remove(3);
            map.Remove(1);


            //assert
            map.Get(4, out var result);

            Assert.IsTrue((uint)4 == result);
        }

        [TestMethod]
        public void AssertRemovingMultipleEntriesShouldResultInReuseOfTombstones()
        {
            //assign
            DenseMap<uint, uint> map = new DenseMap<uint, uint>(16);

            map.Emplace(1, 1);
            map.Emplace(13, 2);
            map.Emplace(16, 3);
            map.Emplace(111, 4);
            //act
            map.Remove(1);
            //assert
            map.Emplace(1, 1);

            Assert.IsTrue(map.Count == 4);
        }

        [TestMethod]
        public void AssertEmplaceRemoveAndEmplaceAgainShouldLeaveTombstone()
        {
            //assign
            DenseMap<uint, uint> map = new DenseMap<uint, uint>(16);

            //act
            map.Emplace(1, 1);
            map.Remove(1);
            map.Emplace(1, 2);

            //assert
            map.Get(1, out var result);

            Assert.IsTrue((uint)2 == result);
        } 

        [TestMethod]
        public void AssertKeyEnumerator()
        {
            //assign
            var map = new DenseMap<uint, uint>(16, 0.5);

            //act
            map.Emplace(1, 1);
            map.Emplace(2, 2);
            map.Emplace(3, 2);
            map.Emplace(4, 2);

            var count = 0;
            foreach (var item in map.Keys)
            {
                ++count;
            }

            //assert
            Assert.AreEqual(count, 4);
        }

        [TestMethod]
        public void AssertValueEnumerator()
        {
            //assign
            var map = new DenseMap<uint, uint>(16, 0.5);

            //act
            map.Emplace(1, 1);
            map.Emplace(2, 2);
            map.Emplace(3, 2);
            map.Emplace(4, 2);

            var count = 0;
            foreach (var item in map.Values)
            {
                ++count;
            }

            //assert
            Assert.AreEqual(count, 4);
        }

        [TestMethod]
        public void AssertEntriesEnumerator()
        {
            //assign
            var map = new DenseMap<uint, uint>(16, 0.5);

            //act
            map.Emplace(1, 1);
            map.Emplace(2, 2);
            map.Emplace(3, 2);
            map.Emplace(4, 2);

            var count = 0;
            foreach (var item in map.Entries)
            {
                ++count;
            }

            //assert
            Assert.AreEqual(count, 4);
        }

        [TestMethod]
        public void AssertAnItemHasBeenLost()
        {
            List<long> keysList = new List<long>()
    {
        3053810, 6107620, 9161430, 12215240, 39699530, 36645720, 33591910, 30538100,
        27484290, 109937160, 106883350, 103829540, 100775730, 97721920, 82452870, 79399060, 76345250, 73291440, 70237630,
        198497650, 195443840, 192390030, 189336220, 186282410, 171013360, 167959550, 164905740, 161851930, 158798120, 128260020,
        125206210, 122152400, 119098590, 116044780, 94668110, 91614300, 88560490, 296219570, 293165760, 290111950, 287058140,
        284004330, 268735280, 265681470, 262627660, 259573850, 256520040, 225981940, 222928130, 219874320, 216820510, 213766700,
        207659080, 204605270, 201551460, 180174790, 177120980, 174067170, 137421450, 134367640, 131313830, 433641020, 430587210,
        427533400, 424479590, 421425780, 406156730, 403102920, 400049110, 396995300, 393941490, 363403390, 360349580, 357295770,
        354241960, 351188150, 345080530, 342026720, 338972910, 335919100, 332865290, 329811480, 326757670, 323703860, 317596240,
        314542430, 311488620, 308434810, 305381000, 302327190, 299273380, 274842900, 271789090, 253466230, 247358610, 244304800,
        241250990, 238197180, 235143370, 232089560, 229035750, 210712890, 149636690, 146582880, 143529070, 140475260, 601600570,
        598546760, 595492950, 592439140, 589385330, 574116280, 571062470, 568008660, 564954850, 561901040, 531362940, 528309130,
        525255320, 522201510, 519147700, 513040080, 509986270, 506932460, 503878650, 500824840, 497771030, 494717220, 491663410,
        485555790, 482501980, 479448170, 476394360, 473340550, 470286740, 467232930, 464179120, 461125310, 458071500, 455017690,
        451963880, 442802450, 439748640, 436694830, 415318160, 412264350, 409210540, 390887680, 387833870, 384780060, 381726250,
        378672440, 375618630, 372564820, 369511010, 366457200, 348134340, 277896710, 250412420, 781775360, 778721550, 775667740,
        772613930, 769560120, 754291070, 751237260, 748183450, 745129640, 742075830, 711537730, 708483920, 705430110, 702376300,
        699322490, 693214870, 690161060, 687107250, 684053440, 680999630, 677945820, 674892010, 671838200, 665730580, 662676770,
        659622960, 656569150, 653515340, 650461530, 647407720, 644353910, 641300100, 638246290, 635192480, 632138670, 622977240,
        619923430, 616869620, 613815810, 610762000, 607708190, 604654380, 586331520, 583277710, 580223900, 577170090, 558847230,
        555793420, 552739610, 549685800, 546631990, 543578180, 540524370, 537470560, 534416750, 516093890, 488609600, 448910070,
        445856260, 418371970, 320650050, 280950520, 183228600, 992488250, 989434440, 986380630, 983326820, 980273010, 965003960,
        961950150, 958896340, 955842530
    };

            long missingKey = 439748640L;

            Assert.IsTrue(keysList.Contains(missingKey), "Sanity check failed");

            var keysSet = new DenseMap<long, long>(190);
            keysSet.Emplace(0L, 0L);

            foreach (var key in keysList)
            {
                keysSet.Emplace(key, key);

                if (key == missingKey)
                {
                    Assert.IsTrue(keysSet.Contains(missingKey), "Sanity check failed");
                }
            }

            var result = keysSet.Contains(missingKey);

            Assert.IsTrue(result, "Entry has been lost");
        }

        [TestMethod]
        public void AssertAdjustCapacity()
        {
            var fmap = new DenseMap<long, long>(1);
            fmap.Emplace(0L, 0L);

            //hashmap has an overhead of + 16

            Assert.IsTrue(fmap.Size == 16);
        }
    }
}
