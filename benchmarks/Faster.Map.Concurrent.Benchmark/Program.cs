using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

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