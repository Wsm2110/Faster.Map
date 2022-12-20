using BenchmarkDotNet.Running;
using Faster.Map.Benchmark;

namespace Faster.Map.Concurrent.Benchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<AddBenchmark>();
        }
    }
}