# Faster.Map

The goal of Faster.Map is to create a more efficient and performant alternative to the Dictionary and ConcurrentDictionary classes provided by .NET.
These standard collections are widely used for key-value pair storage in .NET applications, but they have certain limitations, particularly in high-performance and concurrent scenarios.

## Available Implementations:

* DenseMap with SIMD Instructions:
        Harnesses SIMD (Single Instruction, Multiple Data) instructions for parallel processing, resulting in accelerated lookup times.
        Ideal for scenarios demanding high throughput and optimal CPU utilization.
* RobinHoodMap with Linear Probing:
        Employs linear probing to resolve hash collisions, reducing the likelihood of clustering and improving access speed.
        Suitable for applications where a balance between performance and simplicity is required. 
* CMap  is a high-performance, thread-safe, lockfree concurrent hashmap that uses open addressing, quadratic probing, and Fibonacci hashing to manage key-value pairs. The default load factor is set to 0.5, meaning the hash map will resize when it is half full. Note: this hashmap will only allocate once while resizing.

* Installation:

You can include Faster.Map in your C# project via NuGet Package Manager:
```
Install-Package Faster.Map
```

## Basic Usage

### Default Hasher

By default, `DenseMap` uses the `DefaultHasher<TKey>`, which computes hashes based on the .NET `GetHashCode` method and uses Knuth's Multiplicative Hashing

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

### Custom Hasher

By default, Faster.Map provides four built-in hashers, each optimized for different performance characteristics:

DefaultHasher
GxHasher
XxHash3StringHasher
XxHash3Hasher

You can provide your own `IHasher<TKey>` implementation to customize hash computation.

#### Step 1: Implement a Custom Hasher

```csharp
public class CustomIntHasher : IHasher<int>
{
    public ulong ComputeHash(int key)
    {
        return (ulong)(key * 2654435761); // Multiplicative hashing
    }
}
```

#### Step 2: Use the Custom Hasher in DenseMap

```csharp
var customHasher = new CustomIntHasher();
var map = new DenseMap<int, string>(customHasher);

map.Emplace(1, "Value One");
map.Emplace(42, "Value Two");

if (map.Get(42, out var value))
{
    Console.WriteLine($"Key 42 has value: {value}");
}

map.Update(42, "Updated Value Two");
map.Remove(1);
```

### Advanced Example: Hashing Strings with XxHash

#### Custom String Hasher

```csharp
public class XxHash3StringHasher : IHasher<string>
{
    public ulong ComputeHash(string key)
    {
        return XxHash3.HashToUInt64(MemoryMarshal.AsBytes(key.AsSpan()));
    }
}
```

#### Using the String Hasher

```csharp
var stringHasher = new XxHash3StringHasher();
var stringMap = new DenseMap<string, int>(stringHasher);

stringMap.Emplace("Hello", 100);
stringMap.Emplace("World", 200);

if (stringMap.Get("Hello", out var value))
{
    Console.WriteLine($"Key 'Hello' has value: {value}");
}

stringMap.Remove("World");
```

---

 ## Tested on platforms:
* x86
* x64
* arm
* arm64

## Benchmark

``` ini
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3880/23H2/2023Update/SunValley3)
12th Gen Intel Core i5-12500H, 1 CPU, 16 logical and 12 physical cores
.NET SDK 9.0.100-preview.2.24157.14
  [Host]     : .NET 9.0.0 (9.0.24.12805), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.0 (9.0.24.12805), X64 RyuJIT AVX2
```
### Get Benchmark

| Method       | Length   | Mean         | Error       | StdDev       | Median       | Allocated |
|--------------|----------|-------------:|------------:|-------------:|-------------:|----------:|
| **DenseMap**     | **1000**    |      **0.00183 ms** |   **0.00001 ms** |  **0.00001 ms** |  **0.70** |     **341 B** |         **-** |          **NA** |
| RobinhoodMap | 1000    |      0.00137 ms |   0.00000 ms |  0.00000 ms |  0.53 |     175 B |         - |          NA |
| Dictionary   | 1000    |      0.00260 ms |   0.00001 ms |  0.00001 ms |  1.00 |     412 B |         - |          NA |
| **DenseMap**     | **10000**   |     **0.02125 ms** |   **0.00007 ms** |  **0.00005 ms** |  **0.30** |     **338 B** |         **-** |          **NA** |
| RobinhoodMap | 10000   |     0.01335 ms |   0.00005 ms |  0.00004 ms |  0.19 |     175 B |         - |          NA |
| Dictionary   | 10000   |     0.07140 ms |   0.00030 ms |  0.00025 ms |  1.00 |     401 B |         - |          NA |
| **DenseMap**     | **100000**  |    **0.29921 ms** |   **0.00073 ms** |  **0.00064 ms** |  **0.28** |     **341 B** |         **-** |        **0.00** |
| RobinhoodMap | 100000  |    0.49631 ms |   0.00137 ms |  0.00128 ms |  0.46 |     175 B |       1 B |        1.00 |
| Dictionary   | 100000  |    1.06807 ms |   0.00240 ms |  0.00224 ms |  1.00 |     401 B |       1 B |        1.00 |
| **DenseMap**     | **400000**  |  **1.78020 ms** |  **0.03364 ms** | **0.03304 ms** |  **0.36** |     **341 B** |       **1 B** |        **0.17** |
| RobinhoodMap | 400000  |  2.28105 ms |  0.01908 ms |  0.01593 ms |  0.47 |     175 B |       3 B |        0.50 |
| Dictionary   | 400000  |  4.88311 ms |  0.04191 ms |  0.03920 ms |  1.00 |     401 B |       6 B |        1.00 |
| **DenseMap**     | **900000**  |  **5.80135 ms** |  **0.07350 ms** | **0.06137 ms** |  **0.40** |     **341 B** |       **6 B** |        **0.50** |
| RobinhoodMap | 900000  |  8.77417 ms |  0.08236 ms |  0.07704 ms |  0.61 |     175 B |      12 B |        1.00 |
| Dictionary   | 900000  | 14.45441 ms |  0.06885 ms |  0.05376 ms |  1.00 |     401 B |      12 B |        1.00 |
| **DenseMap**     | **1000000** | **10.84462 ms** |  **0.06526 ms** | **0.05785 ms** |  **0.66** |     **341 B** |      **12 B** |        **0.52** |
| RobinhoodMap | 1000000 | 10.49415 ms |  0.08420 ms |  0.07464 ms |  0.63 |     175 B |      12 B |        0.52 |
| Dictionary   | 1000000 | 16.54632 ms |  0.11538 ms |  0.09635 ms |  1.00 |     401 B |      23 B |        1.00 |

### Adding a million keys

| Method       | Length   | Mean         | Error       | StdDev       | Median       | Allocated |
|--------------|----------|-------------:|------------:|-------------:|-------------:|----------:|
| **DenseMap** | **10000**    | **0.06526 ms**  | **0.00130 ms**  | **0.00286 ms**  | **0.06490 ms**  | **736 B**     |
| RobinhoodMap | 10000      |  0.06092 ms  |  0.00212 ms  |  0.00602 ms  |  0.06100 ms  |  736 B       |
| Dictionary   | 10000      |  0.23942 ms  |  0.00471 ms  |  0.00691 ms  |  0.23980 ms  |  736 B       |
| **DenseMap** | **100000**   | **0.46808 ms**  | **0.00830 ms**  | **0.01019 ms**  | **0.46855 ms**  | **736 B**     |
| RobinhoodMap | 100000     |  0.61636 ms  |  0.01201 ms  |  0.01430 ms  |  0.61195 ms  |  448 B       |
| Dictionary   | 100000     |  2.41674 ms  |  0.04790 ms  |  0.10104 ms  |  2.38790 ms  |  736 B       |
| **DenseMap** | **400000**   | **2.09803 ms**  | **0.04190 ms**  | **0.08746 ms**  | **2.08300 ms**  | **736 B**     |
| RobinhoodMap | 400000     |  3.50303 ms  |  0.06982 ms  |  0.09079 ms  |  3.48000 ms  |  736 B       |
| Dictionary   | 400000     |  6.47455 ms  |  0.33964 ms  |  0.95238 ms  |  6.73420 ms  |  736 B       |
| **DenseMap** | **800000**   | **6.37665 ms**  | **0.28745 ms**  | **0.79651 ms**  | **5.82920 ms**  | **736 B**     |
| RobinhoodMap | 800000     |  8.28828 ms  |  0.16465 ms  |  0.27052 ms  |  8.30780 ms  |  736 B       |
| Dictionary   | 800000     | 16.27052 ms  |  0.89353 ms  |  2.59229 ms  | 16.42450 ms  |  736 B       |
| **DenseMap** | **900000**   | **6.79059 ms**  | **0.13023 ms**  | **0.13934 ms**  | **6.74320 ms**  | **736 B**     |
| RobinhoodMap | 900000     |  9.87488 ms  |  0.19034 ms  |  0.26054 ms  |  9.80475 ms  |  736 B       |
| Dictionary   | 900000     | 18.87343 ms  |  1.00306 ms  |  2.95755 ms  | 18.42155 ms  |  736 B       |


### Add and resize

| Method       | Length   | Mean         | Error      | StdDev      | Median       | Gen0      | Gen1      | Gen2      | Allocated   |
|--------------|----------|-------------:|-----------:|------------:|-------------:|----------:|----------:|----------:|------------:|
| **DenseMap** | **1000**     | **0.08942 ms**  | **0.00234 ms**  | **0.00675 ms**  | **0.08880 ms**  |         **-** |         **-** |         **-** |    **37.75 KB** |
| RobinhoodMap | 1000       |  0.09152 ms   |   0.00414 ms  |   0.01181 ms  |   0.08890 ms   |         - |         - |         - |    37.51 KB |
| Dictionary   | 1000       |  0.06777 ms   |   0.00519 ms  |   0.01522 ms  |   0.06260 ms   |         - |         - |         - |    72.09 KB |
| **DenseMap** | **10000**    | **0.22937 ms** | **0.00458 ms**  | **0.01061 ms** | **0.22780 ms** |         **-** |         **-** |         **-** |   **290.31 KB** |
| RobinhoodMap | 10000      |  0.34865 ms  |   0.00692 ms  |   0.00769 ms  |  0.34860 ms  |         - |         - |         - |   578.18 KB |
| Dictionary   | 10000      |  0.39528 ms  |   0.01529 ms  |   0.04484 ms  |  0.37380 ms  |         - |         - |         - |   657.93 KB |
| **DenseMap** | **100000**   | **1.29055 ms** | **0.01850 ms** | **0.01545 ms** | **1.29280 ms** |         **-** |         **-** |         **-** |  **2306.88 KB** |
| RobinhoodMap | 100000     |  2.34004 ms |  0.03486 ms  |   0.05428 ms  |  2.32295 ms |         - |         - |         - |  4610.78 KB |
| Dictionary   | 100000     |  3.32307 ms |  0.06625 ms  |   0.11776 ms  |  3.29280 ms |         - |         - |         - |  5896.77 KB |
| **DenseMap** | **400000**   | **5.04851 ms** | **0.09965 ms** | **0.25364 ms** | **5.05355 ms** |         **-** |         **-** |         **-** |  **9219.25 KB** |
| RobinhoodMap | 400000     |  9.31596 ms |  0.18625 ms  |   0.30602 ms  |  9.23980 ms |         - |         - |         - | 18435.23 KB |
| Dictionary   | 400000     | 11.52668 ms |  0.40434 ms  |   1.12042 ms  | 11.20910 ms |         - |         - |         - | 25374.59 KB |
| **DenseMap** | **900000**   | **12.38025 ms** | **0.22981 ms** | **0.62521 ms** | **12.22785 ms** |         **-** |         **-** |         **-** | **18435.44 KB** |
| RobinhoodMap | 900000     | 22.62544 ms |  0.69841 ms  |   2.01507 ms  | 21.66310 ms |         - |         - |         - | 36867.18 KB |
| Dictionary   | 900000     | 31.53625 ms |  0.85001 ms  |   2.43885 ms  | 31.31300 ms | 1000.0000 | 1000.0000 | 1000.0000 | 52626.84 KB |
| **DenseMap** | **1000000**  | **21.15451 ms** | **0.40257 ms** | **0.43075 ms** | **21.11780 ms** |         **-** |         **-** |         **-** | **36867.63 KB** |
| RobinhoodMap | 1000000    | 26.03638 ms |  0.41907 ms  |   0.37149 ms  | 26.13220 ms |         - |         - |         - | 36867.46 KB |
| Dictionary   | 1000000    | 34.51489 ms |  0.68455 ms  |   0.89011 ms  | 34.59730 ms | 1000.0000 | 1000.0000 | 1000.0000 | 52626.84 KB |

### Updating a million keys

| Method       | Length   | Mean          | Error       | StdDev      | Median        | Allocated |
|--------------|----------|--------------:|------------:|------------:|--------------:|----------:|
| **RobinhoodMap** | **1000**     |      **0.00122 ms** |   **0.00001 ms** |   **0.00001 ms** |      **0.00122 ms** |         **-** |
| DenseMap     | 1000      |      0.00172 ms |   0.00003 ms |   0.00003 ms |      0.00171 ms |         - |
| Dictionary   | 1000      |      0.00324 ms |   0.00003 ms |   0.00003 ms |      0.00325 ms |         - |
| **DenseMap** | **10000**    |     **0.02178 ms** |   **0.00013 ms** |   **0.00012 ms** |     **0.02181 ms** |         **-** |
| RobinhoodMap | 10000     |     0.01164 ms |   0.00004 ms |   0.00003 ms |     0.01164 ms |         - |
| Dictionary   | 10000     |     0.08113 ms |   0.00027 ms |   0.00023 ms |     0.08122 ms |         - |
| **DenseMap** | **100000**   |    **0.71209 ms** |  **0.01294 ms** |  **0.01211 ms** |    **0.71421 ms** |       **1 B** |
| RobinhoodMap | 100000    |    0.46221 ms |   0.00153 ms |   0.00143 ms |    0.46254 ms |         - |
| Dictionary   | 100000    |    1.16354 ms |   0.00492 ms |   0.00436 ms |    1.16436 ms |       1 B |
| **DenseMap** | **400000**   |    **3.22005 ms** |  **0.28612 ms** |  **0.84362 ms** |    **2.97777 ms** |       **6 B** |
| RobinhoodMap | 400000    |    2.21473 ms |   0.01258 ms |   0.00982 ms |    2.21459 ms |       3 B |
| Dictionary   | 400000    |    5.35118 ms |   0.03751 ms |   0.03325 ms |    5.34191 ms |       6 B |
| **DenseMap** | **900000**   |    **9.97496 ms** |  **0.07694 ms** |  **0.07197 ms** |    **9.96754 ms** |      **12 B** |
| RobinhoodMap | 900000    |    7.88940 ms |   0.07391 ms |   0.06913 ms |    7.88264 ms |      12 B |
| Dictionary   | 900000    |   15.75044 ms |   0.10900 ms |   0.09102 ms |   15.72190 ms |      23 B |
| **DenseMap** | **1000000**  |   **11.15543 ms** |  **0.11340 ms** |  **0.10607 ms** |   **11.14867 ms** |      **12 B** |
| RobinhoodMap | 1000000   |    9.78328 ms |   0.11723 ms |   0.10966 ms |    9.73837 ms |      12 B |
| Dictionary   | 1000000   |   17.87004 ms |   0.11745 ms |   0.10412 ms |   17.90677 ms |      23 B |

### Removing a million keys

| Method       | Length   | Mean          | Error       | StdDev      | Median        | Allocated  |
|--------------|----------|--------------:|------------:|------------:|--------------:|-----------:|
| **DenseMap** | **100**      | **0.00455 ms**  | **0.00009 ms**  | **0.00023 ms**  | **0.00460 ms**  | **448 B**     |
| RobinhoodMap | 100        |  0.00482 ms   |   0.00024 ms  |   0.00069 ms  |   0.00460 ms   |  736 B       |
| Dictionary   | 100        |  0.00510 ms   |   0.00029 ms  |   0.00086 ms  |   0.00470 ms   |  448 B       |
| **DenseMap** | **10000**    | **0.07086 ms** | **0.00140 ms**  | **0.00219 ms**  | **0.07105 ms** | **736 B**     |
| RobinhoodMap | 10000      |  0.07587 ms  |   0.00150 ms  |   0.00321 ms  |  0.07550 ms  |  736 B       |
| Dictionary   | 10000      |  0.15835 ms  |   0.00860 ms  |  0.02494 ms  |  0.15090 ms  |  736 B       |
| **DenseMap** | **100000**   | **0.52464 ms** | **0.01039 ms** | **0.01197 ms** | **0.52030 ms** | **736 B**     |
| RobinhoodMap | 100000     |  0.80547 ms  |  0.01996 ms  |  0.05824 ms  |  0.81955 ms  |  736 B       |
| Dictionary   | 100000     |  1.20308 ms |  0.02320 ms  |  0.03016 ms  |  1.19430 ms |  736 B       |
| **DenseMap** | **400000**   | **4.15055 ms** | **0.16913 ms** | **0.49869 ms** | **4.35160 ms** | **736 B**     |
| RobinhoodMap | 400000     |  4.75463 ms |  0.12934 ms  |  0.37730 ms  |  4.77535 ms |  736 B       |
| Dictionary   | 400000     |  5.94284 ms |  0.11669 ms  |  0.21918 ms  |  5.91770 ms |  736 B       |
| **DenseMap** | **900000**   | **14.50084 ms** | **0.26427 ms** | **0.28276 ms** | **14.55160 ms** | **18875296 B** |
| RobinhoodMap | 900000     | 12.61194 ms |  0.24400 ms  |  0.27121 ms  | 12.56030 ms |  736 B       |
| Dictionary   | 900000     | 17.13184 ms |  0.29442 ms  |  0.22987 ms  | 17.20345 ms |  736 B       |
| **DenseMap** | **1000000**  | **14.90536 ms** | **0.29377 ms** | **0.53717 ms** | **14.76885 ms** | **736 B**     |
| RobinhoodMap | 1000000    | 14.54652 ms |  0.24280 ms  |  0.27960 ms  | 14.55285 ms |  736 B       |
| Dictionary   | 1000000    | 19.09788 ms |  0.37338 ms  |  0.39952 ms  | 19.06965 ms |  736 B       |

### Get String Benchmark

This benchmark compares the performance of different hash map implementations for various input sizes. The **Mean (ms)** column represents the average time taken for each operation. The fastest implementation for each input size is highlighted in bold.  

| Method            | Length   | Mean (ms)      | Error (ms)     | StdDev (ms)    | Allocated |
|-------------------|----------|----------------|----------------|----------------|-----------|
| DenseMap_Default  | 100      | 0.000788       | 0.000005       | 0.000004       | -         |
| DenseMap_Xxhash3  | 100      | 0.000564       | 0.000002       | 0.000002       | -         |
| DenseMap_GxHash   | 100      | 0.000493       | 0.000005       | 0.000005       | -         |
| **DenseMap_FastHash** 🏆 | **100**      | **0.000442**   | **0.000002**   | **0.000001**   | **-**     |
| RobinhoodMap      | 100      | 0.000676       | 0.000008       | 0.000008       | -         |
| Dictionary        | 100      | 0.000657       | 0.000013       | 0.000014       | -         |
| DenseMap_Default  | 1000     | 0.007954       | 0.000155       | 0.000138       | -         |
| DenseMap_Xxhash3  | 1000     | 0.005986       | 0.000112       | 0.000110       | -         |
| DenseMap_GxHash   | 1000     | 0.005258       | 0.000036       | 0.000034       | -         |
| **DenseMap_FastHash** 🏆 | **1000**     | **0.004514**   | **0.000086**   | **0.000096**   | **-**     |
| RobinhoodMap      | 1000     | 0.007829       | 0.000056       | 0.000049       | -         |
| Dictionary        | 1000     | 0.007406       | 0.000138       | 0.000129       | -         |
| DenseMap_Default  | 10000    | 0.115767       | 0.000376       | 0.000334       | -         |
| DenseMap_Xxhash3  | 10000    | 0.089334       | 0.000304       | 0.000237       | -         |
| DenseMap_GxHash   | 10000    | 0.089310       | 0.000520       | 0.000461       | -         |
| **DenseMap_FastHash** 🏆 | **10000**    | **0.081797**   | **0.000617**   | **0.000547**   | **-**     |
| RobinhoodMap      | 10000    | 0.111006       | 0.001039       | 0.000972       | -         |
| Dictionary        | 10000    | 0.142746       | 0.000562       | 0.000498       | -         |
| DenseMap_Default  | 100000   | 1.567090       | 0.017383       | 0.014516       | 1 B       |
| DenseMap_Xxhash3  | 100000   | 1.154352       | 0.003083       | 0.002733       | 1 B       |
| DenseMap_GxHash   | 100000   | 1.215479       | 0.004052       | 0.003163       | 1 B       |
| **DenseMap_FastHash** 🏆 | **100000**   | **1.032067**   | **0.010772**   | **0.008995**   | **1 B**   |
| RobinhoodMap      | 100000   | 1.864259       | 0.022853       | 0.019083       | 1 B       |
| Dictionary        | 100000   | 1.882831       | 0.035338       | 0.042068       | 1 B       |
| DenseMap_Default  | 200000   | 3.627338       | 0.068771       | 0.086973       | 3 B       |
| DenseMap_Xxhash3  | 200000   | 2.558172       | 0.010472       | 0.008744       | 3 B       |
| DenseMap_GxHash   | 200000   | 2.661878       | 0.049537       | 0.043914       | 3 B       |
| **DenseMap_FastHash** 🏆 | **200000**   | **2.252369**   | **0.035632**   | **0.033330**   | **3 B**   |
| RobinhoodMap      | 200000   | 5.201667       | 0.055903       | 0.049556       | 6 B       |
| Dictionary        | 200000   | 3.900861       | 0.018599       | 0.015531       | 3 B       |
| DenseMap_Default  | 400000   | 8.787456       | 0.102671       | 0.096039       | 12 B      |
| DenseMap_Xxhash3  | 400000   | 6.620002       | 0.101268       | 0.089771       | 12 B      |
| DenseMap_GxHash   | 400000   | 6.252525       | 0.024024       | 0.022472       | 6 B       |
| **DenseMap_FastHash** 🏆 | **400000**   | **5.609309**   | **0.028098**   | **0.023463**   | **6 B**   |
| RobinhoodMap      | 400000   | 17.440620      | 0.115656       | 0.108185       | 23 B      |
| Dictionary        | 400000   | 8.838593       | 0.052306       | 0.046368       | 12 B      |
| DenseMap_Default  | 800000   | 29.897668      | 0.183720       | 0.162863       | 23 B      |
| DenseMap_Xxhash3  | 800000   | 23.740642      | 0.461064       | 0.512471       | 23 B      |
| DenseMap_GxHash   | 800000   | 18.662838      | 0.163517       | 0.136544       | 23 B      |
| **DenseMap_FastHash** 🏆 | **800000**   | **17.516771**  | **0.173695**  | **0.145043**  | **23 B**  |
| RobinhoodMap      | 800000   | 45.399041      | 0.752472       | 0.703863       | 67 B      |
| Dictionary        | 800000   | 26.328634      | 0.122597       | 0.095716       | 23 B      |
| DenseMap_Default  | 1000000  | 43.746846      | 0.255447       | 0.238945       | 67 B      |
| DenseMap_Xxhash3  | 1000000  | 30.988997      | 0.138941       | 0.129965       | 46 B      |
| DenseMap_GxHash   | 1000000  | 23.901505      | 0.091898       | 0.081465       | 23 B      |
| **DenseMap_FastHash** 🏆 | **1000000**  | **22.579233**  | **0.060223**  | **0.050289**  | **23 B**  |
| RobinhoodMap      | 1000000  | 62.235292      | 0.339288       | 0.317370       | 92 B      |
| Dictionary        | 1000000  | 34.091925      | 0.387376       | 0.343398       | 49 B      |

🏆 indicates the fastest implementation for each input size.

---

### Add string benchmark

| Method            | Length   | Mean (ms)      | Error (ms)     | StdDev (ms)    | Median (ms)   | Allocated  |
|-------------------|----------|----------------|----------------|----------------|---------------|------------|
| DenseMap          | 100      | 0.009141       | 0.000963       | 0.002808       | 0.009450       | 2368 B     |
| DenseMap_Xxhash3  | 100      | 0.004088       | 0.000222       | 0.000640       | 0.004100       | 64 B       |
| DenseMap_GxHash   | 100      | 0.008854       | 0.000223       | 0.000641       | 0.008750       | 64 B       |
| DenseMap_FastHash | 100      | 0.007225       | 0.000270       | 0.000780       | 0.007150       | 64 B       |
| RobinhoodMap      | 100      | 0.004639       | 0.000712       | 0.002053       | 0.003900       | 64 B       |
| **Dictionary** 🏆       | **100**      | **0.002654**   | **0.000250**   | **0.000722**   | **0.002600**   | **64 B**   |
| DenseMap          | 1000     | 0.038267       | 0.004567       | 0.013322       | 0.041000       | 35488 B    |
| DenseMap_Xxhash3  | 1000     | 0.024968       | 0.000657       | 0.001741       | 0.024400       | 35200 B    |
| DenseMap_GxHash   | 1000     | 0.025384       | 0.001586       | 0.004314       | 0.023550       | 35200 B    |
| DenseMap_FastHash | 1000     | 0.022692       | 0.001391       | 0.003736       | 0.021400       | 35200 B    |
| RobinhoodMap      | 1000     | 0.019667       | 0.000397       | 0.000424       | 0.019550       | 64 B       |
| **Dictionary** 🏆       | **1000**     | **0.015185**   | **0.000311**   | **0.000726**   | **0.015100**   | **64 B**   |
| DenseMap          | 10000    | 0.107737       | 0.002145       | 0.004429       | 0.108150       | 2656 B     |
| DenseMap_Xxhash3  | 10000    | 0.103542       | 0.001024       | 0.000799       | 0.103300       | 3040 B     |
| DenseMap_GxHash   | 10000    | 0.106641       | 0.002131       | 0.003731       | 0.104900       | 3040 B     |
| **DenseMap_FastHash** 🏆 | **10000**    | **0.095307**   | **0.001898**   | **0.004003**   | **0.094000**   | **3040 B** |
| RobinhoodMap      | 10000    | 0.166072       | 0.003311       | 0.007740       | 0.164200       | 3040 B     |
| Dictionary        | 10000    | 0.164537       | 0.003246       | 0.006176       | 0.164250       | 3040 B     |
| DenseMap          | 100000   | 1.300044       | 0.025983       | 0.046853       | 1.290900       | 3040 B     |
| DenseMap_Xxhash3  | 100000   | 1.299239       | 0.025501       | 0.032250       | 1.293900       | 3040 B     |
| DenseMap_GxHash   | 100000   | 1.366111       | 0.027331       | 0.069069       | 1.347800       | 3040 B     |
| **DenseMap_FastHash** 🏆 | **100000**   | **1.140335**   | **0.021935**   | **0.025261**   | **1.131150**   | **3040 B** |
| RobinhoodMap      | 100000   | 2.208244       | 0.043742       | 0.042960       | 2.211900       | 3040 B     |
| Dictionary        | 100000   | 2.046059       | 0.040840       | 0.069349       | 2.032900       | 3040 B     |
| DenseMap          | 200000   | 3.099234       | 0.061630       | 0.115755       | 3.084000       | 3040 B     |
| DenseMap_Xxhash3  | 200000   | 2.987612       | 0.059547       | 0.079493       | 2.977100       | 3040 B     |
| DenseMap_GxHash   | 200000   | 3.052873       | 0.059889       | 0.104891       | 3.020350       | 3040 B     |
| **DenseMap_FastHash** 🏆 | **200000**   | **2.696498**   | **0.053552**   | **0.097922**   | **2.690650**   | **3040 B** |
| RobinhoodMap      | 200000   | 5.625506       | 0.128638       | 0.369088       | 5.483200       | 3040 B     |
| Dictionary        | 200000   | 4.749739       | 0.093001       | 0.268328       | 4.677800       | 3040 B     |
| DenseMap          | 400000   | 8.393189       | 0.263742       | 0.769349       | 8.089750       | 3040 B     |
| DenseMap_Xxhash3  | 400000   | 7.750054       | 0.151604       | 0.207517       | 7.750400       | 3040 B     |
| DenseMap_GxHash   | 400000   | 7.584241       | 0.144610       | 0.148504       | 7.546400       | 3040 B     |
| **DenseMap_FastHash** 🏆 | **400000**   | **7.059086**   | **0.137344**   | **0.196975**   | **7.066150**   | **3040 B** |
| RobinhoodMap      | 400000   | 15.510240      | 0.308929       | 0.637992       | 15.405350      | 3040 B     |
| Dictionary        | 400000   | 11.463663      | 0.262649       | 0.766160       | 11.352950      | 3040 B     |
| DenseMap          | 800000   | 18.639193      | 0.361017       | 0.517760       | 18.480550      | 3040 B     |
| DenseMap_Xxhash3  | 800000   | 19.099343      | 0.363598       | 0.322320       | 19.057950      | 3040 B     |
| DenseMap_GxHash   | 800000   | 18.050536      | 0.212600       | 0.188464       | 18.033900      | 3040 B     |
| **DenseMap_FastHash** 🏆 | **800000**   | **18.712627**  | **0.512323**   | **1.510596**   | **19.412700**  | **3040 B** |
| RobinhoodMap      | 800000   | 37.518002      | 0.738743       | 1.350833       | 36.844850      | 3040 B     |
| Dictionary        | 800000   | 29.891827      | 0.595388       | 1.526206       | 29.250100      | 3040 B     |

🏆 indicates the fastest implementation for each input size.

