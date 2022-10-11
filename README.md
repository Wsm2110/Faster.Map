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
3. MultiMap<Tkey, Tvalue>  is a hashmap that contains of key-value pairs, while permitting multiple entries with the same key. All key-value pairs are stored in a linear fashion and won’t require additional Lists e.g Dictionary<int, List<string>>  
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
private MultiMap<uint, uint> _multimap = new Map<uint, uint>(16);
 _multimap.Emplace(1, 50); 
 _multimap.Remove(24, 24);
 _multimap.RemoveAll(1);
 _multimap.Update(1, 50);
 _multimap.Get(1, out var result);
 _multimap.GetAll(1);
 ``` 
## Benchmark
### Retrieving a million random generated keys
| Method    |   N    | Mean     | Error     | StdDev    |
|-----------|------- |----------|-----------|-----------|
|Dictionary |1000000 |41.01 ms  |1.266 ms  |3.692 ms  | 
|DictionarySlim |1000000 |32.93 ms  |0.752 ms  |2.182 ms | 
|FastMap        |1000000 |20.11 ms  |0.716 ms  |2.015ms|
|Map |1000000 |31.03 ms  |0.875 ms  |0.2568 ms |
|MultiMap |1000000 |30.55 ms  |0.670 ms  |1.529 ms |

### Adding a million keys
| Method    |   N    | Mean     | Error     | StdDev    |
|-----------|------- |----------|-----------|-----------|
|Dictionary |1000000 |3.038 ms  |0.0694 ms  |0.2046 ms  | 
|DictionarySlim |1000000 |23.342 ms  |0.4365 ms  |1.1966 ms | 
|FastMap        |1000000 |27.01 ms  |0.5716 ms  |1.6854 ms|
|Map |1000000 |39.7473 ms  |0.7784 ms  |1.3218 ms |
|MultiMap |1000000 |37.55 ms  |0.6320 ms  |1.224 ms |

### Updating a million keys
| Method    |   N    | Mean     | Error     | StdDev    |
|-----------|------- |----------|-----------|-----------|
|Dictionary |1000000 |80.00 ms  |3.038 ms  |8.815 ms  | 
|DictionarySlim |1000000 |53.54 ms  |5.087 ms  |15.001 ms | 
|FastMap        |1000000 |24.01 ms  |0.804 ms  |2.372 ms|
|Map |1000000 |50.46 ms  |1.515 ms  |4.467 ms |
|MultiMap |1000000 |35.40 ms  |1.110 ms  |3.257 ms |

### Removing a million keys
| Method    |   N    | Mean     | Error     | StdDev    |
|-----------|------- |----------|-----------|-----------|
|Dictionary |1000000 |10.668 ms  |0.6441 ms  |1.87878 ms  | 
|DictionarySlim |1000000 |8.293 ms  |0.6441 ms  |1.8787 ms | 
|FastMap        |1000000 |3.183 ms  |0.1173 ms  |0.3420 ms|
|Map |1000000 |3.229 ms  |0.1309 ms  |0.3840 ms |
|MultiMap |1000000 |3.680 ms  |0.1530 ms  |0.3257 ms |
