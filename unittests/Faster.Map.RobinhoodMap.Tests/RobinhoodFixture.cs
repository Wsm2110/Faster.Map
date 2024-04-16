using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Map.RobinHoodMap.Tests
{
    public class RobinhoodFixture : IDisposable
    {

        public IEnumerable<uint> LoadKeysFromFile()
        {
            var output = File.ReadAllText("Numbers.txt");
            var splittedOutput = output.Split(',');

            for (var index = 0; index < splittedOutput.Length; index++)
            {
                yield return uint.Parse(splittedOutput[index]);
            }
        }

        public RobinhoodMap<uint, uint> CreateMap(IEnumerable<uint> keys)
        {
            var map = new RobinhoodMap<uint, uint>();
            foreach (var key in keys)
            {
                map.Emplace(key, key);
            }

            return map;
        }

        public IEnumerable<uint> GenerateUniqueKeys(int amount)
        {
            var random = new Random();
            var keys = new RobinhoodMap<uint, uint>();

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

        public RobinhoodMap<uint, uint> CreateMap(uint amount)
        {
            var random = new Random();
            var map = new RobinhoodMap<uint, uint>(amount);

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
