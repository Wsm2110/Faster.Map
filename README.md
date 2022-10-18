# Faster.Map - A collection of Robin-hood hashmaps (FastMap, DenseMap, MultiMap)

The goal of Faster is to provide the fastest hashmap that integrates into the .net framework.

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
1. FastMap<Tkey, TValue> is a hashmap  which has incredible performance, will only work with numerical keys
2. DenseMap<Tkey, TValue> is a hashmap which can be used as a replacement to IDicionary. 
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
  ### DenseMap Example
```C#
private DenseMap<uint, uint> _map = new DenseMap<uint, uint>(16);
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
|        Method |     Mean |    Error |   StdDev |
|-------------- |---------:|---------:|---------:|
| DictionarySlim | 30.91 ms | 0.612 ms | 1.645 ms |
| Dictionary | 42.84 ms | 1.062 ms | 3.098 ms |
|    FastMap | 14.61 ms | 0.289 ms | 0.646 ms |
|    DenseMap | 23.10 ms | 0.452 ms | 1.253 ms |
|   MultiMap | 23.31 ms | 0.466 ms | 1.344 ms |


### Adding a million keys
|        Method |     Mean |    Error |   StdDev |
|-------------- |---------:|---------:|---------:|
| DictionarySlim | 34.87 ms | 1.051 ms | 3.099 ms |
| Dictionary | 46.59 ms | 1.438 ms | 4.240 ms |
| FastMap | 17.05 ms | 0.341 ms | 0.978 ms |
| DenseMap | 28.47 ms | 0.679 ms | 1.990 ms |
| MultiMap | 35.51 ms | 1.097 ms | 3.234 ms |

### Updating a million keys
|           Method |      Mean |    Error |   StdDev |
|----------------- |----------:|---------:|---------:|
| DictionarySlim |  34.16 ms | 1.123 ms | 3.277 ms |
| Dictionary |  52.79 ms | 1.391 ms | 3.991 ms |
| FastMap |  15.87 ms | 0.364 ms | 1.073 ms |
| DenseMap |  26.46 ms | 0.527 ms | 1.332 ms |
| MultiMap |  28.85 ms | 0.828 ms | 1.728 ms |

### Removing a million keys
|           Method |      Mean |     Error |    StdDev |
|----------------- |----------:|----------:|----------:|
|  DictionarySlim |  5.777 ms | 0.1150 ms | 0.1985 ms |
|  Dictionary |  6.729 ms | 0.1328 ms | 0.2996 ms |
|   FastMap | 22.278 ms | 0.5384 ms | 1.5873 ms |
|   DenseMap | 45.786 ms | 0.9587 ms | 2.7814 ms |
|   MultiMap | 47.494 ms | 1.8617 ms | 3.1276 ms |
