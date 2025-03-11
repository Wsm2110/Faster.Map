using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using System.Collections.Generic;
using System.Collections;
using System.Numerics;

namespace Faster.Map.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            //BenchmarkRunner.Run<AddBenchmark>();
            BenchmarkRunner.Run<GetBenchmark>();
            //BenchmarkRunner.Run<UpdateBenchmark>();
            //BenchmarkRunner.Run<RemoveBenchmark>();
        }
    }
}
