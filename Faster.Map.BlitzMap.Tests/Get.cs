
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Faster.Map.BlitzMap.Tests
{
    public class Get
    {
        [Fact]
        public void ShouldReturnCorrectKeyValue()
        {
            var length = 80_000_000;
            var rng1 = new Random(3);

            var keys = new int[length];

            for (int i = 0; i < length; i++)
            {
                var key = rng1.Next();
                keys[i] = key;
            }

            var rng2 = new Random(3);
            var map = new BlitzMap<int, int>(length, 0.9);

            for (int i = 0; i < length; i++)
            {
                if (i == 8) 
                {
                
                }

                map.Insert(keys[i], i + 2);
            }


            for (int i = 0; i < length; i++)
            {
                var result = map.Get(keys[i], out var value);
                if (result == false)
                {
                    Assert.Fail();
                }
            }



            //   Assert.Equal(12ul, result);
        }

    }
}
