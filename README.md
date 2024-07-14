# Faster.Map

Faster.Map is a collection of high-performance (concurrent) hashmaps implemented in C#

## Features:

* Optimized Performance: Each hashmap in Faster.Map is finely tuned for performance, ensuring rapid key-value pair operations even under heavy workloads.
* Memory Efficiency: Designed with memory optimization in mind, Faster.Map minimizes overhead to maximize efficiency, making it suitable for resource-constrained environments.
* Variety of Implementations: Choose from different hashmap implementations, including DenseMap with SIMD instructions, RobinHoodMap with linear probing, and QuadMap using triangular numbers, each offering unique advantages for specific use cases.
* Common Interface: All hashmaps in Faster.Map share the same set of functions, including Emplace, Get(), Update(), Remove(), and GetOrUpdate(), providing consistency and ease of use across implementations.


## Available Implementations:

 * DenseMap    - Harnesses SIMD (Single Instruction, Multiple Data) instructions for parallel processing, resulting in accelerated lookup times.                
* RobinHoodMap - is a high-performance hashmap using lineair probing .
* QuadMap      - is a high-performance hashmap using quadratic probing.
* CMap         - is a high-performance, thread-safe, lockfree concurrent hash map that uses open addressing, quadratic probing, and Fibonacci hashing to manage key-value pairs. The default load factor is set to 0.5, meaning the hash map will resize when it is half full.

* Installation:

You can include Faster.Map in your C# project via NuGet Package Manager:
```
Install-Package Faster.Map
```

## How to use

### DenseMap Example

```C#
// Example usage in C# (using DenseMap with SIMD Instructions)
using Faster.Map.DenseMap;

// Create a DenseMapSIMD object
var map = new DenseMap<string, string>();

// Add key-value pairs
map.Emplace("key1", "value1");

// Retrieve values
var result = map.Get("key1", out var retrievedValue);

Console.WriteLine(retrievedValue); // Output: "value1"
  ``` 

 ## Tested on platforms:
* x86
* x64
* arm
* arm64

## Benchmark

The mean is divided by the length

```
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3737/23H2/2023Update/SunValley3)
12th Gen Intel Core i5-12500H, 1 CPU, 16 logical and 12 physical cores
.NET SDK 9.0.100-preview.2.24157.14
  [Host]     : .NET 9.0.0 (9.0.24.12805), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.0 (9.0.24.12805), X64 RyuJIT AVX2

```
### Retrieving a million pre-generated keys

| Method       | Length  | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------- |-------- |---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| DenseMap     | 1000000 | 11.46 ms | 0.181 ms | 0.169 ms | 11.54 ms |  0.64 |    0.03 |     289 B |      12 B |        0.52 |
| RobinhoodMap | 1000000 | 10.85 ms | 0.110 ms | 0.098 ms | 10.85 ms |  0.61 |    0.03 |     182 B |      12 B |        0.52 |
| QuadMap      | 1000000 | 14.26 ms | 0.083 ms | 0.070 ms | 14.25 ms |  0.79 |    0.04 |     221 B |      12 B |        0.52 |
| Dictionary   | 1000000 | 17.68 ms | 0.353 ms | 0.943 ms | 17.39 ms |  1.00 |    0.00 |     412 B |      23 B |        1.00 |

### Adding a million keys

| Method       | Length  | Mean      | Error     | StdDev    |
|------------- |-------- |----------:|----------:|----------:|
| DenseMap     | 1000000 |  7.893 ms | 0.1573 ms | 0.3248 ms |
| RobinhoodMap | 1000000 | 11.132 ms | 0.2184 ms | 0.2428 ms |
| QuadMap      | 1000000 | 13.154 ms | 0.2606 ms | 0.5553 ms |
| Dictionary   | 1000000 | 16.417 ms | 0.2976 ms | 0.4545 ms |

### Updating a million keys

| Method       | Length  | Mean     | Error    | StdDev   | Allocated |
|------------- |-------- |---------:|---------:|---------:|----------:|
| DenseMap     | 1000000 | 10.67 ms | 0.167 ms | 0.148 ms |      12 B |
| RobinhoodMap | 1000000 | 10.67 ms | 0.075 ms | 0.059 ms |      12 B |
| QuadMap      | 1000000 | 13.50 ms | 0.161 ms | 0.151 ms |      12 B |
| Dictionary   | 1000000 | 17.68 ms | 0.331 ms | 0.325 ms |      23 B |

### Removing a million keys

| Method       | Length  | Mean     | Error    | StdDev   | Median   | Allocated |
|------------- |-------- |---------:|---------:|---------:|---------:|----------:|
| DenseMap     | 1000000 | 12.97 ms | 0.256 ms | 0.599 ms | 13.03 ms |     736 B |
| RobinhoodMap | 1000000 | 15.76 ms | 0.308 ms | 0.303 ms | 15.79 ms |     736 B |
| QuadMap      | 1000000 | 15.10 ms | 0.288 ms | 0.296 ms | 15.19 ms |     736 B |
| Dictionary   | 1000000 | 18.79 ms | 0.374 ms | 1.023 ms | 18.36 ms |     736 B |

### Add and resize

| Method       | Length  | Mean     | Error    | StdDev   | Median   |
|------------- |-------- |---------:|---------:|---------:|---------:|
| DenseMap     | 1000000 | 21.36 ms | 0.391 ms | 0.522 ms | 21.29 ms |
| RobinhoodMap | 1000000 | 25.93 ms | 0.512 ms | 0.629 ms | 25.79 ms |
| QuadMap      | 1000000 | 33.80 ms | 0.936 ms | 2.761 ms | 32.26 ms |
| Dictionary   | 1000000 | 36.22 ms | 0.723 ms | 1.904 ms | 36.43 ms |

### Add string benchmark

| Method       | Length  | Mean     | Error    | StdDev   | Allocated |
|------------- |-------- |---------:|---------:|---------:|----------:|
| DenseMap     | 1000000 | 55.88 ms | 0.723 ms | 0.641 ms |      74 B |
| RobinhoodMap | 1000000 | 73.16 ms | 0.377 ms | 0.335 ms |     105 B |
| QuadMap      | 1000000 | 53.19 ms | 0.494 ms | 0.438 ms |      74 B |
| Dictionary   | 1000000 | 33.28 ms | 0.529 ms | 0.469 ms |      23 B |

### Create StringWrapperBenchmark (cached hashcode)
| Method       | Length  | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------- |-------- |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| DenseMap     | 1000000 | 38.58 ms | 0.467 ms | 0.437 ms |  1.05 |    0.04 |      53 B |        1.08 |
| RobinhoodMap | 1000000 | 38.57 ms | 0.477 ms | 0.446 ms |  1.05 |    0.04 |      53 B |        1.08 |
| QuadMap      | 1000000 | 40.21 ms | 0.421 ms | 0.393 ms |  1.09 |    0.04 |      57 B |        1.16 |
| Dictionary   | 1000000 | 37.42 ms | 0.694 ms | 1.120 ms |  1.00 |    0.00 |      49 B |        1.00 |
