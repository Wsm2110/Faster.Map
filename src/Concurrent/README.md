# Faster.Map
 
CMap  is a high-performance, thread-safe, lockfree concurrent hash map that uses open addressing, quadratic probing, and Fibonacci hashing to manage key-value pairs. The default load factor is set to 0.5, meaning the hash map will resize when it is half full.

 ## Tested on platforms:
* x86
* x64
* arm
* arm64

## Benchmark

The mean is divided by the length

``` ini
BenchmarkDotNet v0.13.8, Windows 11 (10.0.22621.2428/22H2/2022Update/SunValley2)
12th Gen Intel Core i5-12500H, 1 CPU, 16 logical and 12 physical cores
.NET SDK 8.0.100-rc.1.23463.5
  [Host]     : .NET 8.0.0 (8.0.23.41904), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.0 (8.0.23.41904), X64 RyuJIT AVX2 
```
### Retrieving a million pre-generated keys



### Adding a million keys



### Updating a million keys


### Removing a million keys

 
### Add and resize

| Method               | Length  | NumberOfThreads | Mean      | Error    | StdDev    | Median    | Gen0       | Gen1       | Gen2      | Allocated |
|--------------------- |-------- |---------------- |----------:|---------:|----------:|----------:|-----------:|-----------:|----------:|----------:|
| NonBlocking          | 1000000 | 1               | 324.91 ms | 6.408 ms |  9.976 ms | 324.75 ms |  6000.0000 |  4000.0000 | 1000.0000 | 132.23 MB |
| CMap                 | 1000000 | 1               |  62.97 ms | 1.257 ms |  2.680 ms |  64.08 ms |          - |          - |         - |     48 MB |
| ConcurrentDictionary | 1000000 | 1               | 352.82 ms | 5.890 ms |  5.221 ms | 354.05 ms | 11000.0000 | 10000.0000 | 3000.0000 |  99.34 MB |
|                      |         |                 |           |          |           |           |            |            |           |           |
| NonBlocking          | 1000000 | 2               | 236.26 ms | 8.065 ms | 23.781 ms | 236.23 ms |  5000.0000 |  3000.0000 | 1000.0000 |  80.28 MB |
| CMap                 | 1000000 | 2               |  63.72 ms | 2.026 ms |  5.943 ms |  66.36 ms |          - |          - |         - |     48 MB |
| ConcurrentDictionary | 1000000 | 2               | 352.58 ms | 6.963 ms |  7.151 ms | 355.23 ms | 11000.0000 | 10000.0000 | 3000.0000 |  99.79 MB |
|                      |         |                 |           |          |           |           |            |            |           |           |
| NonBlocking          | 1000000 | 4               | 214.85 ms | 7.172 ms | 21.035 ms | 219.20 ms |  5000.0000 |  3000.0000 | 1000.0000 |  80.73 MB |
| CMap                 | 1000000 | 4               |  63.48 ms | 2.080 ms |  6.132 ms |  66.42 ms |          - |          - |         - |     48 MB |
| ConcurrentDictionary | 1000000 | 4               | 338.31 ms | 6.734 ms |  8.756 ms | 339.09 ms | 11000.0000 | 10000.0000 | 3000.0000 | 100.26 MB |
|                      |         |                 |           |          |           |           |            |            |           |           |
| NonBlocking          | 1000000 | 8               | 192.75 ms | 8.312 ms | 24.507 ms | 198.89 ms |  6000.0000 |  3000.0000 | 1000.0000 |  134.5 MB |
| CMap                 | 1000000 | 8               |  66.75 ms | 2.541 ms |  7.493 ms |  69.98 ms |          - |          - |         - |     48 MB |
| ConcurrentDictionary | 1000000 | 8               | 336.02 ms | 6.541 ms |  6.424 ms | 338.61 ms | 11000.0000 | 10000.0000 | 3000.0000 |  99.93 MB |
|                      |         |                 |           |          |           |           |            |            |           |           |
| NonBlocking          | 1000000 | 16              | 173.72 ms | 5.921 ms | 17.366 ms | 176.90 ms |  6000.0000 |  4000.0000 | 1000.0000 | 135.08 MB |
| CMap                 | 1000000 | 16              |  59.39 ms | 1.180 ms |  2.411 ms |  59.35 ms |          - |          - |         - |  48.01 MB |
| ConcurrentDictionary | 1000000 | 16              | 337.56 ms | 5.460 ms |  4.840 ms | 338.32 ms | 11000.0000 | 10000.0000 | 3000.0000 |  99.49 MB |
### Add string benchmark


### Create StringWrapperBenchmark (cached hashcode)
