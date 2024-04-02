using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Map.QuadMap.Tests
{
    public class QuadMapFixture : IDisposable
    {
        public QuadMap<uint, uint> CreateMap(IEnumerable<uint> keys)
        {
            var map = new QuadMap<uint, uint>();
            foreach (var key in keys)
            {
                map.Emplace(key, key);
            }

            return map;
        }

        public IEnumerable<uint> GenerateUniqueKeys(int amount)
        {
            var random = new Random();
            var keys = new QuadMap<uint, uint>((uint)amount);

            do
            {
                var key = (uint)random.Next(1, int.MaxValue);
                if (!keys.Get(key, out var _))
                {
                    keys.Emplace(key, key);
                    yield return key;
                }
            } while (keys.Count < amount);
        }

        public QuadMap<uint, uint> CreateMap(uint amount)
        {
            var random = new Random();
            var map = new QuadMap<uint, uint>(amount);

            do
            {
                var key = (uint)random.Next(1, 20000000);
                map.Emplace(key, key);
            } while (map.Count < amount);

            return map;
        }

        public void Dispose()
        {

        }
    }
}
