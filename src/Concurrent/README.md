# CMap

  Is a high-performance, thread-safe concurrent hashmap implemented using open addressing, quadratic probing, and Fibonacci hashing.
  It efficiently handles concurrent access and minimizes contention between threads.

 ## Tested on platforms:
* x86
* x64
* arm
* arm64

## Benchmark

The mean is divided by the length

``` ini
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3880/23H2/2023Update/SunValley3)
12th Gen Intel Core i5-12500H, 1 CPU, 16 logical and 12 physical cores
.NET SDK 9.0.100-preview.2.24157.14
  [Host]     : .NET 9.0.0 (9.0.24.12805), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.0 (9.0.24.12805), X64 RyuJIT AVX2
```
### Retrieving a million pre-generated keys

| Method               | Length  | NumberOfThreads | Mean      | Error     | StdDev    | Allocated |
|--------------------- |-------- |---------------- |----------:|----------:|----------:|----------:|
| ConcurrentDictionary | 1000000 | 1               | 26.658 ms | 0.2631 ms | 0.2333 ms |   1.75 KB |
| NonBlocking          | 1000000 | 1               | 20.674 ms | 0.2542 ms | 0.2123 ms |   1.75 KB |
| CMap                 | 1000000 | 1               | 16.019 ms | 0.2072 ms | 0.1938 ms |   1.75 KB |
| ConcurrentDictionary | 1000000 | 2               | 16.412 ms | 0.2622 ms | 0.2453 ms |   1.96 KB |
| NonBlocking          | 1000000 | 2               | 11.383 ms | 0.1851 ms | 0.1641 ms |   2.01 KB |
| CMap                 | 1000000 | 2               | 10.319 ms | 0.0858 ms | 0.0716 ms |   2.01 KB |
| ConcurrentDictionary | 1000000 | 4               | 17.133 ms | 0.3297 ms | 0.3084 ms |   2.37 KB |
| NonBlocking          | 1000000 | 4               |  6.779 ms | 0.1311 ms | 0.1510 ms |   2.51 KB |
| CMap                 | 1000000 | 4               |  6.364 ms | 0.0873 ms | 0.0774 ms |   2.54 KB |
| ConcurrentDictionary | 1000000 | 8               |  6.967 ms | 0.0616 ms | 0.0515 ms |   3.35 KB |
| NonBlocking          | 1000000 | 8               |  5.198 ms | 0.0708 ms | 0.0628 ms |   3.48 KB |
| CMap                 | 1000000 | 8               |  4.470 ms | 0.0523 ms | 0.0490 ms |   3.59 KB |
| ConcurrentDictionary | 1000000 | 16              |  5.359 ms | 0.1043 ms | 0.1318 ms |   4.95 KB |
| NonBlocking          | 1000000 | 16              |  4.003 ms | 0.0782 ms | 0.0653 ms |   5.16 KB |
| CMap                 | 1000000 | 16              |  2.674 ms | 0.0509 ms | 0.0747 ms |   5.17 KB |


### Adding a million keys

| Method               | Length  | NumberOfThreads | Mean       | Error     | StdDev    | Median     | Gen0      | Gen1      | Allocated   |
|--------------------- |-------- |---------------- |-----------:|----------:|----------:|-----------:|----------:|----------:|------------:|
| NonBlocking          | 1000000 | 1               | 118.129 ms | 1.4671 ms | 1.3724 ms | 118.017 ms | 2000.0000 | 1000.0000 | 23439.69 KB |
| CMap                 | 1000000 | 1               |  41.847 ms | 0.8307 ms | 1.3648 ms |  41.815 ms |         - |         - |     2.26 KB |
| ConcurrentDictionary | 1000000 | 1               | 122.596 ms | 2.4282 ms | 2.8906 ms | 122.222 ms | 4000.0000 | 3000.0000 | 39064.69 KB |
| NonBlocking          | 1000000 | 2               |  84.458 ms | 1.5932 ms | 2.1808 ms |  83.799 ms | 2000.0000 | 1000.0000 | 23441.23 KB |
| CMap                 | 1000000 | 2               |  20.481 ms | 0.3972 ms | 0.8024 ms |  20.401 ms |         - |         - |     2.77 KB |
| ConcurrentDictionary | 1000000 | 2               | 165.067 ms | 2.4957 ms | 2.3345 ms | 165.528 ms | 4000.0000 | 3000.0000 | 39065.13 KB |
| NonBlocking          | 1000000 | 4               |  65.981 ms | 1.2873 ms | 2.2881 ms |  66.014 ms | 2000.0000 | 1000.0000 | 23442.72 KB |
| CMap                 | 1000000 | 4               |  10.142 ms | 0.2002 ms | 0.3346 ms |  10.092 ms |         - |         - |     3.33 KB |
| ConcurrentDictionary | 1000000 | 4               | 155.882 ms | 3.0452 ms | 3.1272 ms | 155.201 ms | 4000.0000 | 3000.0000 | 39065.55 KB |
| NonBlocking          | 1000000 | 8               |  60.029 ms | 1.1932 ms | 2.3829 ms |  59.821 ms | 2000.0000 | 1000.0000 | 23446.35 KB |
| CMap                 | 1000000 | 8               |   6.885 ms | 0.2010 ms | 0.5799 ms |   6.609 ms |         - |         - |     4.45 KB |
| ConcurrentDictionary | 1000000 | 8               | 150.669 ms | 2.5902 ms | 2.4228 ms | 150.207 ms | 4000.0000 | 3000.0000 | 39066.45 KB |
| NonBlocking          | 1000000 | 16              |  58.355 ms | 1.1608 ms | 1.9711 ms |  58.414 ms | 2000.0000 | 1000.0000 | 23447.91 KB |
| CMap                 | 1000000 | 16              |   4.844 ms | 0.0935 ms | 0.1216 ms |   4.842 ms |         - |         - |      6.7 KB |
| ConcurrentDictionary | 1000000 | 16              | 129.973 ms | 2.4430 ms | 3.1766 ms | 130.001 ms | 4000.0000 | 3000.0000 | 39068.08 KB |

### Updating a million keys

| Method               | Length  | NumberOfThreads | Mean       | Error     | StdDev    | Gen0      | Gen1      | Allocated   |
|--------------------- |-------- |---------------- |-----------:|----------:|----------:|----------:|----------:|------------:|
| NonBlocking          | 1000000 | 1               | 110.841 ms | 1.3908 ms | 1.2329 ms | 2000.0000 | 1000.0000 | 23440.22 KB |
| CMap                 | 1000000 | 1               |  11.152 ms | 0.2222 ms | 0.5323 ms |         - |         - |     2.72 KB |
| ConcurrentDictionary | 1000000 | 1               | 132.459 ms | 2.3686 ms | 2.9955 ms | 4000.0000 | 3000.0000 | 39065.22 KB |
| NonBlocking          | 1000000 | 8               |  60.482 ms | 1.1947 ms | 2.2145 ms | 2000.0000 | 1000.0000 | 23446.65 KB |
| CMap                 | 1000000 | 8               |   2.854 ms | 0.0818 ms | 0.2400 ms |         - |         - |     4.38 KB |
| ConcurrentDictionary | 1000000 | 8               | 137.022 ms | 2.7136 ms | 2.9035 ms | 4000.0000 | 3000.0000 | 39066.82 KB |
| NonBlocking          | 1000000 | 16              |  57.862 ms | 1.1253 ms | 1.6494 ms | 2000.0000 | 1000.0000 | 23448.46 KB |
| CMap                 | 1000000 | 16              |   2.694 ms | 0.1004 ms | 0.2914 ms |         - |         - |     5.79 KB |
| ConcurrentDictionary | 1000000 | 16              | 117.916 ms | 2.1711 ms | 2.5003 ms | 4000.0000 | 3000.0000 | 39068.76 KB |
| NonBlocking          | 1000000 | 32              |  54.942 ms | 1.0894 ms | 1.6306 ms | 2000.0000 | 1000.0000 | 23448.55 KB |
| CMap                 | 1000000 | 32              |   2.270 ms | 0.0621 ms | 0.1782 ms |         - |         - |     5.91 KB |
| ConcurrentDictionary | 1000000 | 32              | 105.303 ms | 1.9129 ms | 1.6957 ms | 4000.0000 | 3000.0000 |  39068.7 KB |
| NonBlocking          | 1000000 | 64              |  53.919 ms | 1.0430 ms | 1.1593 ms | 2000.0000 | 1000.0000 |  23448.4 KB |
| CMap                 | 1000000 | 64              |   2.312 ms | 0.0961 ms | 0.2678 ms |         - |         - |     5.79 KB |
| ConcurrentDictionary | 1000000 | 64              |  95.727 ms | 1.4824 ms | 1.5223 ms | 4000.0000 | 3000.0000 | 39068.57 KB |
| NonBlocking          | 1000000 | 128             |  56.711 ms | 1.1341 ms | 2.1301 ms | 2000.0000 | 1000.0000 |  23448.4 KB |
| CMap                 | 1000000 | 128             |   2.228 ms | 0.0875 ms | 0.2552 ms |         - |         - |     5.85 KB |
| ConcurrentDictionary | 1000000 | 128             |  88.947 ms | 1.7320 ms | 2.1905 ms | 4000.0000 | 3000.0000 |  39068.6 KB |

### Removing a million keys

| Method               | Length  | NumberOfThreads | Mean      | Error     | StdDev    | Median    | Allocated |
|--------------------- |-------- |---------------- |----------:|----------:|----------:|----------:|----------:|
| ConcurrentDictionary | 1000000 | 1               | 81.100 ms | 1.4729 ms | 1.3057 ms | 81.128 ms |    2.2 KB |
| NonBlocking          | 1000000 | 1               | 72.727 ms | 1.4092 ms | 1.7306 ms | 72.605 ms |    2.2 KB |
| CMap                 | 1000000 | 1               | 38.879 ms | 0.7483 ms | 0.7685 ms | 38.688 ms |    2.2 KB |
| ConcurrentDictionary | 1000000 | 2               | 97.203 ms | 2.8311 ms | 8.3032 ms | 97.641 ms |   2.63 KB |
| NonBlocking          | 1000000 | 2               | 38.481 ms | 0.7013 ms | 1.0709 ms | 38.518 ms |   3.19 KB |
| CMap                 | 1000000 | 2               | 23.204 ms | 0.5885 ms | 1.5708 ms | 23.209 ms |   2.63 KB |
| ConcurrentDictionary | 1000000 | 4               | 91.633 ms | 2.1806 ms | 6.3952 ms | 91.293 ms |   3.05 KB |
| NonBlocking          | 1000000 | 4               | 21.225 ms | 0.2979 ms | 0.2326 ms | 21.293 ms |   4.41 KB |
| CMap                 | 1000000 | 4               | 12.889 ms | 0.2237 ms | 0.3417 ms | 12.880 ms |   3.05 KB |
| ConcurrentDictionary | 1000000 | 8               | 75.948 ms | 1.6868 ms | 4.9205 ms | 75.359 ms |    3.9 KB |
| NonBlocking          | 1000000 | 8               | 16.144 ms | 0.3227 ms | 0.3842 ms | 16.162 ms |   6.45 KB |
| CMap                 | 1000000 | 8               |  9.249 ms | 0.2001 ms | 0.5836 ms |  9.242 ms |    3.9 KB |
| ConcurrentDictionary | 1000000 | 16              | 57.463 ms | 1.1482 ms | 1.9183 ms | 56.532 ms |   5.59 KB |
| NonBlocking          | 1000000 | 16              | 12.441 ms | 0.2391 ms | 0.3273 ms | 12.520 ms |   8.13 KB |
| CMap                 | 1000000 | 16              |  6.901 ms | 0.1377 ms | 0.3219 ms |  6.914 ms |   5.59 KB |
 
### Add and resize

| Method               | Length  | NumberOfThreads | Mean      | Error    | StdDev    | Median    | Gen0       | Gen1       | Gen2      | Allocated |
|--------------------- |-------- |---------------- |----------:|---------:|----------:|----------:|-----------:|-----------:|----------:|----------:|
| NonBlocking          | 1000000 | 1               | 327.20 ms | 5.587 ms |  4.953 ms | 327.08 ms |  6000.0000 |  4000.0000 | 1000.0000 | 132.23 MB |
| CMap                 | 1000000 | 1               |  66.22 ms | 1.318 ms |  3.330 ms |  67.72 ms |          - |          - |         - |     48 MB |
| ConcurrentDictionary | 1000000 | 1               | 357.79 ms | 7.098 ms |  9.229 ms | 355.60 ms | 11000.0000 | 10000.0000 | 3000.0000 |  99.34 MB |
| NonBlocking          | 1000000 | 2               | 238.66 ms | 8.794 ms | 25.931 ms | 239.94 ms |  5000.0000 |  3000.0000 | 1000.0000 |  79.87 MB |
| CMap                 | 1000000 | 2               |  37.89 ms | 0.937 ms |  2.762 ms |  36.81 ms |          - |          - |         - |     48 MB |
| ConcurrentDictionary | 1000000 | 2               | 355.11 ms | 6.989 ms | 11.085 ms | 354.40 ms | 11000.0000 | 10000.0000 | 3000.0000 |  99.61 MB |
| NonBlocking          | 1000000 | 4               | 214.84 ms | 8.096 ms | 23.743 ms | 217.61 ms |  7000.0000 |  5000.0000 | 2000.0000 | 135.02 MB |
| CMap                 | 1000000 | 4               |  24.52 ms | 1.114 ms |  3.286 ms |  22.84 ms |          - |          - |         - |     48 MB |
| ConcurrentDictionary | 1000000 | 4               | 338.47 ms | 3.924 ms |  3.671 ms | 338.67 ms | 11000.0000 | 10000.0000 | 3000.0000 |  99.93 MB |
| NonBlocking          | 1000000 | 8               | 195.28 ms | 6.744 ms | 19.886 ms | 197.89 ms |  5000.0000 |  3000.0000 | 1000.0000 |  80.59 MB |
| CMap                 | 1000000 | 8               |  27.21 ms | 2.669 ms |  7.395 ms |  25.28 ms |          - |          - |         - |  48.01 MB |
| ConcurrentDictionary | 1000000 | 8               | 337.40 ms | 4.788 ms |  4.479 ms | 338.68 ms | 11000.0000 | 10000.0000 | 3000.0000 |  99.15 MB |
| NonBlocking          | 1000000 | 16              | 183.48 ms | 6.264 ms | 18.370 ms | 183.82 ms |  5000.0000 |  3000.0000 | 1000.0000 |  80.72 MB |
| CMap                 | 1000000 | 16              |  24.41 ms | 1.583 ms |  4.464 ms |  23.26 ms |          - |          - |         - |  48.01 MB |
| ConcurrentDictionary | 1000000 | 16              | 342.46 ms | 6.179 ms |  7.588 ms | 339.73 ms | 11000.0000 | 10000.0000 | 3000.0000 |  99.37 MB |
