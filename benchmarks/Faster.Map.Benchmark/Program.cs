using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftWindowsTCPIP;
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
