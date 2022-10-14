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

|        Method |     Mean |    Error |   StdDev |   Median |
|-------------- |---------:|---------:|---------:|---------:|
|       GetSlim | 43.81 ms | 1.957 ms | 5.770 ms | 42.96 ms |
| GetDictionary | 63.45 ms | 1.761 ms | 5.137 ms | 62.66 ms |
|    GetFastMap | 16.59 ms | 0.916 ms | 2.671 ms | 15.38 ms |
|        GetMap | 45.27 ms | 1.467 ms | 4.301 ms | 45.36 ms |
|   GetMultiMap | 41.28 ms | 1.443 ms | 4.210 ms | 40.86 ms |

### Adding a million keys
|        Method |      Mean |     Error |    StdDev |
|-------------- |----------:|----------:|----------:|
|       AddSlim | 30.190 ms | 1.2018 ms | 3.5056 ms |
| AddDictionary |  3.562 ms | 0.1361 ms | 0.4014 ms |
|    AddFastMap | 27.612 ms | 1.2222 ms | 3.5844 ms |
|        Addmap | 53.386 ms | 2.1225 ms | 6.2583 ms |
|   AddMultiMap | 51.182 ms | 2.0225 ms | 5.1252 ms |


### Updating a million keys

|           Method |     Mean |    Error |    StdDev |
|----------------- |---------:|---------:|----------:|
|       UpdateSlim | 56.94 ms | 2.881 ms |  8.448 ms |
| UpdateDictionary | 83.83 ms | 4.083 ms | 12.039 ms |
|    UpdateFastMap | 21.33 ms | 0.493 ms |  1.438 ms |
|        UpdateMap | 56.68 ms | 1.966 ms |  5.734 ms |
|   UpdateMultiMap | 39.64 ms | 1.540 ms |  4.419 ms |

### Removing a million keys

|           Method |      Mean |     Error |    StdDev |
|----------------- |----------:|----------:|----------:|
|       RemoveSlim |  7.297 ms | 0.1730 ms | 0.4936 ms |
| RemoveDictionary |  9.002 ms | 0.3102 ms | 0.9099 ms |
|    RemoveFastMap | 35.271 ms | 1.5614 ms | 4.4547 ms |
|        RemoveMap |  2.538 ms | 0.0673 ms | 0.1972 ms |
|   RemoveMultiMap | 34.146 ms | 0.6789 ms | 1.9587 ms |

