# Faster.Map — High-Performance HashMap for .NET

Faster.Map is a high-performance HashMap library for .NET built for speed, predictable latency, and efficient memory usage. It provides specialized map implementations for different workloads, including dense datasets, read-heavy access patterns, lock-free concurrent scenarios, and cache-friendly general-purpose use.

If you need a faster alternative to `Dictionary<TKey, TValue>` or `ConcurrentDictionary<TKey, TValue>` for performance-sensitive applications, Faster.Map gives you focused implementations designed to reduce overhead and improve throughput.

## Why Faster.Map

Faster.Map is designed for applications where standard dictionary performance is not enough.

It is a strong fit for:

* Real-time systems
* Game engines
* Caching layers
* High-throughput services
* Data-intensive applications
* Low-latency workloads
* Concurrent and multi-core environments

Instead of relying on one general-purpose design, Faster.Map provides multiple implementations so you can choose the right tradeoff for your workload.

## Key Benefits

* High-performance lookup, insert, update, and remove operations
* Low allocation overhead on hot paths
* Cache-friendly data layouts
* SIMD acceleration where applicable
* Custom hashing support
* Multiple map strategies for different access patterns
* Support for modern .NET targets

## Available Implementations

### DenseMap

DenseMap uses SIMD acceleration to compare keys in parallel and reduce lookup latency in dense tables.

Best for:

* High-density datasets
* Real-time lookups
* CPU-bound workloads
* Scenarios where SIMD provides a measurable advantage

### RobinHoodMap

RobinHoodMap uses Robin Hood hashing with linear probing to reduce clustering and keep probe distances balanced.

Best for:

* Read-heavy workloads
* Predictable lookup behavior
* Stable latency
* Balanced access patterns

### CMap

CMap is a lock-free concurrent HashMap using open addressing, quadratic probing, and Fibonacci hashing.

Best for:

* Multi-threaded applications
* High-throughput concurrent access
* Minimal contention
* Thread-safe performance without coarse locking

### BlitzMap

BlitzMap is a flat open-addressing HashMap optimized for cache locality and strong collision handling.

Best for:

* General-purpose high performance
* Low-latency workloads
* Balanced read/write usage
* Fast default performance across many scenarios

## Installation

Install Faster.Map from NuGet:

```bash
Install-Package Faster.Map
```

## Quick Start

### BlitzMap

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

### DenseMap

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

## Custom Hashing

Faster.Map supports pluggable hash functions so you can optimize for your data and platform.

Available hashers include:

* WyHash
* XXHash3
* FastHash
* CrcHasher
* DefaultHasher

Example:

```csharp
var map = new BlitzMap<int, string, XxHash3Hasher.String>();
map.Insert(1, "Value One");
map.Insert(2, "Value Two");
```

Custom hashing can improve distribution, reduce collisions, and increase lookup performance, especially for large datasets and string-heavy workloads.

## Choosing the Right Map

| Implementation | Best Use Case                                      |
| -------------- | -------------------------------------------------- |
| DenseMap       | High-density datasets and SIMD-accelerated lookups |
| RobinHoodMap   | Read-heavy workloads and stable probe behavior     |
| CMap           | Concurrent workloads requiring lock-free access    |
| BlitzMap       | General-purpose high performance and low latency   |

Recommendation: use BlitzMap as the default choice, DenseMap when table density is high, RobinHoodMap for retrieval-heavy workloads, and CMap when thread-safe concurrent access is the priority.

## Supported Platforms

Faster.Map supports:

* .NET 7
* .NET 8
* .NET 9
* .NET 10
* x86
* x64
* ARM
* ARM64

## Benchmarks

Benchmark results are included in the repository to compare Faster.Map with `Dictionary<TKey, TValue>` across common workloads such as get, insert, update, remove, and enumeration.

The benchmark suite includes:

* Integer key workloads
* String key workloads
* Custom hash function comparisons
* Multiple load factors
* Dense and large-string scenarios

If your project depends on dictionary performance, the benchmark charts help you choose the right implementation for your workload.

## Design Goals

Faster.Map is built around a few core goals:

* Make hot-path operations fast
* Keep memory usage efficient
* Preserve predictable performance under load
* Support specialized hashing strategies
* Offer multiple implementations instead of one compromise design
* Stay practical for real .NET applications

## Repository

* GitHub: https://github.com/Wsm2110/Faster.Map
* NuGet package: Faster.Map

## Keywords

HashMap, hash table, .NET, C#, high-performance dictionary, memory-efficient collections, SIMD, lock-free concurrent map, fast lookup, low latency, cache-friendly data structures, custom hashing, Dictionary replacement
