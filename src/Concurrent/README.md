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

| Method               | Length  | NumberOfThreads | Mean      | Error     | StdDev    | Median    | Allocated |
|--------------------- |-------- |---------------- |----------:|----------:|----------:|----------:|----------:|
| ConcurrentDictionary | 1000000 | 1               | 27.052 ms | 0.2012 ms | 0.1882 ms | 27.031 ms |   1.75 KB |
| NonBlocking          | 1000000 | 1               | 20.919 ms | 0.2913 ms | 0.2583 ms | 20.880 ms |   1.75 KB |
| CMap                 | 1000000 | 1               | 16.103 ms | 0.2116 ms | 0.1875 ms | 16.077 ms |   1.75 KB |
| ConcurrentDictionary | 1000000 | 2               | 17.704 ms | 0.1393 ms | 0.1303 ms | 17.730 ms |   1.96 KB |
| NonBlocking          | 1000000 | 2               | 11.472 ms | 0.1091 ms | 0.0852 ms | 11.491 ms |   2.01 KB |
| CMap                 | 1000000 | 2               | 10.137 ms | 0.1000 ms | 0.0886 ms | 10.122 ms |   2.01 KB |
| ConcurrentDictionary | 1000000 | 4               |  9.063 ms | 0.1703 ms | 0.1593 ms |  9.136 ms |   2.41 KB |
| NonBlocking          | 1000000 | 4               |  6.629 ms | 0.0864 ms | 0.0808 ms |  6.614 ms |   2.53 KB |
| CMap                 | 1000000 | 4               |  5.946 ms | 0.1135 ms | 0.1062 ms |  5.972 ms |   2.54 KB |
| ConcurrentDictionary | 1000000 | 8               |  7.115 ms | 0.0789 ms | 0.0738 ms |  7.104 ms |   3.37 KB |
| NonBlocking          | 1000000 | 8               |  5.394 ms | 0.0941 ms | 0.0834 ms |  5.357 ms |   3.49 KB |
| CMap                 | 1000000 | 8               |  4.487 ms | 0.0652 ms | 0.0609 ms |  4.473 ms |    3.6 KB |
| ConcurrentDictionary | 1000000 | 16              |  5.349 ms | 0.0535 ms | 0.0474 ms |  5.359 ms |   4.95 KB |
| NonBlocking          | 1000000 | 16              |  4.007 ms | 0.0782 ms | 0.0653 ms |  3.996 ms |   5.12 KB |
| CMap                 | 1000000 | 16              |  2.768 ms | 0.0540 ms | 0.1041 ms |  2.726 ms |   4.93 KB |


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

| Method               | Length  | NumberOfThreads | Mean      | Error     | StdDev    | Allocated |
|--------------------- |-------- |---------------- |----------:|----------:|----------:|----------:|
| ConcurrentDictionary | 1000000 | 1               |  5.594 ms | 0.0988 ms | 0.1319 ms |   1.73 KB |
| NonBlocking          | 1000000 | 1               | 21.099 ms | 0.1544 ms | 0.1444 ms |   1.75 KB |
| CMap                 | 1000000 | 1               | 36.303 ms | 0.5778 ms | 0.5405 ms |   1.79 KB |
| ConcurrentDictionary | 1000000 | 2               |  2.587 ms | 0.0511 ms | 0.0588 ms |   1.95 KB |
| NonBlocking          | 1000000 | 2               | 12.340 ms | 0.0987 ms | 0.0875 ms |   2.01 KB |
| CMap                 | 1000000 | 2               | 18.938 ms | 0.3257 ms | 0.3046 ms |   2.04 KB |
| ConcurrentDictionary | 1000000 | 4               |  1.354 ms | 0.0253 ms | 0.0249 ms |   2.43 KB |
| NonBlocking          | 1000000 | 4               |  6.945 ms | 0.0739 ms | 0.0691 ms |   2.52 KB |
| CMap                 | 1000000 | 4               | 10.878 ms | 0.2120 ms | 0.2442 ms |    2.5 KB |
| ConcurrentDictionary | 1000000 | 8               |  1.265 ms | 0.0251 ms | 0.0279 ms |   3.36 KB |
| NonBlocking          | 1000000 | 8               |  5.499 ms | 0.0965 ms | 0.0902 ms |   3.49 KB |
| CMap                 | 1000000 | 8               |  8.702 ms | 0.1719 ms | 0.4312 ms |    3.4 KB |
| ConcurrentDictionary | 1000000 | 16              |  1.014 ms | 0.0170 ms | 0.0142 ms |   4.71 KB |
| NonBlocking          | 1000000 | 16              |  5.129 ms | 0.0960 ms | 0.0898 ms |   4.84 KB |
| CMap                 | 1000000 | 16              |  5.670 ms | 0.1088 ms | 0.1336 ms |    4.9 KB |
 
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
