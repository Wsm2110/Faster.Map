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
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3880/23H2/2023Update/SunValley3)
12th Gen Intel Core i5-12500H, 1 CPU, 16 logical and 12 physical cores
.NET SDK 9.0.100-preview.2.24157.14
  [Host]     : .NET 9.0.0 (9.0.24.12805), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.0 (9.0.24.12805), X64 RyuJIT AVX2
```
### Retrieving a million pre-generated keys

| Method       | Length  | Mean          | Error       | StdDev      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------|---------|---------------|-------------|-------------|-------|---------|-----------|-----------|-------------|
| DenseMap     | 1000    |     0.001561 ms |  0.0000298 ms |  0.0000306 ms |  0.55 |   0.01 |    312 B |         - |         NA  |
| RobinhoodMap | 1000    |     0.001453 ms |  0.0000072 ms |  0.0000063 ms |  0.52 |   0.00 |    182 B |         - |         NA  |
| Dictionary   | 1000    |     0.002817 ms |  0.0000157 ms |  0.0000123 ms |  1.00 |   0.01 |    412 B |         - |         NA  |
| DenseMap     | 10000   |     0.018443 ms |  0.0001868 ms |  0.0001560 ms |  0.25 |   0.01 |    312 B |         - |         NA  |
| RobinhoodMap | 10000   |     0.048846 ms |  0.0000682 ms |  0.0000570 ms |  0.65 |   0.02 |    182 B |         - |         NA  |
| Dictionary   | 10000   |     0.074840 ms |  0.0013760 ms |  0.0034011 ms |  1.00 |   0.06 |    412 B |         - |         NA  |
| DenseMap     | 100000  |     0.302329 ms |  0.0018030 ms |  0.0016865 ms |  0.28 |   0.00 |    289 B |         - |       0.00  |
| RobinhoodMap | 100000  |     0.525929 ms |  0.0020816 ms |  0.0018453 ms |  0.48 |   0.00 |    182 B |       1 B |       1.00  |
| Dictionary   | 100000  |     1.091758 ms |  0.0026861 ms |  0.0022430 ms |  1.00 |   0.00 |    393 B |       1 B |       1.00  |
| DenseMap     | 400000  |     1.966361 ms |  0.0391485 ms |  0.0366195 ms |  0.40 |   0.01 |    289 B |       3 B |       0.50  |
| RobinhoodMap | 400000  |     2.377139 ms |  0.0238691 ms |  0.0223271 ms |  0.48 |   0.01 |    182 B |       3 B |       0.50  |
| Dictionary   | 400000  |     4.901921 ms |  0.0584053 ms |  0.0517748 ms |  1.00 |   0.01 |    412 B |       6 B |       1.00  |
| DenseMap     | 900000  |     9.743361 ms |  0.0631023 ms |  0.0559385 ms |  0.60 |   0.01 |    289 B |      12 B |       0.52  |
| RobinhoodMap | 900000  |     8.845731 ms |  0.0784927 ms |  0.0734222 ms |  0.55 |   0.01 |    182 B |      12 B |       0.52  |
| Dictionary   | 900000  |    16.156572 ms |  0.1954894 ms |  0.1632425 ms |  1.00 |   0.01 |    412 B |      23 B |       1.00  |
| DenseMap     | 1000000 |    11.216461 ms |  0.1142519 ms |  0.0954055 ms |  0.65 |   0.01 |    289 B |      12 B |       0.52  |
| RobinhoodMap | 1000000 |    11.368053 ms |  0.0775996 ms |  0.0687900 ms |  0.66 |   0.01 |    182 B |      12 B |       0.52  |
| Dictionary   | 1000000 |    17.346513 ms |  0.1926637 ms |  0.1504191 ms |  1.00 |   0.01 |    412 B |      23 B |       1.00  |

### Adding a million keys

| Method       | Length  | Mean         | Error      | StdDev       | Median       | Allocated |
|--------------|---------|--------------|------------|--------------|--------------|-----------|
| DenseMap     | 1000    |     0.04696 ms |   0.00105 ms |     0.00301 ms |     0.04580 ms |     784 B |
| RobinhoodMap | 1000    |     0.05695 ms |   0.00139 ms |     0.00407 ms |     0.05695 ms |     448 B |
| Dictionary   | 1000    |     0.05925 ms |   0.00302 ms |     0.00886 ms |     0.05710 ms |     448 B |
| DenseMap     | 10000   |     0.10247 ms |   0.00204 ms |     0.00412 ms |     0.10170 ms |     784 B |
| RobinhoodMap | 10000   |     0.11880 ms |   0.00235 ms |     0.00412 ms |     0.11740 ms |     784 B |
| Dictionary   | 10000   |     0.28064 ms |   0.00560 ms |     0.00872 ms |     0.28010 ms |     784 B |
| DenseMap     | 100000  |     0.74044 ms |   0.01424 ms |     0.01462 ms |     0.73490 ms |     784 B |
| RobinhoodMap | 100000  |     1.09080 ms |   0.02139 ms |     0.03393 ms |     1.08320 ms |     448 B |
| Dictionary   | 100000  |     2.99360 ms |   0.05919 ms |     0.07269 ms |     2.98355 ms |     784 B |
| DenseMap     | 400000  |     5.01900 ms |   0.34001 ms |     0.99719 ms |     5.46250 ms |     784 B |
| RobinhoodMap | 400000  |     4.83217 ms |   0.10687 ms |     0.30317 ms |     4.76510 ms |     784 B |
| Dictionary   | 400000  |     7.67889 ms |   0.15666 ms |     0.42620 ms |     7.59280 ms |     784 B |
| DenseMap     | 900000  |    10.63200 ms |   0.20614 ms |     0.18274 ms |    10.60900 ms |     784 B |
| RobinhoodMap | 900000  |    14.09060 ms |   0.28137 ms |     0.78899 ms |    14.05040 ms |     784 B |
| Dictionary   | 900000  |    24.58053 ms |   0.89271 ms |     2.61815 ms |    24.80390 ms |     784 B |
| DenseMap     | 1000000 |    10.93189 ms |   0.21667 ms |     0.55930 ms |    10.74225 ms |     784 B |
| RobinhoodMap | 1000000 |    17.13302 ms |   0.37145 ms |     1.05975 ms |    16.94610 ms |     784 B |
| Dictionary   | 1000000 |    28.22181 ms |   0.84126 ms |     2.45399 ms |    27.72925 ms |     784 B |

### Updating a million keys

| Method       | Length  | Mean          | Error       | StdDev      | Median        | Allocated |
|--------------|---------|---------------|-------------|-------------|---------------|-----------|
| DenseMap     | 1000    |     0.001497 ms |  0.0000095 ms |  0.0000080 ms |   0.001497 ms |         - |
| RobinhoodMap | 1000    |     0.001886 ms |  0.0000369 ms |  0.0000563 ms |   0.001887 ms |         - |
| Dictionary   | 1000    |     0.003374 ms |  0.0000594 ms |  0.0000660 ms |   0.003388 ms |         - |
| DenseMap     | 10000   |     0.018904 ms |  0.0000967 ms |  0.0000807 ms |   0.018898 ms |         - |
| RobinhoodMap | 10000   |     0.028991 ms |  0.0001625 ms |  0.0001520 ms |   0.029036 ms |         - |
| Dictionary   | 10000   |     0.086054 ms |  0.0001592 ms |  0.0001329 ms |   0.086069 ms |         - |
| DenseMap     | 100000  |     0.722426 ms |  0.0139707 ms |  0.0171572 ms |   0.725357 ms |       1 B |
| RobinhoodMap | 100000  |     0.572789 ms |  0.0107215 ms |  0.0100289 ms |   0.568764 ms |       1 B |
| Dictionary   | 100000  |     1.195203 ms |  0.0032038 ms |  0.0029968 ms |   1.196440 ms |       1 B |
| DenseMap     | 400000  |     4.508174 ms |  0.0646880 ms |  0.0605092 ms |   4.485129 ms |       6 B |
| RobinhoodMap | 400000  |     2.844626 ms |  0.0536748 ms |  0.0475813 ms |   2.823570 ms |       3 B |
| Dictionary   | 400000  |     5.396909 ms |  0.0286768 ms |  0.0268243 ms |   5.405657 ms |       6 B |
| DenseMap     | 900000  |     9.321427 ms |  0.1539810 ms |  0.1365001 ms |   9.280112 ms |      12 B |
| RobinhoodMap | 900000  |     8.692224 ms |  0.0805071 ms |  0.0713675 ms |   8.675028 ms |      12 B |
| Dictionary   | 900000  |    16.036116 ms |  0.1468814 ms |  0.1226526 ms |  16.054666 ms |      23 B |
| DenseMap     | 1000000 |    10.349873 ms |  0.1659302 ms |  0.1385592 ms |  10.316205 ms |      12 B |
| RobinhoodMap | 1000000 |    11.018986 ms |  0.2149889 ms |  0.5273715 ms |  10.852050 ms |      12 B |
| Dictionary   | 1000000 |    18.725914 ms |  0.2099566 ms |  0.1753233 ms |  18.752681 ms |      23 B |

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
