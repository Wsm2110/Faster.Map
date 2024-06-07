# Faster.Map

Faster.Map is a collection of high-performance hashmaps implemented in C#, each tailored to provide lightning-fast key-value pair operations. Whether you're dealing with large datasets or demanding real-time applications, Faster.Map offers optimized solutions for efficient data retrieval and manipulation.

## Features:

* Optimized Performance: Each hashmap in Faster.Map is finely tuned for performance, ensuring rapid key-value pair operations even under heavy workloads.
* Memory Efficiency: Designed with memory optimization in mind, Faster.Map minimizes overhead to maximize efficiency, making it suitable for resource-constrained environments.
* Variety of Implementations: Choose from different hashmap implementations, including DenseMap with SIMD instructions, RobinHoodMap with linear probing, and QuadMap using triangular numbers, each offering unique advantages for specific use cases.
* Common Interface: All hashmaps in Faster.Map share the same set of functions, including Emplace, Get(), Update(), Remove(), and GetOrUpdate(), providing consistency and ease of use across implementations.


## Available Implementations:

* DenseMap with SIMD Instructions:
        Harnesses SIMD (Single Instruction, Multiple Data) instructions for parallel processing, resulting in accelerated lookup times.
        Ideal for scenarios demanding high throughput and optimal CPU utilization.
* RobinHoodMap with Linear Probing:
        Employs linear probing to resolve hash collisions, reducing the likelihood of clustering and improving access speed.
        Suitable for applications where a balance between performance and simplicity is required.
* QuadMap using Triangular Numbers:
        Utilizes triangular numbers to achieve a balanced distribution of keys, minimizing collisions and enhancing lookup efficiency.
        Offers a unique approach to hashmap implementation, ideal for specialized use cases. 
* CMap  is a high-performance, thread-safe, lockfree concurrent hash map that uses open addressing, quadratic probing, and Fibonacci hashing to manage key-value pairs. The default load factor is set to 0.5, meaning the hash map will resize when it is half full.

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

``` ini
BenchmarkDotNet v0.13.8, Windows 11 (10.0.22621.2428/22H2/2022Update/SunValley2)
12th Gen Intel Core i5-12500H, 1 CPU, 16 logical and 12 physical cores
.NET SDK 8.0.100-rc.1.23463.5
  [Host]     : .NET 8.0.0 (8.0.23.41904), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.0 (8.0.23.41904), X64 RyuJIT AVX2 
```
### Retrieving a million pre-generated keys

| Method       | Length  | Mean per Operation | Error per Operation | StdDev per Operation | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------- |-------- |-------------------:|---------------------:|---------------------:|------:|--------:|----------:|----------:|------------:|
| DenseMap     | 1       |           1.681 ns |           0.0295 ns  |           0.0276 ns  |  0.96 |    0.02 |     312 B |         - |          NA |
| RobinhoodMap | 1       |           1.539 ns |           0.0473 ns  |           0.0443 ns  |  0.88 |    0.03 |     181 B |         - |          NA |
| QuadMap      | 1       |           1.491 ns |           0.0336 ns  |           0.0297 ns  |  0.85 |    0.02 |     220 B |         - |          NA |
| Dictionary   | 1       |           1.744 ns |           0.0223 ns  |           0.0186 ns  |  1.00 |    0.00 |     413 B |         - |          NA |
|              |         |                    |                      |                      |       |         |           |           |             |
| DenseMap     | 10      |           1.703 ns |           0.0220 ns  |           0.0206 ns  |  0.92 |    0.02 |     312 B |         - |          NA |
| RobinhoodMap | 10      |           1.640 ns |           0.0090 ns  |           0.0075 ns  |  0.89 |    0.01 |     182 B |         - |          NA |
| QuadMap      | 10      |           1.344 ns |           0.0139 ns  |           0.0130 ns  |  0.73 |    0.01 |     220 B |         - |          NA |
| Dictionary   | 10      |           1.845 ns |           0.0274 ns  |           0.0256 ns  |  1.00 |    0.00 |     412 B |         - |          NA |
|              |         |                    |                      |                      |       |         |           |           |             |
| DenseMap     | 100     |           1.571 ns |           0.0146 ns  |           0.0136 ns  |  0.72 |    0.01 |     284 B |         - |          NA |
| RobinhoodMap | 100     |           1.429 ns |           0.0286 ns  |           0.0329 ns  |  0.66 |    0.02 |     182 B |         - |          NA |
| QuadMap      | 100     |           1.470 ns |           0.0240 ns  |           0.0225 ns  |  0.68 |    0.01 |     221 B |         - |          NA |
| Dictionary   | 100     |           2.176 ns |           0.0341 ns  |           0.0302 ns  |  1.00 |    0.00 |     420 B |         - |          NA |
|              |         |                    |                      |                      |       |         |           |           |             |
| DenseMap     | 1000    |           1.715 ns |           0.0282 ns  |           0.0264 ns  |  0.55 |    0.01 |     312 B |         - |          NA |
| RobinhoodMap | 1000    |           1.648 ns |           0.0293 ns  |           0.0275 ns  |  0.52 |    0.01 |     182 B |         - |          NA |
| QuadMap      | 1000    |           1.677 ns |           0.0320 ns  |           0.0300 ns  |  0.53 |    0.01 |     220 B |         - |          NA |
| Dictionary   | 1000    |           3.145 ns |           0.0341 ns  |           0.0319 ns  |  1.00 |    0.00 |     412 B |         - |          NA |
|              |         |                    |                      |                      |       |         |           |           |             |
| DenseMap     | 10000   |           1.895 ns |           0.0174 ns  |           0.0162 ns  |  0.23 |    0.00 |     284 B |         - |          NA |
| RobinhoodMap | 10000   |           1.866 ns |           0.0236 ns  |           0.0221 ns  |  0.22 |    0.00 |     182 B |         - |          NA |
| QuadMap      | 10000   |           2.311 ns |           0.0297 ns  |           0.0278 ns  |  0.27 |    0.00 |     221 B |         - |          NA |
| Dictionary   | 10000   |           8.417 ns |           0.0880 ns  |           0.0823 ns  |  1.00 |    0.00 |     412 B |         - |          NA |
|              |         |                    |                      |                      |       |         |           |           |             |
| DenseMap     | 100000  |           2.797 ns |           0.0265 ns  |           0.0235 ns  |  0.23 |    0.00 |     289 B |         - |        0.00 |
| RobinhoodMap | 100000  |           5.519 ns |           0.0554 ns  |           0.0491 ns  |  0.45 |    0.01 |     182 B |         - |        0.00 |
| QuadMap      | 100000  |           6.072 ns |           0.1147 ns  |           0.1073 ns  |  0.50 |    0.01 |     221 B |         - |        0.00 |
| Dictionary   | 100000  |          12.264 ns |           0.2075 ns  |           0.1941 ns  |  1.00 |    0.00 |     412 B |       1 B |        1.00 |
|              |         |                    |                      |                      |       |         |           |           |             |
| DenseMap     | 1000000 |          11.881 ns |           0.2330 ns  |           0.2773 ns  |  0.60 |    0.02 |     289 B |       1 B |        0.04 |
| RobinhoodMap | 1000000 |          11.664 ns |           0.2040 ns  |           0.1809 ns  |  0.59 |    0.01 |     182 B |       1 B |        0.04 |
| QuadMap      | 1000000 |          15.003 ns |           0.0696 ns  |           0.0617 ns  |  0.75 |    0.01 |     221 B |      12 B |        0.52 |
| Dictionary   | 1000000 |          19.925 ns |           0.3739 ns  |           0.3314 ns  |  1.00 |    0.00 |     412 B |      23 B |        1.00 |



### Adding a million keys

| Method       | Length  | Mean          | Error       | StdDev        | Median        |
|--------------|---------|---------------|-------------|---------------|---------------|
| DenseMap     | 1       |      2.386 ns |   0.1922 ns |     0.5515 ns |      2.400 ns |
| RobinhoodMap | 1       |      2.119 ns |   0.1589 ns |     0.4509 ns |      2.200 ns |
| QuadMap      | 1       |      1.920 ns |   0.1392 ns |     0.3972 ns |      1.900 ns |
| Dictionary   | 1       |      2.350 ns |   0.1711 ns |     0.4826 ns |      2.200 ns |
|              |         |               |             |               |               |
| DenseMap     | 10      |      3.398 ns |   0.3427 ns |     0.9667 ns |      3.450 ns |
| RobinhoodMap | 10      |      1.920 ns |   0.2966 ns |     0.8462 ns |      1.500 ns |
| QuadMap      | 10      |      1.880 ns |   0.2944 ns |     0.8398 ns |      1.800 ns |
| Dictionary   | 10      |      1.727 ns |   0.2253 ns |     0.6205 ns |      1.550 ns |
|              |         |               |             |               |               |
| DenseMap     | 100     |     11.574 ns |   1.1192 ns |     3.1385 ns |     11.400 ns |
| RobinhoodMap | 100     |     10.112 ns |   0.8784 ns |     2.4632 ns |     10.000 ns |
| QuadMap      | 100     |      5.642 ns |   0.6559 ns |     1.8607 ns |      5.300 ns |
| Dictionary   | 100     |      8.330 ns |   1.2318 ns |     3.5542 ns |      6.500 ns |
|              |         |               |             |               |               |
| DenseMap     | 1000    |     82.654 ns |  10.0903 ns |    29.2737 ns |     67.600 ns |
| RobinhoodMap | 1000    |     52.873 ns |   7.6720 ns |    21.8887 ns |     44.650 ns |
| QuadMap      | 1000    |     57.005 ns |   5.0141 ns |    14.6264 ns |     58.200 ns |
| Dictionary   | 1000    |     69.909 ns |   8.5316 ns |    24.6156 ns |     62.700 ns |
|              |         |               |             |               |               |
| DenseMap     | 10000   |    267.313 ns |  14.2547 ns |    40.6694 ns |    255.950 ns |
| RobinhoodMap | 10000   |    230.529 ns |  18.8404 ns |    53.4470 ns |    218.800 ns |
| QuadMap      | 10000   |    148.073 ns |   9.0149 ns |    25.8654 ns |    142.700 ns |
| Dictionary   | 10000   |    257.940 ns |  16.0387 ns |    41.4010 ns |    246.000 ns |
|              |         |               |             |               |               |
| DenseMap     | 100000  |  1,916.486 ns | 145.2617 ns |   407.3294 ns |  1,794.700 ns |
| RobinhoodMap | 100000  |  1,863.903 ns | 121.3538 ns |   346.2292 ns |  1,714.050 ns |
| QuadMap      | 100000  |  1,183.141 ns |  61.6521 ns |   178.8639 ns |  1,162.700 ns |
| Dictionary   | 100000  |  2,156.636 ns | 145.1711 ns |   394.9488 ns |  1,975.900 ns |
|              |         |               |             |               |               |
| DenseMap     | 1000000 | 17,395.683 ns | 554.7733 ns | 1,591.7487 ns | 17,677.400 ns |
| RobinhoodMap | 1000000 | 17,408.482 ns | 345.9695 ns |   855.1508 ns | 17,380.450 ns |
| QuadMap      | 1000000 | 15,149.836 ns | 294.0958 ns |   810.0251 ns | 15,105.700 ns |
| Dictionary   | 1000000 | 21,542.058 ns | 414.8775 ns | 1,070.9313 ns | 21,619.000 ns |


### Updating a million keys

| Method       | Length  | Mean              | Error           | StdDev          | Median            | Allocated |
|------------- |-------- |-------------------:|---------------------:|---------------------:|----------------------:|----------:|
| DenseMap     | 1       |          2.810 ns  |          0.0570 ns   |          0.0533 ns   |          2.787 ns     |         - |
| RobinhoodMap | 1       |          1.744 ns  |          0.0187 ns   |          0.0175 ns   |          1.744 ns     |         - |
| QuadMap      | 1       |          1.716 ns  |          0.0140 ns   |          0.0117 ns   |          1.713 ns     |         - |
| Dictionary   | 1       |          3.537 ns  |          0.0274 ns   |          0.0256 ns   |          3.533 ns     |         - |
| DenseMap     | 10      |          1.689 ns  |          0.0101 ns   |          0.0084 ns   |          1.688 ns     |         - |
| RobinhoodMap | 10      |          1.184 ns  |          0.0105 ns   |          0.0098 ns   |          1.185 ns     |         - |
| QuadMap      | 10      |          1.238 ns  |          0.0045 ns   |          0.0042 ns   |          1.236 ns     |         - |
| Dictionary   | 10      |          3.091 ns  |          0.0113 ns   |          0.0105 ns   |          3.090 ns     |         - |
| DenseMap     | 100     |          1.622 ns  |          0.0143 ns   |          0.0133 ns   |          1.624 ns     |         - |
| RobinhoodMap | 100     |          1.357 ns  |          0.0065 ns   |          0.0060 ns   |          1.358 ns     |         - |
| QuadMap      | 100     |          1.369 ns  |          0.0235 ns   |          0.0220 ns   |          1.360 ns     |         - |
| Dictionary   | 100     |          3.099 ns  |          0.0128 ns   |          0.0119 ns   |          3.098 ns     |         - |
| DenseMap     | 1000    |          1.479 ns  |          0.0053 ns   |          0.0071 ns   |          1.477 ns     |         - |
| RobinhoodMap | 1000    |          1.385 ns  |          0.0146 ns   |          0.0136 ns   |          1.382 ns     |         - |
| QuadMap      | 1000    |          1.465 ns  |          0.0245 ns   |          0.0229 ns   |          1.474 ns     |         - |
| Dictionary   | 1000    |          3.161 ns  |          0.0632 ns   |          0.0592 ns   |          3.186 ns     |         - |
| DenseMap     | 10000   |          1.718 ns  |          0.0093 ns   |          0.0087 ns   |          1.720 ns     |         - |
| RobinhoodMap | 10000   |          2.013 ns  |          0.0085 ns   |          0.0079 ns   |          2.011 ns     |         - |
| QuadMap      | 10000   |          1.614 ns  |          0.0137 ns   |          0.0128 ns   |          1.614 ns     |         - |
| Dictionary   | 10000   |          5.437 ns  |          0.0303 ns   |          0.0284 ns   |          5.439 ns     |         - |
| DenseMap     | 100000  |          2.440 ns  |          0.0081 ns   |          0.0072 ns   |          2.441 ns     |         - |
| RobinhoodMap | 100000  |          8.727 ns  |          0.2386 ns   |          0.7034 ns   |          8.973 ns     |       1 B |
| QuadMap      | 100000  |          9.036 ns  |          0.3137 ns   |          0.9250 ns   |          9.301 ns     |       1 B |
| Dictionary   | 100000  |          9.780 ns  |          0.0390 ns   |          0.0346 ns   |          9.786 ns     |       1 B |
| DenseMap     | 1000000 |         10.570 ns  |          0.1628 ns   |          0.1444 ns   |         10.566 ns     |      12 B |
| RobinhoodMap | 1000000 |         13.121 ns  |          0.0498 ns   |          0.0441 ns   |         13.112 ns     |      12 B |
| QuadMap      | 1000000 |         13.535 ns  |          0.0831 ns   |          0.0737 ns   |         13.553 ns     |      12 B |
| Dictionary   | 1000000 |         18.278 ns  |          0.1861 ns   |          0.1554 ns   |         18.242 ns     |      23 B |

### Removing a million keys

| Method       | Length  | Mean per Operation | Error per Operation | StdDev per Operation | Median per Operation | Allocated |
|------------- |-------- |-------------------:|---------------------:|---------------------:|----------------------:|----------:|
| DenseMap     | 1       |          1,482.5 ns |         395.0 ns   |       1,145.9 ns   |        1,800.0 ns     |     736 B |
| RobinhoodMap | 1       |          1,107.3 ns |         301.0 ns   |         868.4 ns   |        1,350.0 ns     |     736 B |
| QuadMap      | 1       |          1,383.7 ns |         369.0 ns   |       1,076.3 ns   |        1,700.0 ns     |     736 B |
| Dictionary   | 1       |          1,806.0 ns |         505.7 ns   |       1,491.0 ns   |        2,100.0 ns     |     736 B |
| DenseMap     | 10      |            85.9 ns |          18.7 ns   |          54.0 ns   |          105.0 ns     |     736 B |
| RobinhoodMap | 10      |           100.6 ns |          18.6 ns   |          53.8 ns   |          120.0 ns     |     736 B |
| QuadMap      | 10      |            89.0 ns |          16.6 ns   |          48.3 ns   |          100.0 ns     |     736 B |
| Dictionary   | 10      |            95.3 ns |          17.0 ns   |          49.3 ns   |          110.0 ns     |     736 B |
| DenseMap     | 100     |            17.9 ns |           2.4 ns   |           6.9 ns   |           21.0 ns     |     736 B |
| RobinhoodMap | 100     |            20.6 ns |           2.8 ns   |           8.1 ns   |           25.0 ns     |     736 B |
| QuadMap      | 100     |            18.4 ns |           2.7 ns   |           7.8 ns   |           22.5 ns     |     736 B |
| Dictionary   | 100     |            24.3 ns |           2.3 ns   |           6.5 ns   |           26.5 ns     |     736 B |
| DenseMap     | 1000    |             9.9 ns |           1.7 ns   |           5.0 ns   |           11.2 ns     |     736 B |
| RobinhoodMap | 1000    |            13.8 ns |           1.9 ns   |           5.5 ns   |           15.3 ns     |     736 B |
| QuadMap      | 1000    |            13.1 ns |           2.0 ns   |           6.0 ns   |           14.4 ns     |     736 B |
| Dictionary   | 1000    |            15.8 ns |           1.9 ns   |           5.7 ns   |           17.0 ns     |     736 B |
| DenseMap     | 10000   |             5.2 ns |           0.1 ns   |           0.2 ns   |            5.3 ns     |     736 B |
| RobinhoodMap | 10000   |             9.0 ns |           0.2 ns   |           0.3 ns   |            9.0 ns     |     736 B |
| QuadMap      | 10000   |             9.0 ns |           0.2 ns   |           0.2 ns   |            9.1 ns     |     736 B |
| Dictionary   | 10000   |            12.9 ns |           0.3 ns   |           0.3 ns   |           12.8 ns     |     736 B |
| DenseMap     | 100000  |             4.7 ns |           0.1 ns   |           0.4 ns   |            4.6 ns     |     736 B |
| RobinhoodMap | 100000  |            11.3 ns |           0.2 ns   |           0.6 ns   |           11.3 ns     |     736 B |
| QuadMap      | 100000  |             9.4 ns |           0.2 ns   |           0.6 ns   |            9.2 ns     |     736 B |
| Dictionary   | 100000  |            11.9 ns |           0.3 ns   |           0.8 ns   |           11.7 ns     |     736 B |
| DenseMap     | 1000000 |            12.7 ns |           0.3 ns   |           0.3 ns   |           12.7 ns     |     736 B |
| RobinhoodMap | 1000000 |            16.6 ns |           0.3 ns   |           0.2 ns   |           16.6 ns     |     736 B |
| QuadMap      | 1000000 |            15.1 ns |           0.3 ns   |           0.3 ns   |           15.2 ns     |     736 B |
| Dictionary   | 1000000 |            20.9 ns |           0.3 ns   |           0.3 ns   |           20.9 ns     |     736 B |


### Add and resize

| Method       | Length  | Mean per Operation | Error per Operation | StdDev per Operation | Median per Operation |
|------------- |-------- |-------------------:|---------------------:|---------------------:|----------------------:|
| DenseMap     | 1       |           660.9 ns |           56.60 ns   |          159.6 ns    |           700.0 ns    |
| RobinhoodMap | 1       |           415.1 ns |           53.24 ns   |          151.0 ns    |           400.0 ns    |
| QuadMap      | 1       |           496.8 ns |           58.76 ns   |          167.7 ns    |           450.0 ns    |
| Dictionary   | 1       |         3,348.5 ns |          577.13 ns   |        1,692.6 ns    |         3,100.0 ns    |
| DenseMap     | 10      |           116.0 ns |            9.99 ns   |           28.8 ns    |           110.0 ns    |
| RobinhoodMap | 10      |           491.9 ns |           60.34 ns   |          174.1 ns    |           490.0 ns    |
| QuadMap      | 10      |           508.1 ns |           48.24 ns   |          134.5 ns    |           520.0 ns    |
| Dictionary   | 10      |           551.6 ns |           54.59 ns   |          158.4 ns    |           540.0 ns    |
| DenseMap     | 100     |           121.8 ns |            7.21 ns   |           20.3 ns    |           125.0 ns    |
| RobinhoodMap | 100     |           216.3 ns |           17.88 ns   |           51.3 ns    |           229.0 ns    |
| QuadMap      | 100     |           182.6 ns |           12.62 ns   |           35.2 ns    |           187.0 ns    |
| Dictionary   | 100     |           166.6 ns |           15.54 ns   |           45.3 ns    |           174.0 ns    |
| DenseMap     | 1000    |            94.5 ns |            3.46 ns   |            9.5 ns    |            95.3 ns    |
| RobinhoodMap | 1000    |           118.8 ns |            4.15 ns   |           11.6 ns    |           121.1 ns    |
| QuadMap      | 1000    |           110.1 ns |            3.74 ns   |           10.2 ns    |           110.9 ns    |
| Dictionary   | 1000    |            96.1 ns |            6.38 ns   |           18.5 ns    |            98.9 ns    |
| DenseMap     | 10000   |            27.5 ns |            0.55 ns   |            1.5 ns    |            27.4 ns    |
| RobinhoodMap | 10000   |            50.3 ns |            1.51 ns   |            4.1 ns    |            50.2 ns    |
| QuadMap      | 10000   |            50.3 ns |            1.59 ns   |            4.5 ns    |            50.5 ns    |
| Dictionary   | 10000   |            45.8 ns |            1.18 ns   |            3.2 ns    |            45.5 ns    |
| DenseMap     | 100000  |            16.3 ns |            0.87 ns   |            2.5 ns    |            15.3 ns    |
| RobinhoodMap | 100000  |            32.1 ns |            1.55 ns   |            4.6 ns    |            31.2 ns    |
| QuadMap      | 100000  |            35.3 ns |            2.24 ns   |            6.5 ns    |            33.3 ns    |
| Dictionary   | 100000  |            33.1 ns |            3.25 ns   |            9.5 ns    |            32.1 ns    |
| DenseMap     | 1000000 |            28.0 ns |            0.55 ns   |            0.7 ns    |            28.0 ns    |
| RobinhoodMap | 1000000 |            37.6 ns |            0.93 ns   |            2.7 ns    |            36.7 ns    |
| QuadMap      | 1000000 |            37.4 ns |            1.11 ns   |            3.3 ns    |            36.0 ns    |
| Dictionary   | 1000000 |            39.6 ns |            0.77 ns   |            1.7 ns    |            39.7 ns    |

### Add string benchmark

| Method       | Length  | Mean per Operation | Error per Operation | StdDev per Operation | Median per Operation |
|------------- |-------- |-------------------:|---------------------:|---------------------:|----------------------:|
| DenseMap     | 1       |           9.534 ns |           0.0911 ns  |           0.0852 ns  |           9.520 ns    |
| RobinhoodMap | 1       |           8.094 ns |           0.0495 ns  |           0.0463 ns  |           8.087 ns    |
| QuadMap      | 1       |           8.284 ns |           0.0382 ns  |           0.0338 ns  |           8.281 ns    |
| Dictionary   | 1       |           6.540 ns |           0.0427 ns  |           0.0356 ns  |           6.537 ns    |
| DenseMap     | 10      |           8.620 ns |           0.0276 ns  |           0.0258 ns  |           8.619 ns    |
| RobinhoodMap | 10      |           7.590 ns |           0.0379 ns  |           0.0336 ns  |           7.589 ns    |
| QuadMap      | 10      |           7.872 ns |           0.0390 ns  |           0.0365 ns  |           7.861 ns    |
| Dictionary   | 10      |           6.611 ns |           0.0343 ns  |           0.0304 ns  |           6.611 ns    |
| DenseMap     | 100     |           8.678 ns |           0.0557 ns  |           0.0521 ns  |           8.668 ns    |
| RobinhoodMap | 100     |           8.489 ns |           0.0271 ns  |           0.0240 ns  |           8.493 ns    |
| QuadMap      | 100     |           7.843 ns |           0.0201 ns  |           0.0188 ns  |           7.842 ns    |
| Dictionary   | 100     |           7.285 ns |           0.0620 ns  |           0.0555 ns  |           7.279 ns    |
| DenseMap     | 1000    |           9.852 ns |           0.1071 ns  |           0.0949 ns  |           9.851 ns    |
| RobinhoodMap | 1000    |          10.573 ns |           0.1971 ns  |           0.1844 ns  |          10.550 ns    |
| QuadMap      | 1000    |           9.469 ns |           0.1820 ns  |           0.1870 ns  |           9.480 ns    |
| Dictionary   | 1000    |           7.693 ns |           0.0624 ns  |           0.0553 ns  |           7.684 ns    |
| DenseMap     | 10000   |          10.958 ns |           0.1777 ns  |           0.1575 ns  |          10.979 ns    |
| RobinhoodMap | 10000   |          14.014 ns |           0.2693 ns  |           0.2519 ns  |          13.984 ns    |
| QuadMap      | 10000   |          13.471 ns |           0.1864 ns  |           0.1830 ns  |          13.496 ns    |
| Dictionary   | 10000   |          11.753 ns |           0.1942 ns  |           0.1817 ns  |          11.762 ns    |
| DenseMap     | 100000  |          16.322 ns |           0.2633 ns  |           0.2463 ns  |          16.374 ns    |
| RobinhoodMap | 100000  |          25.513 ns |           0.6388 ns  |           1.8432 ns  |          25.370 ns    |
| QuadMap      | 100000  |          20.426 ns |           0.4077 ns  |           1.0376 ns  |          20.301 ns    |
| Dictionary   | 100000  |          16.702 ns |           0.2272 ns  |           0.2125 ns  |          16.682 ns    |
| DenseMap     | 1000000 |          55.786 ns |           1.1114 ns  |           1.2799 ns  |          55.947 ns    |
| RobinhoodMap | 1000000 |          69.357 ns |           1.5083 ns  |           1.6138 ns  |          69.520 ns    |
| QuadMap      | 1000000 |          56.245 ns |           1.1081 ns  |           1.4013 ns  |          56.028 ns    |
| Dictionary   | 1000000 |          36.926 ns |           0.7072 ns  |           2.0516 ns  |          36.274 ns    |

### Create StringWrapperBenchmark (cached hashcode)
| Method       | Length  | Mean per Operation | Error per Operation | StdDev per Operation | Allocated |
|------------- |-------- |-------------------:|---------------------:|---------------------:|----------:|
| DenseMap     | 1       |           8.129 ns |           0.0911 ns  |           0.0852 ns  |         - |
| RobinhoodMap | 1       |           7.145 ns |           0.0481 ns  |           0.0450 ns  |         - |
| QuadMap      | 1       |           7.226 ns |           0.0234 ns  |           0.0219 ns  |         - |
| Dictionary   | 1       |           8.049 ns |           0.0695 ns  |           0.0616 ns  |         - |
| DenseMap     | 10      |           7.595 ns |           0.0496 ns  |           0.0414 ns  |         - |
| RobinhoodMap | 10      |           6.906 ns |           0.0416 ns  |           0.0389 ns  |         - |
| QuadMap      | 10      |           6.993 ns |           0.0397 ns  |           0.0372 ns  |         - |
| Dictionary   | 10      |           7.856 ns |           0.0098 ns  |           0.0082 ns  |         - |
| DenseMap     | 100     |           7.946 ns |           0.0279 ns  |           0.0233 ns  |         - |
| RobinhoodMap | 100     |           6.906 ns |           0.0596 ns  |           0.0529 ns  |         - |
| QuadMap      | 100     |           7.038 ns |           0.0403 ns  |           0.0377 ns  |         - |
| Dictionary   | 100     |           7.921 ns |           0.0097 ns  |           0.0086 ns  |         - |
| DenseMap     | 1000    |           8.654 ns |           0.0645 ns  |           0.0603 ns  |         - |
| RobinhoodMap | 1000    |           7.804 ns |           0.1524 ns  |           0.1565 ns  |         - |
| QuadMap      | 1000    |           8.008 ns |           0.1012 ns  |           0.0946 ns  |         - |
| Dictionary   | 1000    |           8.938 ns |           0.1754 ns  |           0.2459 ns  |         - |
| DenseMap     | 10000   |          10.104 ns |           0.2007 ns  |           0.3065 ns  |         - |
| RobinhoodMap | 10000   |          10.253 ns |           0.1926 ns  |           0.1802 ns  |         - |
| QuadMap      | 10000   |          11.468 ns |           0.1580 ns  |           0.1478 ns  |         - |
| Dictionary   | 10000   |          13.704 ns |           0.1490 ns  |           0.1393 ns  |         - |
| DenseMap     | 100000  |          14.724 ns |           0.1515 ns  |           0.1417 ns  |       2 B |
| RobinhoodMap | 100000  |          20.351 ns |           0.2714 ns  |           0.2539 ns  |       4 B |
| QuadMap      | 100000  |          18.965 ns |           0.2364 ns  |           0.2211 ns  |       4 B |
| Dictionary   | 100000  |          19.405 ns |           0.2534 ns  |           0.2370 ns  |       4 B |
| DenseMap     | 1000000 |          50.876 ns |           0.2725 ns  |           0.2416 ns  |     102 B |
| RobinhoodMap | 1000000 |          50.850 ns |           0.4853 ns  |           0.4539 ns  |     102 B |
| QuadMap      | 1000000 |          51.706 ns |           0.4721 ns  |           0.4416 ns  |     102 B |
| Dictionary   | 1000000 |          44.046 ns |           0.8703 ns  |           1.0688 ns  |      85 B |
