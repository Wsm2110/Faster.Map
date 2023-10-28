# Faster.Map

The goal of Faster is to provide an incredible fast hashmap, faster than the default in .net which is currently the dictionary.
During the years we've tried lots of different types of hashmaps for example robinhood hashing or hashmaps using quadratic probing.
All of them have beat the dictionary in terms of performance. But since the release of SIMD instructions in dotNetCore we tried 
a different approach which is well described in https://abseil.io/about/design/swisstables or https://faultlore.com/blah/hashbrown-tldr/

High level concepts of DenseMap to keep in mind:

- open-addressing
- searches in parallel using SIMD
- first come first serve collision resolution
- chunked (SIMD) triangular (quadratic-ish) probing
- tombstones to avoid backshifts
- The default loadfactor is 0.9
- It's mindblowingly fast 

1. Install nuget package Faster.Map to your project.
```
dotnet add package Faster.Map
```
## How to use

  ### DenseMap Example
```C#
private DenseMap<uint, uint> _map = new DenseMap<uint, uint>(16);
 _map.Emplace(1, 50); 
 _map.Remove(1);
 _map.Get(1, out var result);
 _map.Update(1, 51);
 _map.AddOrUpdate(1, 50);

 ref var x = _map.GetOrAdd(1);

  ``` 

 ## Tested on platforms:
* x86
* x64
* arm
* arm64

## Benchmark

``` ini

BenchmarkDotNet v0.13.8, Windows 11 (10.0.22621.2428/22H2/2022Update/SunValley2)
12th Gen Intel Core i5-12500H, 1 CPU, 16 logical and 12 physical cores
.NET SDK 8.0.100-rc.1.23463.5
  [Host]     : .NET 8.0.0 (8.0.23.41904), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.0 (8.0.23.41904), X64 RyuJIT AVX2

### Retrieving a million random generated keys

| Method         | Mean      | Error     | StdDev    | Code Size | Allocated |
|--------------- |----------:|----------:|----------:|----------:|----------:|
| DenseMap       |  5.191 ms | 0.0668 ms | 0.0592 ms |     276 B |       6 B |
| SlimDictionary |  9.589 ms | 0.1804 ms | 0.1687 ms |     393 B |      12 B |
| Dictionary     | 16.974 ms | 0.7074 ms | 2.0859 ms |     400 B |      12 B |

### Adding a million keys

| Method         | Mean     | Error    | StdDev   | Median   | Code Size | Allocated  |
|--------------- |---------:|---------:|---------:|---------:|----------:|-----------:|
| DenseMap       | 12.64 ms | 0.376 ms | 1.108 ms | 13.81 ms |     383 B |      736 B |
| Dictionary     | 16.57 ms | 0.454 ms | 1.317 ms | 16.65 ms |     741 B |      736 B |
| DictionarySlim | 22.62 ms | 0.449 ms | 0.876 ms | 22.44 ms |     367 B |      736 B |

### Updating a million keys
| Method        | Mean      | Error     | StdDev    |
| DenseMap      |  5.271 ms | 0.0498 ms | 0.0416 ms |
| DictionarySlim| 11.188 ms | 0.1745 ms | 0.1457 ms |

### Removing a million keys

| Method         | Mean      | Error     | StdDev    |
|--------------- |----------:|----------:|----------:|
| DenseMap       |  8.811 ms | 0.1762 ms | 0.4321 ms |
| SlimDictionary | 13.150 ms | 0.2602 ms | 0.6134 ms |
| Dictionary     | 17.063 ms | 0.3516 ms | 1.0313 ms |

### Add and resize
| Method         | Mean     | Error    | StdDev   | Median   |
|--------------- |---------:|---------:|---------:|---------:|
| DenseMap       | 14.19 ms | 0.722 ms | 2.119 ms | 14.98 ms |
| Dictionary     | 31.30 ms | 0.624 ms | 1.590 ms | 30.98 ms |
| DictionarySlim | 22.63 ms | 0.451 ms | 0.980 ms | 22.55 ms |

