using BenchmarkDotNet.Running;
using System;

namespace Faster.Map.Benchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Benchmark>();
        }
    }
}
