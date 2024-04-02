using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Map.RobinhoodMap.Tests
{
    public class RobinhoodNumberFileTests(RobinhoodBenchmarkFixture fixture) : IClassFixture<RobinhoodBenchmarkFixture>
    {

        [Fact]
        public void Get_Preloaded_keys()
        {
            var keys = fixture.CreateKeys();
            var map = fixture.CreatePreLoadedMap(keys);
            map.Get(2294959796, out var retrievedValue);
        }


        [Fact]
        public void Get_Preloaded_keys_()
        {
            var keys = fixture.CreateKeys().Take(100000).ToList();
            var map = fixture.CreatePreLoadedMap(keys);
            foreach (var key in keys)
            {
                map.Get(key, out var r);
            }
        }
    }
}
