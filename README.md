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

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.22621
12th Gen Intel Core i5-12500H, 1 CPU, 16 logical and 12 physical cores
.NET SDK=7.0.101
  [Host]     : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT
  Job-HLVSMK : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT

InvocationCount=1  UnrollFactor=1  


### Retrieving a million random generated keys
|         Method |      Mean |     Error |    StdDev |
|--------------- |----------:|----------:|----------:|
| SlimDictionary | 14.041 ms | 0.1952 ms | 0.1731 ms |
|     Dictionary | 16.712 ms | 0.3339 ms | 0.8000 ms |
|   DenseMapSIMD | 7.272 ms | 0.0748 ms | 0.0700 ms  | 
|       DenseMap | 13.126 ms | 0.1976 ms | 0.1848 ms |
|        FastMap |  9.577 ms | 0.1660 ms | 0.1386 ms |

### Adding a million keys
|         Method |      Mean |     Error |    StdDev |
|--------------- |----------:|----------:|----------:|
|   DenseMapSIMD |  6.695 ms | 0.1332 ms | 0.2151 ms |
|       DenseMap | 42.891 ms | 0.8520 ms | 1.4235 ms |
|        FastMap | 28.681 ms | 0.5674 ms | 0.6968 ms |
|     Dictionary | 16.298 ms | 0.3254 ms | 0.7477 ms |
| DictionarySlim | 29.161 ms | 0.5290 ms | 0.4417 ms |

### Updating a million keys
|         Method |     Mean |    Error |   StdDev |
|--------------- |---------:|---------:|---------:|
| SlimDictionary | 14.624 ms | 0.2158 ms | 0.1913 ms |
|   Dictionary   | 19.102 ms | 0.3739 ms | 0.7023 ms |
| DenseMapSIMD   |  7.913 ms | 0.0811 ms | 0.0677 ms |
|      FastMap   |  9.953 ms | 0.1157 ms | 0.1082 ms |
|     DenseMap   | 13.793 ms | 0.1122 ms | 0.0995 ms |

### Removing a million keys
|         Method |      Mean |     Error |    StdDev |
|--------------- |----------:|----------:|----------:|
| SlimDictionary | 14.49 ms | 0.193 ms | 0.171 ms |
|     Dictionary | 19.58 ms | 0.328 ms | 0.403 ms |
|        FastMap | 15.88 ms | 0.291 ms | 0.272 ms |
|   DenseMapSIMD | 12.63 ms | 0.247 ms | 0.362 ms |
|       DenseMap | 20.36 ms | 0.394 ms | 0.590 ms |

### Add and resize
|         Method |     Mean |    Error |   StdDev |   Median |
|--------------- |----------:|----------:|----------:|----------:|
|   DenseMapSIMD | 11.81 ms | 0.089 ms | 0.079 ms | 12.55 ms |
|       DenseMap | 55.18 ms | 1.103 ms | 2.601 ms | 55.39 ms |
|        FastMap | 42.25 ms | 0.805 ms | 0.988 ms | 42.11 ms |
|     Dictionary | 34.22 ms | 0.673 ms | 1.007 ms | 34.24 ms |
| DictionarySlim | 27.08 ms | 0.531 ms | 0.546 ms | 27.11 ms |
