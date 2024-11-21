using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Faster.Map.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<GetBenchmark>(new DebugInProcessConfig());
            //BenchmarkRunner.Run<AddAndResizeBenchmark>();
            //BenchmarkRunner.Run<UpdateBenchmark>();
            //BenchmarkRunner.Run<RemoveBenchmark>();
            //  BenchmarkRunner.Run<GetBenchmark>();
            //BenchmarkRunner.Run<StringBenchmark>();
            // BenchmarkRunner.Run<StringWrapperBenchmark>();
        }
    }
}
