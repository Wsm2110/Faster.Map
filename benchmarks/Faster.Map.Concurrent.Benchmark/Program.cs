using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Faster.Map.Concurrent.Benchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            var config = new DebugInProcessConfig()
                .WithOptions(ConfigOptions.DisableOptimizationsValidator);
#else
			var config = ManualConfig.CreateMinimumViable ();
#endif
            BenchmarkRunner.Run<AddBenchmark>();
        }
    }
}