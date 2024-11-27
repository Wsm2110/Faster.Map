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
### Get Uint Benchmark

| Method            | Length   | Mean (ms)      | Error (ms)     | StdDev (ms)    | Median (ms)    | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------------|----------|----------------|----------------|----------------|----------------|-------|---------|-----------|-----------|-------------|
| DenseMap          | 100      | 0.000194       | 0.000001       | 0.000001       | 0.000194       | 0.96   | 0.01    | 431 B     | -         | NA          |
| DenseMap_XXhash3  | 100      | 0.000261       | 0.000001       | 0.000001       | 0.000261       | 1.29   | 0.01    | 544 B     | -         | NA          |
| DenseMap_FastHash | 100      | 0.000181       | 0.000002       | 0.000002       | 0.000180       | 0.89   | 0.01    | 447 B     | -         | NA          |
| DenseMap_GxHash   | 100      | 0.000301       | 0.000001       | 0.000001       | 0.000301       | 1.48   | 0.01    | 587 B     | -         | NA          |
| **RobinhoodMap** 🏆 | **100**      | **0.000130**   | **0.000001**   | **0.000001**   | **0.000130**   | **0.64** | **0.00**  | **175 B**  | **-**       | **NA**       |
| Dictionary        | 100      | 0.000203       | 0.000001       | 0.000001       | 0.000203       | 1.00   | 0.01    | 390 B     | -         | NA          |
| DenseMap          | 1000     | 0.001753       | 0.000008       | 0.000008       | 0.001755       | 0.64   | 0.01    | 431 B     | -         | NA          |
| DenseMap_XXhash3  | 1000     | 0.002561       | 0.000004       | 0.000003       | 0.002560       | 0.94   | 0.01    | 549 B     | -         | NA          |
| DenseMap_FastHash | 1000     | 0.001736       | 0.000005       | 0.000005       | 0.001735       | 0.64   | 0.01    | 452 B     | -         | NA          |
| DenseMap_GxHash   | 1000     | 0.010619       | 0.000005       | 0.000004       | 0.010617       | 3.89   | 0.03    | 595 B     | -         | NA          |
| **RobinhoodMap** 🏆 | **1000**     | **0.001357**   | **0.000004**   | **0.000003**   | **0.001357**   | **0.50** | **0.00**  | **175 B**  | **-**       | **NA**       |
| Dictionary        | 1000     | 0.002732       | 0.000024       | 0.000021       | 0.002727       | 1.00   | 0.01    | 401 B     | -         | NA          |
| DenseMap          | 10000    | 0.022729       | 0.000449       | 0.000749       | 0.022621       | 0.30   | 0.01    | 426 B     | -         | NA          |
| DenseMap_XXhash3  | 10000    | 0.031829       | 0.000626       | 0.000814       | 0.031466       | 0.42   | 0.01    | 544 B     | -         | NA          |
| DenseMap_FastHash | 10000    | 0.023133       | 0.000453       | 0.000445       | 0.022998       | 0.31   | 0.01    | 447 B     | -         | NA          |
| DenseMap_GxHash   | 10000    | 0.037798       | 0.000657       | 0.000615       | 0.037732       | 0.50   | 0.01    | 587 B     | -         | NA          |
| **RobinhoodMap** 🏆 | **10000**    | **0.013777**   | **0.000196**   | **0.000184**   | **0.013766**   | **0.18** | **0.00**  | **175 B**  | **-**       | **NA**       |
| Dictionary        | 10000    | 0.075074       | 0.001470       | 0.001509       | 0.075345       | 1.00   | 0.03    | 401 B     | -         | NA          |
| DenseMap          | 100000   | 0.589076       | 0.009191       | 0.008598       | 0.585025       | 0.49   | 0.01    | 431 B     | 1 B       | 1.00        |
| DenseMap_XXhash3  | 100000   | 0.755758       | 0.011338       | 0.010605       | 0.750415       | 0.62   | 0.01    | 549 B     | 1 B       | 1.00        |
| DenseMap_FastHash | 100000   | 0.638576       | 0.012430       | 0.017015       | 0.637227       | 0.53   | 0.01    | 452 B     | 1 B       | 1.00        |
| DenseMap_GxHash   | 100000   | 1.909495       | 0.013846       | 0.012274       | 1.902422       | 1.58   | 0.02    | 595 B     | 1 B       | 1.00        |
| **RobinhoodMap** 🏆 | **100000**  | **0.525858**   | **0.009747**   | **0.009117**   | **0.523486**   | **0.43** | **0.01**  | **175 B**  | **1 B**     | **1.00**     |
| Dictionary        | 100000   | 1.211352       | 0.010631       | 0.009944       | 1.210796       | 1.00   | 0.01    | 401 B     | 1 B       | 1.00        |
| DenseMap          | 200000   | 1.365416       | 0.025382       | 0.023742       | 1.363805       | 0.56   | 0.01    | 431 B     | 1 B       | 0.33        |
| DenseMap_XXhash3  | 200000   | 1.681107       | 0.016207       | 0.014367       | 1.679022       | 0.69   | 0.01    | 549 B     | 1 B       | 0.33        |
| DenseMap_FastHash | 200000   | 1.413841       | 0.008123       | 0.007201       | 1.415798       | 0.58   | 0.00    | 452 B     | 1 B       | 0.33        |
| DenseMap_GxHash   | 200000   | 4.816318       | 0.093553       | 0.111368       | 4.805059       | 1.97   | 0.05    | 595 B     | 3 B       | 1.00        |
| **RobinhoodMap** 🏆 | **200000**  | **1.094053**   | **0.008370**   | **0.007829**   | **1.093887**   | **0.45** | **0.00**  | **175 B**  | **1 B**     | **0.33**     |
| Dictionary        | 200000   | 2.445983       | 0.009854       | 0.008736       | 2.447805       | 1.00   | 0.00    | 401 B     | 3 B       | 1.00        |
| DenseMap          | 400000   | 2.837709       | 0.036080       | 0.031984       | 2.846067       | 0.55   | 0.01    | 431 B     | 3 B       | 0.50        |
| DenseMap_XXhash3  | 400000   | 3.528719       | 0.040348       | 0.037741       | 3.518422       | 0.69   | 0.01    | 549 B     | 3 B       | 0.50        |
| DenseMap_FastHash | 400000   | 2.873634       | 0.019479       | 0.018221       | 2.868161       | 0.56   | 0.01    | 452 B     | 3 B       | 0.50        |
| DenseMap_GxHash   | 400000   | 12.225273      | 0.145211       | 0.128726       | 12.230369      | 2.37   | 0.05    | 595 B     | 6 B       | 1.00        |
| **RobinhoodMap** 🏆 | **400000**  | **2.409849**   | **0.036286**   | **0.030301**   | **2.414273**   | **0.47** | **0.01**  | **175 B**  | **3 B**     | **0.50**     |
| Dictionary        | 400000   | 5.152673       | 0.101303       | 0.099493       | 5.122177       | 1.00   | 0.03    | 401 B     | 6 B       | 1.00        |
| **DenseMap** 🏆       | **800000**   | **7.591542**       | **0.150057**      | **0.285499**       | **7.657127**       | **0.48**   | **0.03**    | **431 B**     | **6 B**       | **0.50**        |
| DenseMap_XXhash3  | 800000   | 10.558574      | 0.291804       | 0.832532       | 10.373517      | 0.67   | 0.06    | 549 B     | 12 B      | 1.00        |
| DenseMap_FastHash | 800000   | 7.851049       | 0.155718       | 0.228250       | 7.903258       | 0.50   | 0.02    | 452 B     | 7 B       | 0.58        |
| DenseMap_GxHash   | 800000   | 9.982939       | 0.199132       | 0.495907       | 9.816153       | 0.64   | 0.04    | 595 B     | 1 B       | 0.08        |
| RobinhoodMap      | 800000   | 8.672066   | 0.163641   | 0.175094   | 8.657665   | 0.55 | 0.02  | 175 B  | 12 B    | 1.00    |
| Dictionary        | 800000   | 15.704979      | 0.309662       | 0.632557       | 15.698669      | 1.00   | 0.06    | 401 B     | 12 B      | 1.00        |
| DenseMap          | 1000000  | 13.657758      | 0.269128       | 0.251743       | 13.658680      | 0.69   | 0.02    | 431 B     | 12 B      | 0.52        |
| DenseMap_XXhash3  | 1000000  | 18.736959      | 0.374438       | 0.384520       | 18.775884      | 0.94   | 0.02    | 549 B     | 23 B      | 1.00        |
| DenseMap_FastHash | 1000000  | 13.895038      | 0.269635       | 0.369079       | 13.890234      | 0.70   | 0.02    | 452 B     | 12 B      | 0.52        |
| DenseMap_GxHash   | 1000000  | 93.297906      | 1.801600       | 2.278448       | 92.519569      | 4.69   | 0.13    | 595 B     | 23 B      | 1.00        |
| **RobinhoodMap** 🏆 | **1000000** | **12.220638**   | **0.096660**   | **0.085687**   | **12.225982**   | **0.61** | **0.01**  | **175 B**  | **12 B**    | **0.52**     |
| Dictionary        | 1000000  | 19.900920      | 0.336682       | 0.314932       | 20.024941      | 1.00   | 0.02    | 401 B     | 23 B      | 1.00        |


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
| DenseMap          | 800      | 0.04059        | 0.007124       | 0.020892       | 0.05355        | 64 B       |
| DenseMap_Xxhash3  | 800      | 0.01706        | 0.000616       | 0.001758       | 0.01660        | 64 B       |
| DenseMap_GxHash   | 800      | 0.05274        | 0.000956       | 0.002363       | 0.05270        | 3040 B     |
| DenseMap_FastHash | 800      | 0.04192        | 0.000721       | 0.001504       | 0.04210        | 64 B       |
| RobinhoodMap      | 800      | 0.03995        | 0.000797       | 0.001832       | 0.04000        | 64 B       |
| **DenseMap_Xxhash3** 🏆  | **800**      | **0.01706**    | **0.000616**   | **0.001758**   | **0.01660**    | **64 B**   |
| DenseMap          | 10000    | 0.12274        | 0.002461       | 0.006861       | 0.12135        | 3040 B     |
| DenseMap_Xxhash3  | 10000    | 0.11998        | 0.003871       | 0.010918       | 0.12040        | 3040 B     |
| DenseMap_GxHash   | 10000    | 0.11572        | 0.003281       | 0.009572       | 0.11295        | 3040 B     |
| DenseMap_FastHash | 10000    | 0.10445        | 0.003452       | 0.010125       | 0.10110        | 3040 B     |
| RobinhoodMap      | 10000    | 0.17132        | 0.003413       | 0.007910       | 0.17180        | 3040 B     |
| Dictionary        | 10000    | 0.15904        | 0.002961       | 0.002625       | 0.15925        | 3040 B     |
| **DenseMap_FastHash** 🏆 | **10000**   | **0.10445**   | **0.003452**   | **0.010125**   | **0.10110**    | **3040 B** |
| DenseMap          | 100000   | 1.35808        | 0.027144       | 0.069090       | 1.35985        | 3040 B     |
| DenseMap_Xxhash3  | 100000   | 1.28464        | 0.025021       | 0.041804       | 1.26875        | 3040 B     |
| DenseMap_GxHash   | 100000   | 1.36449        | 0.033564       | 0.095760       | 1.33085        | 3040 B     |
| **DenseMap_FastHash** 🏆 | **100000**  | **1.15728**   | **0.022744**   | **0.061489**   | **1.14390**    | **3040 B** |
| RobinhoodMap      | 100000   | 2.21979        | 0.043974       | 0.101916       | 2.19720        | 3040 B     |
| Dictionary        | 100000   | 1.94669        | 0.027539       | 0.021501       | 1.94855        | 3040 B     |
| DenseMap          | 200000   | 2.98278        | 0.059115       | 0.137009       | 2.94060        | 3040 B     |
| DenseMap_Xxhash3  | 200000   | 3.18936        | 0.087598       | 0.254139       | 3.06625        | 3040 B     |
| DenseMap_GxHash   | 200000   | 3.15534        | 0.094124       | 0.271570       | 3.03850        | 3040 B     |
| **DenseMap_FastHash** 🏆 | **200000**  | **2.95021**   | **0.157610**   | **0.464717**   | **2.71500**    | **3040 B** |
| RobinhoodMap      | 200000   | 5.64369        | 0.138519       | 0.395203       | 5.53480        | 3040 B     |
| Dictionary        | 200000   | 4.68717        | 0.112804       | 0.321835       | 4.62610        | 3040 B     |
| DenseMap          | 400000   | 7.68123        | 0.151588       | 0.253270       | 7.61775        | 3040 B     |
| DenseMap_Xxhash3  | 400000   | 7.78637        | 0.138350       | 0.326108       | 7.73170        | 3040 B     |
| DenseMap_GxHash   | 400000   | 7.51799        | 0.149517       | 0.219160       | 7.49140        | 3040 B     |
| **DenseMap_FastHash** 🏆 | **400000**  | **7.15870**   | **0.183103**   | **0.510418**   | **7.07320**    | **3040 B** |
| RobinhoodMap      | 400000   | 15.24456       | 0.304043       | 0.647941       | 15.07110       | 3040 B     |
| Dictionary        | 400000   | 11.70488       | 0.542859       | 1.504260       | 11.32320       | 3040 B     |
| DenseMap          | 800000   | 19.18497       | 0.464368       | 1.369201       | 18.31950       | 3040 B     |
| DenseMap_Xxhash3  | 800000   | 18.79378       | 0.374126       | 0.292093       | 18.75695       | 3040 B     |
| DenseMap_GxHash   | 800000   | 17.75575       | 0.196469       | 0.164060       | 17.74450       | 3040 B     |
| **DenseMap_FastHash** 🏆 | **800000**  | **16.90848**   | **0.336720**   | **0.360287**   | **16.74990**    | **3040 B** |
| RobinhoodMap      | 800000   | 36.95577       | 0.720106       | 1.121118       | 36.45030       | 3040 B     |
| Dictionary        | 800000   | 28.71253       | 0.567615       | 1.413559       | 28.26150       | 3040 B     |


🏆 indicates the fastest implementation for each input size.

