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

``` ini
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3880/23H2/2023Update/SunValley3)
12th Gen Intel Core i5-12500H, 1 CPU, 16 logical and 12 physical cores
.NET SDK 9.0.100-preview.2.24157.14
  [Host]     : .NET 9.0.0 (9.0.24.12805), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.0 (9.0.24.12805), X64 RyuJIT AVX2
```
### Get Benchmark

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


### Add and resize

| Method       | Length   | Mean         | Error      | StdDev       | Median       | Gen0      | Gen1      | Gen2      | Allocated   |
|--------------|----------|-------------:|-----------:|-------------:|-------------:|----------:|----------:|----------:|------------:|
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

### Get string benchmark using XXHash3StringHasher

| Method       | Length   | Mean         | Error      | StdDev     | Allocated |
|--------------|----------|-------------:|-----------:|-----------:|----------:|
| **DenseMap** | **10000**    | **0.06328 ms**  | **0.00119 ms**  | **0.00117 ms**  |         **-** |
| RobinhoodMap | 10000     | 0.11138 ms  |   0.00089 ms  |   0.00083 ms  |         - |
| Dictionary   | 10000     | 0.14030 ms  |   0.00101 ms  |   0.00084 ms  |         - |
| **DenseMap** | **100000**   | **0.89303 ms**  | **0.00092 ms**  | **0.00081 ms**  |       **1 B** |
| RobinhoodMap | 100000    | 1.74953 ms  |   0.00542 ms  |   0.00507 ms  |       1 B |
| Dictionary   | 100000    | 1.82837 ms  |   0.00251 ms  |   0.00222 ms  |       1 B |
| **DenseMap** | **400000**   | **5.69521 ms** | **0.08819 ms** | **0.07364 ms** |       **6 B** |
| RobinhoodMap | 400000    | 17.48812 ms | 0.11409 ms | 0.10672 ms |      23 B |
| Dictionary   | 400000    |  8.90541 ms | 0.10513 ms | 0.09319 ms |      12 B |
| **DenseMap** | **900000**   | **25.03465 ms** | **0.15671 ms** | **0.12235 ms** |      **23 B** |
| RobinhoodMap | 900000    | 52.43294 ms | 0.71473 ms | 0.63359 ms |      74 B |
| Dictionary   | 900000    | 29.69477 ms | 0.19468 ms | 0.15200 ms |      23 B |
| **DenseMap** | **1000000**  | **33.72319 ms** | **0.28449 ms** | **0.25219 ms** |      **49 B** |
| RobinhoodMap | 1000000   | 62.24546 ms | 0.39693 ms | 0.37129 ms |      92 B |
| Dictionary   | 1000000   | 34.00727 ms | 0.10688 ms | 0.08925 ms |      49 B |


### Add string benchmark

| Method       | Length   | Mean         | Error      | StdDev     | Allocated |
|--------------|----------|-------------:|-----------:|-----------:|----------:|
| **DenseMap** | **10000**    | **0.06328 ms**  | **0.00119 ms**  | **0.00117 ms**  |         **-** |
| RobinhoodMap | 10000     | 0.11138 ms  |   0.00089 ms  |   0.00083 ms  |         - |
| Dictionary   | 10000     | 0.14030 ms  |   0.00101 ms  |   0.00084 ms  |         - |
| **DenseMap** | **100000**   | **0.89303 ms**  | **0.00092 ms**  | **0.00081 ms**  |       **1 B** |
| RobinhoodMap | 100000    | 1.74953 ms  |   0.00542 ms  |   0.00507 ms  |       1 B |
| Dictionary   | 100000    | 1.82837 ms  |   0.00251 ms  |   0.00222 ms  |       1 B |
| **DenseMap** | **400000**   | **5.69521 ms** | **0.08819 ms** | **0.07364 ms** |       **6 B** |
| RobinhoodMap | 400000    | 17.48812 ms | 0.11409 ms | 0.10672 ms |      23 B |
| Dictionary   | 400000    |  8.90541 ms | 0.10513 ms | 0.09319 ms |      12 B |
| **DenseMap** | **900000**   | **25.03465 ms** | **0.15671 ms** | **0.12235 ms** |      **23 B** |
| RobinhoodMap | 900000    | 52.43294 ms | 0.71473 ms | 0.63359 ms |      74 B |
| Dictionary   | 900000    | 29.69477 ms | 0.19468 ms | 0.15200 ms |      23 B |
| **DenseMap** | **1000000**  | **33.72319 ms** | **0.28449 ms** | **0.25219 ms** |      **49 B** |
| RobinhoodMap | 1000000   | 62.24546 ms | 0.39693 ms | 0.37129 ms |      92 B |
| Dictionary   | 1000000   | 34.00727 ms | 0.10688 ms | 0.08925 ms |      49 B |


