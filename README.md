# Faster.Map - A collection of hashmaps (FastMap, DenseMapSIMD and DenseMap)

The goal of Faster is to provide a collection of incredible fast hashmaps that integrates into the .net framework.
   
## About
Faster.Map is a collection of hashmaps with minimal memory overhead and incredibly fast runtime speed. See benchmarks, or try it out yourself. Faster evolved from the fact that C# dictionaries in targetframework 4.0 are terribly slow. So i decided to create my own robinhood hashmap, turns out that this hashmap even performs better than the current dictionary written in .net7.
## Get Started
1. Install nuget package Faster.Map to your project.
```
dotnet add package Faster.Map
```
## How to use
Faster.Map provides 3 unique hashmaps:
1. FastMap<Tkey, TValue> is a hashmap which has incredible performance, will only work with numerical keys. Keys need to be unique. Won`t handle hashcollisions

2. DenseMap<Tkey, TValue> is a hashmap which can be used as a replacement to IDicionary. Default loadfactor is 0.5

3. DenseMapSIMD<Tkey, TValue> is a next level hashmap using simd intructions.
   The default loadfactor is 0.9. This allows us to store 15% more entries than a dictionary while maintaining incredible speed.
   This hashmap is fast and i mean mindblowing fast.

 ## Tested on platforms:
* x86
* x64
* arm
* arm64

 ## Examples    
  ### Default Example
```C#
private FastMap<uint, uint> _map = new FastMap<uint, uint>(16);     
  _map.Emplace(1, 50); 
  _map.Remove(1);
  _map.Get(1, out var result);
  _map.Update(1, 51); 
 var result = _map[1];    
``` 
  ### DenseMap Example
```C#
private DenseMap<uint, uint> _map = new DenseMap<uint, uint>(16);
 _map.Emplace(1, 50); 
 _map.Remove(1);
 _map.Get(1, out var result);
 _map.Update(1, 51);
 ``` 
 
 ### DenseMapSIMD
``` C#
private DenseMapSIMD<uint, uint> _map = new DenseMapSIMD<uint, uint>(16);
 _map.Emplace(1, 50); 
 _map.Remove(1);
 _map.Get(1, out var result);
 _map.Update(1, 51);
``` 

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
| DenseMapSIMD   |  5.191 ms | 0.0668 ms | 0.0592 ms |     276 B |       6 B |
| DenseMap       |  9.600 ms | 0.1915 ms | 0.3689 ms |     247 B |      12 B |
| FastMap        | 15.392 ms | 0.2994 ms | 0.5399 ms |     119 B |      12 B |
| SlimDictionary |  9.589 ms | 0.1804 ms | 0.1687 ms |     393 B |      12 B |
| Dictionary     | 12.271 ms | 0.1204 ms | 0.1006 ms |     407 B |      12 B |   

### Adding a million keys
|         Method |     Mean |    Error |   StdDev |   Median |
|--------------- |---------:|---------:|---------:|---------:|
|   DenseMapSIMD | 12.38 ms | 0.600 ms | 1.750 ms | 12.87 ms |
|       DenseMap | 14.10 ms | 0.280 ms | 0.734 ms | 14.18 ms |
|        FastMap | 14.40 ms | 0.286 ms | 0.674 ms | 14.47 ms |
|     Dictionary | 16.80 ms | 0.334 ms | 0.970 ms | 16.77 ms |
| DictionarySlim | 27.29 ms | 0.395 ms | 0.330 ms | 27.21 ms |

### Updating a million keys
|         Method |     Mean |    Error |   StdDev |
|--------------- |---------:|---------:|---------:|
| DenseMapSIMD   |  6.478 ms | 0.1141 ms | 0.1067 ms |
|      FastMap   |  8.170 ms | 0.1086 ms | 0.1016 ms |
|     DenseMap   | 13.394 ms | 0.1897 ms | 0.1682 ms |
|   UpdateSlim   | 12.584 ms | 0.2393 ms | 0.2350 ms |
|   Dictionary   | 15.332 ms | 0.2669 ms | 0.2967 ms |

### Removing a million keys
|         Method |      Mean |     Error |    StdDev |
|--------------- |----------:|----------:|----------:|
| SlimDictionary | 14.49 ms | 0.193 ms | 0.171 ms |
|     Dictionary | 19.58 ms | 0.328 ms | 0.403 ms |
|        FastMap | 15.88 ms | 0.291 ms | 0.272 ms |
|   DenseMapSIMD | 9.903 ms | 0.2949 ms | 0.7972 ms |
|       DenseMap | 13.52 ms | 0.270 ms | 0.420 ms |


### Add and resize
|         Method |     Mean |    Error |   StdDev |
|--------------- |---------:|---------:|---------:|
|   DenseMapSIMD | 15.61 ms | 0.311 ms | 0.763 ms |
|       DenseMap | 23.23 ms | 0.206 ms | 0.172 ms |
|        FastMap | 25.63 ms | 0.293 ms | 0.260 ms |
|     Dictionary | 32.65 ms | 0.647 ms | 1.421 ms |
| DictionarySlim | 25.63 ms | 0.505 ms | 1.348 ms |

