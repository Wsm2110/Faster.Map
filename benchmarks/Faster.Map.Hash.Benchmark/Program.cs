using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Faster.Map.Hash.Benchmark;

BenchmarkRunner.Run<Benchmark>(new DebugInProcessConfig());