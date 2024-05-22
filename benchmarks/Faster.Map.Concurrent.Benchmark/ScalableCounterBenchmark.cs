using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Disassemblers;
using Faster.Map.Concurrent.Benchmark.Counter;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Faster.Map.Concurrent.Benchmark
{
    public class ScalableCounterBenchmark
    {


        private const int N = 10000000; // Adjust as needed for your scale

        [Params(1, 8, 16, 32, 64, 128, 256, 512)] // Example thread counts to test scalability
        public int NumberOfThreads { get; set; }

        [Benchmark]
        public void ScalableCounter()
        {
            Counter32 counter = new Counter32();
            Parallel.For(0, N, new ParallelOptions { MaxDegreeOfParallelism = NumberOfThreads }, i =>
            {
                counter.Increment();
            });
        }

        [Benchmark]
        public void ScalableAtomicCounter()
        {
            uint i = 0;
            Parallel.For(0, N, new ParallelOptions { MaxDegreeOfParallelism = NumberOfThreads }, i =>
            {
                Interlocked.Increment(ref i);
            });

        }


    }
}
