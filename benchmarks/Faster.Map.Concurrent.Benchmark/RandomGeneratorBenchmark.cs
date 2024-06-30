using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Faster.Map.Concurrent.CMap<int, int>;

namespace Faster.Map.Concurrent.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    public class RandomGeneratorBenchmark
    {
        [Params(100, 1000, 10000, 100000, 1000000)]
        public uint Length { get; set; }

        [Benchmark]
        public void Random8Benchmark()
        {
            //var random = new Random8(33);

            //for (int i = 0; i < 100; i++)
            //{
            //    var x = random.Generate(63);
            //}
        }


        ulong state;

        [Benchmark]
        public void Test()
        {
            for (int i = 0; i < Length; i++)
            {
                state *= 0xda942042e4dd58b5;
                var x = state >> 64;
            }
        }

        //[Benchmark]
        //public void RandomBenchmark()
        //{
        //    var random = new Random(33);

        //    for (int i = 0; i < Length; i++)
        //    {
        //        random.Next();
        //    }
        //}
    }
}