[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Faster.Map.RobinhoodMap.Tests
{
    public class RobinhoodBenchmarkFixture
    {
        public IList<uint> CreateKeys()
        {
            var output = File.ReadAllText("Numbers.txt");
            var splittedOutput = output.Split(',');
            var keys = new uint[1000000];

            for (var index = 0; index < 1000000; index++)
            {
                keys[index] = uint.Parse(splittedOutput[index]);
            }

            return keys;
        }

        public RobinhoodMap<uint, uint> CreatePreLoadedMap(IList<uint> keys)
        {
            var map = new RobinhoodMap<uint, uint>();

            foreach (var key in keys)
            {
                map.Emplace(key, key);
            }

            return map;
        }

    }
}