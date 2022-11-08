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
|         Method |     Mean |    Error |   StdDev |
|--------------- |---------:|---------:|---------:|
| SlimDictionary | 28.94 ms | 0.740 ms | 2.136 ms |
|     Dictionary | 41.60 ms | 1.486 ms | 4.334 ms |
|       DenseMap | 22.06 ms | 0.573 ms | 1.644 ms |
|        FastMap | 12.74 ms | 0.239 ms | 0.425 ms |
|   MultiMap | 23.31 ms | 0.466 ms | 1.344 ms |


### Adding a million keys
|         Method |     Mean |    Error |   StdDev |
|--------------- |---------:|---------:|---------:|
| DictionarySlim | 21.34 ms | 0.424 ms | 0.904 ms |
|     Dictionary | 27.84 ms | 0.556 ms | 1.256 ms |
|        FastMap | 14.88 ms | 0.292 ms | 0.437 ms |
|       DenseMap | 26.81 ms | 0.689 ms | 2.011 ms |
|       MultiMap | 35.51 ms | 1.097 ms | 3.234 ms |

### Updating a million keys
|           Method |     Mean |    Error |   StdDev |   Median |
|----------------- |---------:|---------:|---------:|---------:|
|       DictionarySlim | 30.93 ms | 1.168 ms | 3.407 ms | 29.91 ms |
| Dictionary | 48.30 ms | 2.132 ms | 6.252 ms | 47.03 ms |
|    FastMap | 13.62 ms | 0.272 ms | 0.511 ms | 13.60 ms |
|        DenseMap | 22.63 ms | 0.487 ms | 1.420 ms | 22.63 ms |
| MultiMap |  28.85 ms | 0.828 ms | 1.728 ms |28.85|

### Removing a million keys
|         Method |      Mean |     Error |    StdDev |
|--------------- |----------:|----------:|----------:|
| SlimDictionary |  5.867 ms | 0.1152 ms | 0.2076 ms |
|     Dictionary |  7.636 ms | 0.1703 ms | 0.4994 ms |
|        FastMap | 21.410 ms | 0.5362 ms | 1.5297 ms |
|           DenseMap | 37.651 ms | 0.9171 ms | 2.6752 ms |
|   MultiMap | 47.494 ms | 1.8617 ms | 3.1276 ms |
