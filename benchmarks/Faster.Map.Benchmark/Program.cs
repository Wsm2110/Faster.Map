using BenchmarkDotNet.Running;
using System;

namespace Faster.Map.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            //BenchmarkRunner.Run<AddBenchmark>();
           // BenchmarkRunner.Run<EnumerableBenchmark>();
            //BenchmarkRunner.Run<UpdateBenchmark>();
            BenchmarkRunner.Run<GetBenchmark>();

            Console.ReadLine();
        }
    }
}
