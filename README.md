# ⚡ Faster.Map — High-Performance HashMap for .NET

**Faster.Map** is a **blazing-fast, memory-efficient HashMap library for .NET**.
It’s built to outperform `Dictionary<TKey,TValue>` and `ConcurrentDictionary<TKey,TValue>` by providing **SIMD acceleration**, **lock-free concurrency**, and **custom hashing algorithms**.

Designed for **real-time systems, game engines, caching layers**, and **data-intensive applications**, Faster.Map delivers exceptional speed, scalability, and predictable performance across varying load factors.

---

## 🚀 Why Faster.Map?

The goal of Faster.Map is to create a **more efficient and performant alternative** to .NET’s built-in `Dictionary` and `ConcurrentDictionary`.
While those are reliable, they can struggle under **high-density workloads**, **frequent concurrent operations**, or **tight memory constraints**.

Faster.Map solves those problems by providing **specialized implementations** optimized for different workloads.

---

## 🧩 Available Implementations

### **DenseMap** – SIMD-Accelerated Lookups
Harnesses **SIMD (Single Instruction, Multiple Data)** instructions for parallel key comparisons, drastically reducing lookup latency.
✅ Best for **high-density datasets**, **real-time lookups**, and **CPU-bound workloads**.

---

### **RobinHoodMap** – Linear Probing Strategy
Uses **Robin Hood hashing** to evenly distribute probe distances, minimizing clustering.
✅ Ideal for **retrieval-heavy applications** and **balanced workloads**.

---

### **CMap** – Lock-Free Concurrent HashMap
A **thread-safe**, **lock-free**, **open-addressing** HashMap using **quadratic probing** and **Fibonacci hashing**.
✅ Perfect for **multi-threaded environments** requiring **high throughput** and **minimal contention**.

---

### **BlitzMap** – Flat Open-Addressing HashMap
Employs a **linked bucket approach** similar to separate chaining but optimized for **cache locality** and **collision resilience**.
✅ The **fastest all-round implementation**, ideal for **general-purpose** and **low-latency** workloads.

---

## 📜 Table of Contents
- [Available Implementations](#-available-implementations)
- [Installation](#-installation)
- [Basic Usage](#-basic-usage)
- [Advanced Usage](#-advanced-usage)
- [Selecting the Right Hashmap](#-selecting-the-right-hashmap)
- [Tested Platforms](#-tested-on-platforms)
- [Benchmarks](#-benchmark)

---

## 📦 Installation

Install Faster.Map via NuGet:

```bash
Install-Package Faster.Map
```

Supports **.NET 6+, .NET 8, .NET Framework 4.8**, and cross-platform (x86, x64, ARM, ARM64).

---

## 🧠 Basic Usage Examples

### Example: Using BlitzMap
```csharp
var map = new BlitzMap<int, string>();
map.Insert(1, "Value One");
map.Insert(2, "Value Two");
map.InsertUnique(3, "Value Four");
map.InsertOrUpdate(2, "Updated");

if (map.Get(1, out var value))
    Console.WriteLine($"Key 1 has value: {value}");

map.Update(1, "Updated value one");
map.Remove(1);

var n = new BlitzMap<uint, uint>();
n.Insert(1,1);
map.Copy(n);
```

### Example: Using DenseMap
```csharp
var map = new DenseMap<int, string>();
map.Emplace(1, "Value One");
map.Emplace(2, "Value Two");

if (map.Get(1, out var value))
    Console.WriteLine($"Key 1 has value: {value}");

map.Remove(1);
```

---

## 🧩 Advanced Usage

### 🔑 Custom Hashing Algorithms
Faster.Map supports **pluggable hash functions** for maximum performance:
- **WyHash** – High-speed general purpose hashing.
- **XXHash3** – Optimized for throughput and low latency.
- **FastHash** – AES-based hashing (requires X86Aes support).
- **CrcHasher** – Non-cryptographic hash with good distribution (requires Sse42)
- **DefaultHasher** – .NET's built-in `GetHashCode()`.

Example:
```csharp
var map = new BlitzMap<int, string, XxHash3Hasher.String>();
map.Insert(1, "Value One");
map.Insert(2, "Value Two");
```
All hashers are in the `Faster.Map.Hasher` namespace.
👉 Custom hashing significantly improves lookup speed and reduces collisions in large datasets.

---

## 🧱 Tested on Platforms
- x86
- x64
- ARM
- ARM64

---

## 🧮 Selecting the Right HashMap

| Implementation | Best Use Case |
| --------------- | -------------- |
| **DenseMap** | High-density datasets, SIMD-accelerated lookups |
| **RobinHoodMap** | Retrieval-heavy workloads, stable latency |
| **CMap** | Lock-free multi-threaded performance |
| **BlitzMap** | General-purpose speed and consistent performance |

✅ **Recommendation:**  
Use **BlitzMap** for balanced performance, **DenseMap** for dense tables, **RobinHoodMap** for read-heavy workloads, and **CMap** for multi-threaded use.

---

# 🧪 Benchmarks

*(All benchmarks executed with BenchmarkDotNet v0.13.12 on .NET 9, Intel i5-12500H)*

These benchmarks demonstrate how Faster.Map compares to `Dictionary<TKey,TValue>` across GET, INSERT, UPDATE, REMOVE, and ENUMERATE workloads at various load factors.

---

### 📊 [Get Benchmark](#-get-benchmark)
![Get Benchmark by Load Factor](Assets/Charts/get_benchmark_by_loadfactor.png)

**🏆 BlitzMap** is the most consistent performer across all load factors.  
**🚀 DenseMap** excels at high densities via SIMD acceleration.  
**⚠️ RobinhoodMap** collapses after 0.5 load factor.  
**🔻 Dictionary** suffers from excessive collisions.  

**Conclusion:**  
For balanced workloads, choose **BlitzMap**; for dense workloads, choose **DenseMap**.

---

### 📊 [Insert Benchmark](#-insert-benchmark)
![Insert Benchmark by Load Factor](Assets/Charts/insert_benchmark_by_loadfactor.png)

**🏆 BlitzMap** remains the fastest overall.  
**🚀 DenseMap** shines at high load factors.  
**⚠️ RobinHoodMap** degrades above 0.5.  
**🔻 Dictionary** performs poorly under heavy loads.  

**Conclusion:**  
**BlitzMap** is the best general performer; **DenseMap** dominates full tables.

---

### 📊 [Update Benchmark](#-update-benchmark)
![Update Benchmark by Load Factor](Assets/Charts/update_benchmark_by_loadfactor.png)

**🏆 BlitzMap** leads beyond 0.5 load factor.  
**⚡ RobinhoodMap** dominates at lower densities.  
**🚀 DenseMap** struggles at low densities due to SIMD overhead.  

**Conclusion:**  
Use **RobinhoodMap** for sparse data; **BlitzMap** for dense updates.

---

### 📊 [Remove Benchmark](#-remove-benchmark)
![Remove Benchmark by Load Factor](Assets/Charts/remove_benchmark.png)

**🏆 RobinhoodMap** offers best scaling for removals.  
**🚀 Dictionary** wins at low densities.  
**⚠️ DenseMap** performs worst at high load factors.  

**Conclusion:**  
Choose **RobinhoodMap** for removal-heavy workloads.

---

### 📊 [Enumerable Benchmark](#-enumerable-benchmark)
![Enumerable Benchmark by Load Factor](Assets/Charts/enumerable_benchmark.png)

**🏆 BlitzMap** is fastest across all load factors.  
**⚡ RobinhoodMap** and **DenseMap** degrade significantly at high densities.  

**Conclusion:**  
For frequent iteration, **BlitzMap** is unmatched.

---

### 📊 [Get String Benchmark](#-get-string-benchmark)
![Get String Benchmark by Load Factor](Assets/Charts/get_string_benchmark.png)

**🏆 BlitzMap** scales best; **Dictionary** wins at low density.  
**🚀 DenseMap** struggles with string hashing.  

**Conclusion:**  
For large string keys, use **BlitzMap**.

---

### 📊 [Get String Custom Hash Benchmark](#-get-string-custom-hash-benchmark)
![Get String Custom Hash Benchmark by Load Factor](Assets/Charts/get_string_custom_hash_benchmark.png)

**🏆 BlitzMap + FastHash** is fastest overall.  
**🧠 WyHash** and **XXHash3** perform better than defaults.  

**Conclusion:**  
Use **FastHash** for best string lookup performance.

---

### 📊 [Get Large String Benchmark](#-get-large-string-benchmark)
![Get Large String Benchmark by Load Factor](Assets/Charts/largestringBenchmark.png)

**🏆 Dictionary** dominates at low load factors.  
**🚀 BlitzMap** maintains performance at high densities.  
**⚠️ DenseMap** degrades quickly.  

**Conclusion:**  
**Dictionary** wins for small tables; **BlitzMap** wins for scalability.

---

### 📊 [Large String Custom Hash Benchmark](#-large-string-custom-hash-benchmark)
![Large String Custom Hash Benchmark by Load Factor](Assets/Charts/largestringcustomhash.png)

**🏆 BlitzMapFastHash** is the clear winner.  
**WyHash** and **XXHash3** are strong alternatives.  
**Dictionary** performs worst at scale.  

**Conclusion:**  
For high-performance string key lookups, **BlitzMapFastHash** delivers the best scalability and speed.

---

## 🔍 SEO Keywords
`hashmap`, `fast dictionary`, `csharp`, `dotnet`, `lock-free`, `simd`, `memory-efficient`, `high-performance`, `collections`, `data-structures`, `xxhash`, `wyhash`, `fast-hashmap`, `performance-benchmark`
