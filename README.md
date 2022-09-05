# Faster.Map - A collection of Robin hood hashmaps (FastMap, Map, MultiMap)

The goal of Faster is to provide the fastest dict/set that integrates into the .net scientific ecosystem.

 ### Faster.Map uses the following:
   - Open addressing
   - Linear probing
   - Upper limit on the probe sequence lenght(psl) which is Log2(size)   
   - Fibonacci hashing  

## About

Faster is a small robinhood hashmap with minimal memory overhead and incredibly fast runtime speed. See benchmarks, or try it out yourself. Faster.Map evolved from the fact that C# dictionaries in targetframework 4.0 are terribly slow. So i decided to create my own robinhood hashmap, turns out that this hashmap even performs better than the current dictionary written in .net5.

## Get Started

1. Install nuget package Faster.Map to your project.
```
dotnet add package Faster.Map
```

## How to use
Faster.Map provides 3 unique hashmaps:
1. FastMap<Tkey, TValue> is a hashmap  which is highly optimized to be used with numerical keys.
2. Map<Tkey, TValue> is a hashmap which can be used as a replacement to IDicionary. 
3. MultiMap<Tkey, Tvalue>  is a hashmap that contains of key-value pairs, while permitting multiple entries with the same key. All key-value pairs are stored in a linear fashion and wont require additional Lists e.g Dictionary<int, List<string>>
  
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
private Map<uint, uint> _map = new Map<uint, uint>(16);
 _map.Emplace(1, 50); 
 _map.Remove(1);
 _map.Get(1, out var result);
 _map.Update(1, 51);
    
``` 
  ### MultiMap Example
```C#
private MultiMap<uin,t uint> _multimap = new Map<uint, uint>(16);
 _multimap.Emplace(1, 50); 
 _multimap.Remove(24, 24);
 _multimap.RemoveAll(1);
 _multimap.Update(1, 50);
 _multimap.Get(1, out var result);
 _multimap.GetAll(1);
    
``` 

## Benchmark

| Method    |   N    | Mean     | Error     | StdDev    |  BranchInstructionRetired/Op | CacheMisses/Op | LLCMisses/Op  |
|-----------|------- |----------|-----------|-----------|------------------------------|----------------|---------------|
|FastMap        |1000000 |1.451 ms  |0.0155s    |0.0145ms   |3015435                       |175             |232            |
|Map |1000000 |3.451 ms  |0.0102s    |0.0095ms   |8040841                       |610             |3358           |
|Dictionary |1000000 |6.902 ms  |0.1305 ms  |0.1451 ms  |11,075,4822	                  | 1050           |922         | 
 
