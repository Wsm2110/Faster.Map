# Faster.Map

**A high-performance HashMap library for .NET - built for speed, predictable latency, and low memory overhead.**

[![NuGet Version](https://img.shields.io/nuget/v/Faster.Map.svg?style=flat-square&logo=nuget&label=NuGet)](https://www.nuget.org/packages/Faster.Map)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Faster.Map.svg?style=flat-square&logo=nuget&label=Downloads)](https://www.nuget.org/packages/Faster.Map)
[![License](https://img.shields.io/github/license/Wsm2110/Faster.Map?style=flat-square)](https://github.com/Wsm2110/Faster.Map/blob/main/LICENSE)
[![Stars](https://img.shields.io/github/stars/Wsm2110/Faster.Map?style=flat-square&logo=github)](https://github.com/Wsm2110/Faster.Map/stargazers)
[![Issues](https://img.shields.io/github/issues/Wsm2110/Faster.Map?style=flat-square)](https://github.com/Wsm2110/Faster.Map/issues)
[![.NET](https://img.shields.io/badge/.NET-7%20%7C%208%20%7C%209%20%7C%2010-512BD4?style=flat-square&logo=dotnet)](https://github.com/Wsm2110/Faster.Map)

If `Dictionary<TKey, TValue>` or `ConcurrentDictionary<TKey, TValue>` is the bottleneck in your hot path, Faster.Map gives you four purpose-built alternatives, each tuned for a different access pattern, instead of one generic compromise.

> If Faster.Map saves you a few microseconds (or a few million of them), consider starring the repo. It genuinely helps the project grow.

---

## Table of Contents

- [Why Faster.Map](#why-faster-map)
- [Available Implementations](#available-implementations)
- [Choosing the Right Map](#choosing-the-right-map)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Custom Hashing](#custom-hashing)
- [Benchmarks](#benchmarks)
- [What's New](#whats-new)
- [Supported Platforms](#supported-platforms)
- [Contributing](#contributing)
- [License](#license)

---

## Why Faster.Map

Standard `Dictionary` and `ConcurrentDictionary` are reliable defaults, but they start to show their limits under high-density tables, heavy concurrent access, or tight allocation budgets, exactly the conditions that real-time systems, game engines, caching layers, and high-throughput services live in.

Faster.Map takes a different approach: rather than one general-purpose design, it ships four specialized implementations, so you pick the tradeoff that matches your workload instead of paying for one you don't need.

**Key benefits:**

- High-performance lookup, insert, update, and remove operations
- Low allocation overhead on hot paths
- Cache-friendly data layouts
- SIMD acceleration where applicable
- Pluggable, swappable hash functions
- Multiple map strategies for different access patterns
- Support for modern, actively-maintained .NET targets

---

## Available Implementations

### BlitzMap

A flat, open-addressing hashmap tuned for cache locality and strong collision handling. It's the **default recommendation**: fast across the board, with no sharp edges.

Best for: general-purpose high performance, low-latency workloads, balanced read/write usage, "just give me the fast one."

### DenseMap

Uses SIMD instructions to compare multiple keys in parallel, cutting lookup latency in dense tables.

Best for: high-density datasets, real-time lookups, CPU-bound workloads, any scenario where SIMD gives a measurable edge.

### RobinHoodMap

Robin Hood hashing with linear probing keeps probe distances balanced and clustering low.

Best for: read-heavy workloads, predictable lookup behavior, stable, low-variance latency.

### CMap

A lock-free concurrent hashmap using open addressing, quadratic probing, and Fibonacci hashing: thread-safe performance without a coarse-grained lock.

Best for: multi-threaded applications, high-throughput concurrent access, minimizing contention.

---

## Choosing the Right Map

| Implementation | Best Use Case | Default Choice? |
|---|---|---|
| **BlitzMap** | General-purpose speed, balanced read/write | Yes, start here |
| **DenseMap** | High-density tables, SIMD-accelerated lookups | When density is high |
| **RobinHoodMap** | Read-heavy, retrieval-focused workloads | When reads dominate |
| **CMap** | Lock-free multi-threaded access | When thread-safety is required |

---

## Installation

```bash
dotnet add package Faster.Map
```

or via the Package Manager Console:

```powershell
Install-Package Faster.Map
```

---

## Quick Start

### Using BlitzMap

```csharp
var map = new BlitzMap<int, string>();

map.Insert(1, "Value One");
map.Insert(2, "Value Two");
map.InsertUnique(3, "Value Three");
map.InsertOrUpdate(2, "Updated");

if (map.Get(1, out var value))
{
    Console.WriteLine($"Key 1 has value: {value}");
}

map.Update(1, "Updated value one");
map.Remove(1);
```

### Using DenseMap

```csharp
var map = new DenseMap<int, string>();

map.Emplace(1, "Value One");
map.Emplace(2, "Value Two");

if (map.Get(1, out var value))
{
    Console.WriteLine($"Key 1 has value: {value}");
}

map.Remove(1);
```

---

## Custom Hashing

Faster.Map supports pluggable hash functions so you can tune distribution and throughput for your data shape and target hardware:

| Hasher | Notes |
|---|---|
| `WyHash` | High-speed, general-purpose |
| `XXHash3` | Optimized for throughput and low latency |
| `FastHash` | AES-based (requires hardware AES support) |
| `CrcHasher` | Non-cryptographic, hardware-accelerated on x86 (SSE4.2) and ARM64 |
| `DefaultHasher` | Falls back to .NET's built-in `GetHashCode()` |

```csharp
var map = new BlitzMap<int, string, XxHash3Hasher.String>();
map.Insert(1, "Value One");
map.Insert(2, "Value Two");
```

Custom hashing tends to pay off most on large datasets and string-heavy workloads, where distribution quality has an outsized effect on collision rates.

---

## Benchmarks

All figures below come from the benchmark suite in [`/benchmarks`](https://github.com/Wsm2110/Faster.Map/tree/main/benchmarks), run with BenchmarkDotNet on .NET 9.

### Results at a glance

Mean time per operation across 1,048,576 elements, lower is better:

| Implementation | Load Factor 0.1 | Load Factor 0.4 | Load Factor 0.8 |
|---|---:|---:|---:|
| **BlitzMap** | 337.2 us | 2,282.0 us | 6,661.6 us |
| **DenseMap** | 496.4 us | 2,161.9 us | **4,721.2 us** |
| Dictionary | 432.4 us | 3,242.6 us | 11,808.1 us |
| RobinHoodMap | 450.3 us | 3,331.5 us | 17,820.8 us |

A few honest takeaways, not just the flattering ones:

- At low load factors, the built-in `Dictionary` is already competitive. You're mainly buying headroom for later.
- Past a 0.4 load factor, `DenseMap`'s SIMD scanning pulls decisively ahead, running roughly 2.5x faster than `Dictionary` at 0.8.
- `RobinHoodMap`'s linear probing is great at low density but degrades sharply as tables fill up. Pick it for read-heavy, low-density workloads, not dense ones.
- `BlitzMap` stays close to the front across every load factor, which is why it's the default recommendation.

### Full benchmark gallery

<details>
<summary>Click to expand charts for Get / Insert / Update / Remove / Enumerate / String-key workloads</summary>

**Get**
![Get Benchmark by Load Factor](https://github.com/Wsm2110/Faster.Map/raw/main/Assets/Charts/get_benchmark_by_loadfactor.png)

**Insert**
![Insert Benchmark by Load Factor](https://github.com/Wsm2110/Faster.Map/raw/main/Assets/Charts/insert_benchmark_by_loadfactor.png)

**Update**
![Update Benchmark by Load Factor](https://github.com/Wsm2110/Faster.Map/raw/main/Assets/Charts/update_benchmark_by_loadfactor.png)

**Remove**
![Remove Benchmark by Load Factor](https://github.com/Wsm2110/Faster.Map/raw/main/Assets/Charts/remove_benchmark.png)

**Enumeration**
![Enumerable Benchmark by Load Factor](https://github.com/Wsm2110/Faster.Map/raw/main/Assets/Charts/enumerable_benchmark.png)

**String keys**
![Get String Benchmark by Load Factor](https://github.com/Wsm2110/Faster.Map/raw/main/Assets/Charts/get_string_benchmark.png)

**String keys, custom hash**
![Get String Custom Hash Benchmark by Load Factor](https://github.com/Wsm2110/Faster.Map/raw/main/Assets/Charts/get_string_custom_hash_benchmark.png)

**Large strings**
![Get Large String Benchmark by Load Factor](https://github.com/Wsm2110/Faster.Map/raw/main/Assets/Charts/largestringBenchmark.png)

**Large strings, custom hash**
![Large String Custom Hash Benchmark by Load Factor](https://github.com/Wsm2110/Faster.Map/raw/main/Assets/Charts/largestringcustomhash.png)

</details>

---

## What's New

Recent releases have focused on squeezing more out of `BlitzMap`'s hot path:

- 7 to 13.7% faster execution across load factors from a fresh round of low-level tuning.
- Signature validation and slot extraction fused into a single branchless operation, cutting redundant ALU work on the lookup path.
- Bucket traversal reordered to trigger out-of-order hardware prefetching, hiding memory latency during hash collisions.
- `CrcHasher` gained a hardware-accelerated ARM64 path, with a safe software fallback on unsupported hardware.
- Added GC-safety checks around uninitialized memory for reference-type values.
- Reworked probing math and memory marshaling to shrink IL size and help the JIT inline more aggressively.

See the [release notes](https://github.com/Wsm2110/Faster.Map/releases) for the full history.

---

## Supported Platforms

| | |
|---|---|
| **.NET** | 7, 8, 9, 10 |
| **Architectures** | x86, x64, ARM, ARM64 |

> Faster.Map targets modern .NET only. There's no .NET Framework or `netstandard` build. If you need those, pin to an older major version on NuGet.

---

## Contributing

Issues, pull requests, and [discussions](https://github.com/Wsm2110/Faster.Map/discussions) are welcome. If you're proposing a larger change, opening an issue first to talk through the approach is appreciated, especially for anything touching the probing or hashing internals.

If Faster.Map is working well for you in production, a comment in [Discussions](https://github.com/Wsm2110/Faster.Map/discussions) about your use case helps other people evaluate it too.

---

## License

MIT. See [LICENSE](https://github.com/Wsm2110/Faster.Map/blob/main/LICENSE) for details.

---

<div align="center">

**If this project helped you ship something faster, [star it on GitHub](https://github.com/Wsm2110/Faster.Map). It's the easiest way to support the work.**

[![Star History Chart](https://api.star-history.com/svg?repos=Wsm2110/Faster.Map&type=Date)](https://star-history.com/#Wsm2110/Faster.Map&Date)

</div>
