# Faster.Map - Robinhood hashmap

The goal of Faster is to provide the fastest dict/set that integrates into the .net scientific ecosystem.

 ### Faster.Map uses the following:
   - Open addressing
   - Linear probing
   - Upper limit on the probe sequence lenght(psl) which is Log2(size)   
   - Fibonacci hashing 
 
## About

Faster is a small robinhood hashmap with minimal memory overhead and incredibly fast runtime speed. See benchmarks, or try it out yourself. Faster.Map evolved from the fact that C# dictionaries in targetframework 4.0 are terribly slow. So i decided to create my own robinhood hashmap, turns out that this hashmap even performs better than the current dictionary written in .net5.

## How to use
Faster.Map provides 2 hashmaps. FastMap<> which is highly optimized to be used with numerical keys. And Map<> which has no key constraints and will resolve hashcollissions. the main difference between these two maps is the use of the EqualityComparer<T>. Numerical keys dont need an EqualityComparer<T>, hence the speedboost.
  
 ```C#
Install nuget package Faster.Map to your project

dotnet add package Faster.Map
```

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
  ### Map Example
```C#
private Map<string, uint> _map = new Map<string, uint>(16);
 _map.Emplace(1, 50); 
 _map.Remove(1);
 _map.Get(1, out var result);
 _map.Update(1, 51);
    
``` 

## Benchmark

| Method    |   N    | Mean     | Error     | StdDev    |  BranchInstructionRetired/Op | CacheMisses/Op | LLCMisses/Op  |
|-----------|------- |----------|-----------|-----------|------------------------------|----------------|---------------|
|FastMap        |1000000 |1.451 ms  |0.0155s    |0.0145ms   |3015435                       |175             |232            |
|Map |1000000 |3.451 ms  |0.0102s    |0.0095ms   |8040841                       |610             |3358           |
|Dictionary |1000000 |6.902 ms  |0.1305 ms  |0.1451 ms  |11,075,4822	                  | 1050           |922            |

 
 
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19042.631 (20H2/October2020Update)
Intel Core i7-4770 CPU 3.40GHz (Haswell), 1 CPU, 8 logical and 4 physical cores
.NET SDK=5.0.401
 
 [Host]     : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT
 DefaultJob : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT
  
