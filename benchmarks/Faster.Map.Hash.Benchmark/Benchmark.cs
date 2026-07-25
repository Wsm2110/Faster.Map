using BenchmarkDotNet.Attributes;
using Faster.Map.Hashing.Algorithm;
using System.Runtime.InteropServices;

namespace Faster.Map.Hash.Benchmark
{
    public class Benchmark
    {
        private FastHash _hash = new FastHash();
        private string source;

        [Params(100, 1000, 10000)]
        public uint StringLength { get; set; }

        [Params(10000)]
        public uint Iterations { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            FastHash.CreateSeed();

            var data = Enumerable.Range(0, (int)StringLength).Select(i => (char)i).ToArray();

            source = new string(data);
        }

        [Benchmark]
        public void RunFastHash()
        {
            for (int i = 0; i < Iterations; ++i)
            {
                FastHash.HashU64(MemoryMarshal.AsBytes(source.AsSpan()));
            }
        }

        [Benchmark]
        public void RunMarvinHash()
        {
            for (int i = 0; i < Iterations; ++i)
            {
                source.GetHashCode();
            }
        }

    }
}
