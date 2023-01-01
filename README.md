# Faster.Map - A collection of Robin-hood hashmaps (FastMap, DenseMapSIMD and DenseMap)

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
   This hashmap is fast and i mean mindblowing fast. DenseMapSIMD wont cache hashcodes, using types like strings actually need a wrapper that caches the hashcode. Hence using a slow hash fuction will result in a slow hashmap.

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
private DenseMapSIMD<uint, uint> _map = new DenseMapSIMD<uint, uint>(16);
 _map.Emplace(1, 50); 
 _map.Remove(1);
 _map.Get(1, out var result);
 _map.Update(1, 51);
``` 

## Benchmark

### Retrieving a million random generated keys
|         Method |      Mean |     Error |    StdDev |
|--------------- |----------:|----------:|----------:|
| SlimDictionary | 19.305 ms | 0.1827 ms | 0.1620 ms |
|     Dictionary | 23.199 ms | 0.4628 ms | 0.5144 ms |
|   DenseMapSIMD |  9.685 ms | 0.1328 ms | 0.1109 ms |
|       DenseMap | 15.714 ms | 0.3012 ms | 0.2958 ms |
|        FastMap | 11.608 ms | 0.1197 ms | 0.1061 ms |
w	

### Adding a million keys
|         Method |     Mean |    Error |   StdDev |
|--------------- |---------:|---------:|---------:|
| DictionarySlim | 21.34 ms | 0.424 ms | 0.904 ms |
|     Dictionary | 27.84 ms | 0.556 ms | 1.256 ms |
|   DenseMapSIMD | 10.04 ms | 0.305 ms | 0.857 ms |
|        FastMap | 14.88 ms | 0.292 ms | 0.437 ms |
|       DenseMap | 26.81 ms | 0.689 ms | 2.011 ms |

### Updating a million keys
|           Method |     Mean |    Error |   StdDev |   Median |
|----------------- |---------:|---------:|---------:|---------:|
|       DictionarySlim | 30.93 ms | 1.168 ms | 3.407 ms | 29.91 ms |
| Dictionary | 48.30 ms | 2.132 ms | 6.252 ms | 47.03 ms |
|    FastMap | 13.62 ms | 0.272 ms | 0.511 ms | 13.60 ms |
|        DenseMap | 22.63 ms | 0.487 ms | 1.420 ms | 22.63 ms |

### Removing a million keys
|         Method |      Mean |     Error |    StdDev |
|--------------- |----------:|----------:|----------:|
| SlimDictionary |  5.867 ms | 0.1152 ms | 0.2076 ms |
|     Dictionary |  7.636 ms | 0.1703 ms | 0.4994 ms |
|        FastMap | 21.410 ms | 0.5362 ms | 1.5297 ms |
|           DenseMap | 37.651 ms | 0.9171 ms | 2.6752 ms |