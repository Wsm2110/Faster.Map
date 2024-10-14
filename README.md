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

### Add and resize

| Method       | Length  | Mean          | Error       | StdDev        | Median        | Gen0      | Gen1      | Gen2      | Allocated   |
|--------------|---------|---------------|-------------|---------------|---------------|-----------|-----------|-----------|-------------|
| DenseMap     | 1000    |     0.06888 ms |  0.002836 ms |   0.008361 ms |   0.06740 ms |         - |         - |         - |    37.75 KB |
| RobinhoodMap | 1000    |     0.08617 ms |  0.003494 ms |   0.010303 ms |   0.08575 ms |         - |         - |         - |    37.51 KB |
| Dictionary   | 1000    |     0.06571 ms |  0.004702 ms |   0.013865 ms |   0.06270 ms |         - |         - |         - |    72.09 KB |
| DenseMap     | 10000   |     0.19847 ms |  0.003901 ms |   0.007880 ms |   0.19530 ms |         - |         - |         - |   290.31 KB |
| RobinhoodMap | 10000   |     0.34157 ms |  0.006765 ms |   0.007239 ms |   0.33995 ms |         - |         - |         - |   578.18 KB |
| Dictionary   | 10000   |     0.31703 ms |  0.005693 ms |   0.009668 ms |   0.31630 ms |         - |         - |         - |   657.93 KB |
| DenseMap     | 100000  |     1.23743 ms |  0.019447 ms |   0.015183 ms |   1.23370 ms |         - |         - |         - |  2306.88 KB |
| RobinhoodMap | 100000  |     2.31568 ms |  0.044450 ms |   0.041579 ms |   2.30430 ms |         - |         - |         - |  4610.78 KB |
| Dictionary   | 100000  |     3.03312 ms |  0.057396 ms |   0.092684 ms |   3.02825 ms |         - |         - |         - |  5896.77 KB |
| DenseMap     | 400000  |     5.08197 ms |  0.101222 ms |   0.108306 ms |   5.08500 ms |         - |         - |         - |  9219.25 KB |
| RobinhoodMap | 400000  |     9.40513 ms |  0.186977 ms |   0.337159 ms |   9.31860 ms |         - |         - |         - | 18435.23 KB |
| Dictionary   | 400000  |    11.62261 ms |  0.232425 ms |   0.595794 ms |  11.54280 ms |         - |         - |         - | 25374.92 KB |
| DenseMap     | 900000  |    12.07392 ms |  0.238274 ms |   0.222882 ms |  12.07595 ms |         - |         - |         - | 18435.44 KB |
| RobinhoodMap | 900000  |    21.91546 ms |  0.422548 ms |   0.433926 ms |  21.90840 ms |         - |         - |         - | 36867.18 KB |
| Dictionary   | 900000  |    32.15119 ms |  0.642537 ms |   1.843559 ms |  32.09600 ms | 1000.0000 | 1000.0000 | 1000.0000 | 52626.84 KB |
| DenseMap     | 1000000 |    21.12683 ms |  0.199052 ms |   0.155406 ms |  21.11290 ms |         - |         - |         - | 36867.63 KB |
| RobinhoodMap | 1000000 |    26.75000 ms |  0.529428 ms |   0.742185 ms |  26.66140 ms |         - |         - |         - | 36867.46 KB |
| Dictionary   | 1000000 |    36.88133 ms |  0.731351 ms |   1.510366 ms |  36.99375 ms | 1000.0000 | 1000.0000 | 1000.0000 | 52626.84 KB |


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

| Method       | Length  | Mean          | Error       | StdDev        | Median        | Allocated |
|--------------|---------|---------------|-------------|---------------|---------------|-----------|
| DenseMap     | 1000    |     0.02876 ms |  0.001047 ms |   0.003037 ms |   0.02820 ms |     736 B |
| RobinhoodMap | 1000    |     0.04083 ms |  0.001065 ms |   0.003089 ms |   0.03990 ms |     736 B |
| Dictionary   | 1000    |     0.04365 ms |  0.002059 ms |   0.005973 ms |   0.04250 ms |     448 B |
| DenseMap     | 10000   |     0.05232 ms |  0.000904 ms |   0.000755 ms |   0.05210 ms |     736 B |
| RobinhoodMap | 10000   |     0.07519 ms |  0.001501 ms |   0.002892 ms |   0.07450 ms |     736 B |
| Dictionary   | 10000   |     0.14230 ms |  0.002837 ms |   0.006686 ms |   0.14110 ms |     736 B |
| DenseMap     | 100000  |     0.41937 ms |  0.030693 ms |   0.087072 ms |   0.38860 ms |     736 B |
| RobinhoodMap | 100000  |     0.79855 ms |  0.023237 ms |   0.067415 ms |   0.82460 ms |      64 B |
| Dictionary   | 100000  |     1.23970 ms |  0.024222 ms |   0.038419 ms |   1.23260 ms |     736 B |
| DenseMap     | 400000  |     3.47176 ms |  0.099247 ms |   0.286350 ms |   3.39285 ms |     736 B |
| RobinhoodMap | 400000  |     4.69018 ms |  0.105336 ms |   0.308932 ms |   4.70110 ms |     736 B |
| Dictionary   | 400000  |     6.04550 ms |  0.118591 ms |   0.173829 ms |   6.07010 ms |     736 B |
| DenseMap     | 900000  |    10.99406 ms |  0.212917 ms |   0.493468 ms |  10.93480 ms |     736 B |
| RobinhoodMap | 900000  |    12.97197 ms |  0.358164 ms |   1.050432 ms |  12.51800 ms |     736 B |
| Dictionary   | 900000  |    17.48864 ms |  0.273846 ms |   0.242757 ms |  17.47670 ms |     736 B |
| DenseMap     | 1000000 |    12.14540 ms |  0.241315 ms |   0.435141 ms |  12.16020 ms |     736 B |
| RobinhoodMap | 1000000 |    14.51971 ms |  0.284443 ms |   0.252151 ms |  14.44940 ms |     736 B |
| Dictionary   | 1000000 |    19.87700 ms |  0.305629 ms |   0.285886 ms |  19.80510 ms |     736 B |


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

| Method       | Length  | Mean          | Error        | StdDev       | Allocated |
|--------------|---------|---------------|--------------|--------------|-----------|
| DenseMap     | 1000    |     0.009561 ms |  0.0001123 ms |  0.0000995 ms |         - |
| RobinhoodMap | 1000    |     0.009880 ms |  0.0001149 ms |  0.0001075 ms |         - |
| Dictionary   | 1000    |     0.007242 ms |  0.0000712 ms |  0.0000595 ms |         - |
| DenseMap     | 10000   |     0.108277 ms |  0.0012752 ms |  0.0010649 ms |         - |
| RobinhoodMap | 10000   |     0.127488 ms |  0.0006820 ms |  0.0006046 ms |         - |
| Dictionary   | 10000   |     0.140610 ms |  0.0007416 ms |  0.0006574 ms |         - |
| DenseMap     | 100000  |     1.527491 ms |  0.0102525 ms |  0.0090886 ms |       1 B |
| RobinhoodMap | 100000  |     1.971168 ms |  0.0137025 ms |  0.0128173 ms |       3 B |
| Dictionary   | 100000  |     1.796192 ms |  0.0107030 ms |  0.0100116 ms |       1 B |
| DenseMap     | 400000  |    15.195286 ms |  0.0608646 ms |  0.0539549 ms |      12 B |
| RobinhoodMap | 400000  |    20.500458 ms |  0.1949961 ms |  0.1628305 ms |      23 B |
| Dictionary   | 400000  |    10.485305 ms |  0.2214228 ms |  0.6528703 ms |      12 B |
| DenseMap     | 900000  |    51.631312 ms |  0.6341017 ms |  0.4950649 ms |      74 B |
| RobinhoodMap | 900000  |    68.940244 ms |  1.0120403 ms |  1.4187392 ms |       8 B |
| Dictionary   | 900000  |    37.085791 ms |  0.7275504 ms |  1.0664340 ms |       9 B |
| DenseMap     | 1000000 |    62.505928 ms |  1.2287320 ms |  2.4820994 ms |      14 B |
| RobinhoodMap | 1000000 |    84.237352 ms |  1.6354160 ms |  2.2926251 ms |     123 B |
| Dictionary   | 1000000 |    45.689701 ms |  0.9896159 ms |  2.8867563 ms |      67 B |

### Create StringWrapperBenchmark (cached hashcode)

| Method       | Length  | Mean           | Error         | StdDev        | Allocated |
|--------------|---------|----------------|---------------|---------------|-----------|
| DenseMap     | 1000    |     0.005590 ms |   0.0000622 ms |   0.0000582 ms |         - |
| RobinhoodMap | 1000    |     0.004822 ms |   0.0000862 ms |   0.0000807 ms |         - |
| Dictionary   | 1000    |     0.006721 ms |   0.0001277 ms |   0.0001311 ms |         - |
| DenseMap     | 10000   |     0.072046 ms |   0.0005074 ms |   0.0004237 ms |         - |
| RobinhoodMap | 10000   |     0.071678 ms |   0.0010047 ms |   0.0008390 ms |         - |
| Dictionary   | 10000   |     0.134088 ms |   0.0004288 ms |   0.0004011 ms |         - |
| DenseMap     | 100000  |     1.111280 ms |   0.0174223 ms |   0.0154444 ms |       1 B |
| RobinhoodMap | 100000  |     1.359501 ms |   0.0153216 ms |   0.0143318 ms |       1 B |
| Dictionary   | 100000  |     1.866555 ms |   0.0053967 ms |   0.0045064 ms |       1 B |
| DenseMap     | 400000  |    13.668025 ms |   0.0525273 ms |   0.0465641 ms |      12 B |
| RobinhoodMap | 400000  |    13.020727 ms |   0.0614468 ms |   0.0479736 ms |      12 B |
| Dictionary   | 400000  |    10.846306 ms |   0.0903438 ms |   0.0800874 ms |      12 B |
| DenseMap     | 900000  |    34.296921 ms |   0.1181645 ms |   0.0986727 ms |      49 B |
| RobinhoodMap | 900000  |    33.615793 ms |   0.4644336 ms |   0.4344315 ms |      49 B |
| Dictionary   | 900000  |    41.753783 ms |   0.8050352 ms |   0.7906522 ms |       4 B |
| DenseMap     | 1000000 |    40.145635 ms |   0.7456945 ms |   0.6975231 ms |      57 B |
| RobinhoodMap | 1000000 |    41.060782 ms |   0.5800290 ms |   0.5141806 ms |       5 B |
| Dictionary   | 1000000 |    40.949160 ms |   0.3808110 ms |   0.3179945 ms |       5 B |

