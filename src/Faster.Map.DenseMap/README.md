# Faster.Map

The goal of Faster.Map is to provide incredible fast hashmaps, faster than the default in .net which is currently the dictionary.
This Repository provides 3 kind of hashmaps: Densemap, Robinhoodmap and Quadmap. Each hashmap provides unique characteristics and really shine at different parts throughout.
To make sure you choose the right hashmap for your needs, we included benchmarks. 

High level concepts of DenseMap to keep in mind:

- open-addressing
- searches in parallel using SIMD
- first come first serve collision resolution
- chunked (SIMD) triangular (quadratic-ish) probing
- tombstones to avoid backshifts
- The default loadfactor is 0.9
- It's mindblowingly fast 
- https://abseil.io/about/design/swisstables or https://faultlore.com/blah/hashbrown-tldr/


High level concepts of RobinhoodMap to keep in mind:

 - Open addressing
 - Uses linear probing
 - Robinghood hashing(steal from the rich, give to the poor)
 - Upper limit on the probe sequence lenght(psl) which is Log2(size)
 - Keeps track of the currentProbeCount which makes sure we can back out early eventhough the maxprobcount exceeds the cpc
 - fibonacci hashing
 - https://programming.guide/robin-hood-hashing.html

- - High level concepts of QuadMap to keep in mind:

 - Open addressing
 - triangular (quadratic-ish) probing
 - fibonacci hashing
 - Uses the same concept like DenseMap 

1. Install nuget package Faster.Map to your project.
```
dotnet add package Faster.Map
```
## How to use

  ### DenseMap Example
```C#
private DenseMap<uint, uint> _map = new DenseMap<uint, uint>(16);
 _map.Emplace(1, 50); 
 _map.Remove(1);
 _map.Get(1, out var result);
 _map.Update(1, 51);
 _map.AddOrUpdate(1, 50);

 ref var x = _map.GetOrAdd(1);

  ``` 

 ## Tested on platforms:
* x86
* x64
* arm
* arm64

## Benchmark

``` ini

BenchmarkDotNet v0.13.8, Windows 11 (10.0.22621.2428/22H2/2022Update/SunValley2)
12th Gen Intel Core i5-12500H, 1 CPU, 16 logical and 12 physical cores
.NET SDK 8.0.100-rc.1.23463.5
  [Host]     : .NET 8.0.0 (8.0.23.41904), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.0 (8.0.23.41904), X64 RyuJIT AVX2

  
```
### Retrieving a million pre-generated keys
```
| Method       | Length  | Mean              | Error           | StdDev            | Code Size | Allocated |
|------------- |-------- |------------------:|----------------:|------------------:|----------:|----------:|
| DenseMap     | 1       |          2.149 ns |       0.0257 ns |         0.0240 ns |     312 B |         - |
**| RobinhoodMap | 1       |          1.134 ns |       0.0108 ns |         0.0096 ns |     179 B |         - |
| QuadMap      | 1       |          1.575 ns |       0.0131 ns |         0.0122 ns |     223 B |         - |
| Dictionary   | 1       |          1.771 ns |       0.0091 ns |         0.0081 ns |     420 B |         - |
| DenseMap     | 10      |         17.561 ns |       0.1200 ns |         0.1122 ns |     312 B |         - |
**| RobinhoodMap | 10      |         12.790 ns |       0.0780 ns |         0.0692 ns |     179 B |         - |
| QuadMap      | 10      |         13.588 ns |       0.0678 ns |         0.0634 ns |     223 B |         - |
| Dictionary   | 10      |         17.583 ns |       0.1700 ns |         0.1507 ns |     417 B |         - |
| DenseMap     | 100     |        161.095 ns |       2.5777 ns |         2.4112 ns |     289 B |         - |
| RobinhoodMap | 100     |        145.197 ns |       0.6498 ns |         0.6078 ns |     185 B |         - |
**| QuadMap      | 100     |        139.150 ns |       0.6484 ns |         0.5415 ns |     223 B |         - |
| Dictionary   | 100     |        178.755 ns |       0.8758 ns |         0.8192 ns |     417 B |         - |
| DenseMap     | 1000    |      1,548.900 ns |       9.3929 ns |         8.7862 ns |     312 B |         - |
**| RobinhoodMap | 1000    |      1,403.767 ns |      27.8217 ns |        27.3246 ns |     185 B |         - |
| QuadMap      | 1000    |      1,544.375 ns |      28.8677 ns |        44.9435 ns |     222 B |         - |
| Dictionary   | 1000    |      2,645.519 ns |      41.8649 ns |        39.1605 ns |     422 B |         - |
| DenseMap     | 10000   |     17,195.149 ns |      86.3734 ns |        80.7937 ns |     289 B |         - |
| RobinhoodMap | 10000   |     17,769.409 ns |      85.5364 ns |        80.0108 ns |     185 B |         - |
**| QuadMap      | 10000   |     17,104.365 ns |     160.6611 ns |       142.4219 ns |     223 B |         - |
| Dictionary   | 10000   |     46,318.604 ns |     190.4533 ns |       148.6934 ns |     422 B |         - |
**| DenseMap     | 100000  |    238,871.591 ns |     594.8745 ns |       527.3407 ns |     289 B |         - |
| RobinhoodMap | 100000  |    533,425.234 ns |   1,740.3357 ns |     1,627.9111 ns |     185 B |       1 B |
| QuadMap      | 100000  |    542,744.510 ns |   2,390.7427 ns |     2,119.3311 ns |     223 B |       1 B |
| Dictionary   | 100000  |    884,184.968 ns |   2,186.1627 ns |     1,937.9763 ns |     422 B |       1 B |
**| DenseMap     | 1000000 | 10,604,508.259 ns | 136,204.8926 ns |   120,742.0895 ns |     289 B |      12 B |
| RobinhoodMap | 1000000 | 15,126,555.578 ns | 301,947.4884 ns |   821,470.4112 ns |     185 B |      12 B |
| QuadMap      | 1000000 | 15,568,526.648 ns | 308,285.7772 ns |   656,982.1260 ns |     223 B |      23 B |
| Dictionary   | 1000000 | 19,310,078.923 ns | 383,434.8338 ns | 1,093,961.1566 ns |     415 B |      23 B |
```

### Adding a million keys
```
| Method       | Length  | Mean            | Error         | StdDev       | Median          | Code Size | Allocated |
|------------- |-------- |----------------:|--------------:|-------------:|----------------:|----------:|----------:|
| DenseMap     | 1       |        916.7 ns |      80.09 ns |     231.1 ns |        900.0 ns |     951 B |     736 B |
**| RobinhoodMap | 1       |        788.0 ns |      80.41 ns |     237.1 ns |        800.0 ns |     665 B |     736 B |
| QuadMap      | 1       |        924.0 ns |     130.48 ns |     384.7 ns |        900.0 ns |     726 B |     736 B |
| Dictionary   | 1       |        886.6 ns |      98.46 ns |     285.6 ns |        900.0 ns |     241 B |     736 B |
| DenseMap     | 10      |      1,674.0 ns |     134.98 ns |     398.0 ns |      1,550.0 ns |     951 B |     736 B |
**| RobinhoodMap | 10      |      1,028.9 ns |      84.72 ns |     245.8 ns |      1,000.0 ns |     665 B |     736 B |
| QuadMap      | 10      |      1,637.0 ns |     139.60 ns |     411.6 ns |      1,600.0 ns |     726 B |     736 B |
| Dictionary   | 10      |      1,477.1 ns |     112.17 ns |     323.6 ns |      1,400.0 ns |     241 B |     736 B |
| DenseMap     | 100     |      6,585.4 ns |     399.76 ns |   1,153.4 ns |      6,200.0 ns |     951 B |     448 B |
**| RobinhoodMap | 100     |      3,758.7 ns |     237.87 ns |     670.9 ns |      3,700.0 ns |     665 B |     736 B |
| QuadMap      | 100     |      6,222.9 ns |     487.43 ns |   1,406.3 ns |      6,050.0 ns |     726 B |     736 B |
| Dictionary   | 100     |      5,785.3 ns |     381.66 ns |   1,095.1 ns |      5,700.0 ns |     241 B |     736 B |
| DenseMap     | 1000    |     42,642.1 ns |   2,677.75 ns |   7,683.0 ns |     39,600.0 ns |     951 B |     736 B |
**| RobinhoodMap | 1000    |     27,190.6 ns |   1,756.51 ns |   5,067.9 ns |     25,950.0 ns |     665 B |     736 B |
| QuadMap      | 1000    |     58,205.4 ns |   4,188.26 ns |  11,813.1 ns |     58,850.0 ns |     726 B |     400 B |
| Dictionary   | 1000    |     42,427.3 ns |   3,690.56 ns |  10,823.8 ns |     39,600.0 ns |     241 B |     736 B |
| DenseMap     | 10000   |    154,554.0 ns |  13,111.03 ns |  38,658.2 ns |    168,250.0 ns |     951 B |     736 B |
**| RobinhoodMap | 10000   |     82,389.7 ns |   4,216.83 ns |  12,233.8 ns |     80,200.0 ns |     665 B |     736 B |
| QuadMap      | 10000   |    169,317.2 ns |   5,981.77 ns |  17,543.5 ns |    168,200.0 ns |     726 B |     736 B |
| Dictionary   | 10000   |    230,415.5 ns |  25,918.36 ns |  75,193.9 ns |    228,200.0 ns |     210 B |     736 B |
| DenseMap     | 100000  |  1,510,250.0 ns |  20,645.25 ns |  16,118.5 ns |  1,510,350.0 ns |     951 B |     736 B |
**| RobinhoodMap | 100000  |    873,204.0 ns |  44,290.67 ns | 130,592.1 ns |    858,450.0 ns |     455 B |     736 B |
| QuadMap      | 100000  |  1,038,734.7 ns |  43,601.84 ns | 127,188.6 ns |  1,021,800.0 ns |     740 B |     736 B |
| Dictionary   | 100000  |  1,978,714.0 ns | 227,974.10 ns | 672,186.8 ns |  2,160,700.0 ns |     210 B |     736 B |
| DenseMap     | 1000000 | 15,462,453.3 ns | 253,565.66 ns | 237,185.5 ns | 15,447,000.0 ns |     541 B |     736 B |
|** RobinhoodMap | 1000000 | 14,484,900.0 ns | 288,403.79 ns | 283,251.1 ns | 14,468,950.0 ns |     455 B |     736 B |
| QuadMap      | 1000000 | 16,479,456.4 ns | 327,129.02 ns | 572,940.1 ns | 16,466,700.0 ns |     740 B |     736 B |
| Dictionary   | 1000000 | 19,482,316.9 ns | 388,865.86 ns | 908,961.4 ns | 19,520,700.0 ns |     210 B |     736 B |

```
### Updating a million keys
```
| Method       | Length  | Mean              | Error           | StdDev          | Median            | Allocated |
|------------- |-------- |------------------:|----------------:|----------------:|------------------:|----------:|
| DenseMap     | 1       |          2.810 ns |       0.0570 ns |       0.0533 ns |          2.787 ns |         - |
| RobinhoodMap | 1       |          1.744 ns |       0.0187 ns |       0.0175 ns |          1.744 ns |         - |
**| QuadMap      | 1       |          1.716 ns |       0.0140 ns |       0.0117 ns |          1.713 ns |         - |
| Dictionary   | 1       |          3.537 ns |       0.0274 ns |       0.0256 ns |          3.533 ns |         - |
| DenseMap     | 10      |         16.894 ns |       0.1006 ns |       0.0840 ns |         16.884 ns |         - |
**| RobinhoodMap | 10      |         11.842 ns |       0.1045 ns |       0.0978 ns |         11.853 ns |         - |
| QuadMap      | 10      |         12.377 ns |       0.0448 ns |       0.0419 ns |         12.358 ns |         - |
| Dictionary   | 10      |         30.910 ns |       0.1126 ns |       0.1053 ns |         30.902 ns |         - |
| DenseMap     | 100     |        162.240 ns |       1.4266 ns |       1.3344 ns |        162.382 ns |         - |
**| RobinhoodMap | 100     |        135.651 ns |       0.6453 ns |       0.6037 ns |        135.828 ns |         - |
| QuadMap      | 100     |        136.923 ns |       2.3546 ns |       2.2025 ns |        135.999 ns |         - |
| Dictionary   | 100     |        309.918 ns |       1.2761 ns |       1.1936 ns |        309.814 ns |         - |
| DenseMap     | 1000    |      1,478.988 ns |       5.3437 ns |       7.1337 ns |      1,477.433 ns |         - |
**| RobinhoodMap | 1000    |      1,385.409 ns |      14.5556 ns |      13.6153 ns |      1,382.235 ns |         - |
| QuadMap      | 1000    |      1,464.897 ns |      24.5302 ns |      22.9455 ns |      1,474.236 ns |         - |
| Dictionary   | 1000    |      3,161.205 ns |      63.2353 ns |      59.1504 ns |      3,185.990 ns |         - |
| DenseMap     | 10000   |     17,182.280 ns |      93.3494 ns |      87.3191 ns |     17,195.264 ns |         - |
| RobinhoodMap | 10000   |     20,128.086 ns |      84.7392 ns |      79.2651 ns |     20,107.541 ns |         - |
**| QuadMap      | 10000   |     16,145.086 ns |     136.8411 ns |     128.0012 ns |     16,139.624 ns |         - |
| Dictionary   | 10000   |     54,373.870 ns |     303.3445 ns |     283.7486 ns |     54,395.490 ns |         - |
**| DenseMap     | 100000  |    243,997.363 ns |     811.9395 ns |     719.7632 ns |    244,132.129 ns |         - |
| RobinhoodMap | 100000  |    872,676.890 ns |  23,856.2982 ns |  70,340.8335 ns |    897,335.303 ns |       1 B |
| QuadMap      | 100000  |    903,597.342 ns |  31,370.8942 ns |  92,497.7894 ns |    930,140.039 ns |       1 B |
| Dictionary   | 100000  |    977,998.926 ns |   3,898.1523 ns |   3,455.6105 ns |    978,622.754 ns |       1 B |
**| DenseMap     | 1000000 | 10,569,871.094 ns | 162,838.8142 ns | 144,352.3674 ns | 10,565,692.188 ns |      12 B |
| RobinhoodMap | 1000000 | 13,121,053.571 ns |  49,769.1786 ns |  44,119.0805 ns | 13,112,350.781 ns |      12 B |
| QuadMap      | 1000000 | 13,535,474.665 ns |  83,141.3924 ns |  73,702.6788 ns | 13,553,058.594 ns |      12 B |
| Dictionary   | 1000000 | 18,277,522.837 ns | 186,056.3708 ns | 155,365.4961 ns | 18,242,250.000 ns |      23 B |
```

### Removing a million keys

```
| Method       | Length  | Mean            | Error        | StdDev       | Median          | Allocated |
|------------- |-------- |----------------:|-------------:|-------------:|----------------:|----------:|
| DenseMap     | 1       |      1,482.5 ns |     395.0 ns |   1,145.9 ns |      1,800.0 ns |     736 B |
**| RobinhoodMap | 1       |      1,107.3 ns |     301.0 ns |     868.4 ns |      1,350.0 ns |     736 B |
| QuadMap      | 1       |      1,383.7 ns |     369.0 ns |   1,076.3 ns |      1,700.0 ns |     736 B |
| Dictionary   | 1       |      1,806.0 ns |     505.7 ns |   1,491.0 ns |      2,100.0 ns |     736 B |
**| DenseMap     | 10      |        859.4 ns |     187.3 ns |     540.4 ns |      1,050.0 ns |     736 B |
| RobinhoodMap | 10      |      1,006.2 ns |     185.5 ns |     538.1 ns |      1,200.0 ns |     736 B |
| QuadMap      | 10      |        889.8 ns |     165.7 ns |     483.2 ns |      1,000.0 ns |     736 B |
| Dictionary   | 10      |        952.6 ns |     170.0 ns |     493.3 ns |      1,100.0 ns |     736 B |
**| DenseMap     | 100     |      1,792.9 ns |     236.7 ns |     690.3 ns |      2,100.0 ns |     736 B |
| RobinhoodMap | 100     |      2,056.6 ns |     275.7 ns |     808.6 ns |      2,500.0 ns |     736 B |
| QuadMap      | 100     |      1,843.9 ns |     267.3 ns |     784.0 ns |      2,250.0 ns |     736 B |
| Dictionary   | 100     |      2,429.2 ns |     226.8 ns |     654.4 ns |      2,650.0 ns |     736 B |
**| DenseMap     | 1000    |      9,898.0 ns |   1,726.1 ns |   5,035.2 ns |     11,150.0 ns |     736 B |
| RobinhoodMap | 1000    |     13,813.1 ns |   1,864.3 ns |   5,467.8 ns |     15,300.0 ns |     736 B |
| QuadMap      | 1000    |     13,093.9 ns |   2,046.6 ns |   5,970.1 ns |     14,400.0 ns |     736 B |
| Dictionary   | 1000    |     15,778.8 ns |   1,940.2 ns |   5,690.3 ns |     17,000.0 ns |     736 B |
**| DenseMap     | 10000   |     52,296.7 ns |   1,035.9 ns |   1,550.4 ns |     52,700.0 ns |     736 B |
| RobinhoodMap | 10000   |     89,681.8 ns |   1,774.1 ns |   2,814.0 ns |     90,000.0 ns |     736 B |
| QuadMap      | 10000   |     90,326.9 ns |   1,796.6 ns |   2,459.2 ns |     90,500.0 ns |     736 B |
| Dictionary   | 10000   |    128,544.4 ns |   2,560.7 ns |   2,739.9 ns |    127,850.0 ns |     736 B |
**| DenseMap     | 100000  |    471,295.7 ns |  12,876.9 ns |  36,529.6 ns |    464,000.0 ns |     736 B |
| RobinhoodMap | 100000  |  1,127,486.4 ns |  22,495.9 ns |  61,960.2 ns |  1,131,700.0 ns |     736 B |
| QuadMap      | 100000  |    935,820.4 ns |  20,940.1 ns |  59,403.4 ns |    921,200.0 ns |     736 B |
| Dictionary   | 100000  |  1,187,290.7 ns |  27,822.5 ns |  80,718.2 ns |  1,168,200.0 ns |     736 B |
**| DenseMap     | 1000000 | 12,691,330.4 ns | 253,806.5 ns | 320,984.0 ns | 12,686,700.0 ns |     736 B |
| RobinhoodMap | 1000000 | 16,609,715.4 ns | 264,069.4 ns | 220,510.0 ns | 16,624,100.0 ns |     736 B |
| QuadMap      | 1000000 | 15,098,803.3 ns | 292,986.1 ns | 274,059.4 ns | 15,174,450.0 ns |     736 B |
| Dictionary   | 1000000 | 20,893,285.7 ns | 332,394.2 ns | 294,658.8 ns | 20,900,150.0 ns |     736 B |
```

### Add and resize
```
| Method       | Length  | Mean            | Error           | StdDev         | Median          |
|------------- |-------- |----------------:|----------------:|---------------:|----------------:|
| DenseMap     | 1       |        660.9 ns |        56.60 ns |       159.6 ns |        700.0 ns |
**| RobinhoodMap | 1       |        415.1 ns |        53.24 ns |       151.0 ns |        400.0 ns |
| QuadMap      | 1       |        496.8 ns |        58.76 ns |       167.7 ns |        450.0 ns |
| Dictionary   | 1       |      3,348.5 ns |       577.13 ns |     1,692.6 ns |      3,100.0 ns |
**| DenseMap     | 10      |      1,160.4 ns |        99.88 ns |       288.2 ns |      1,100.0 ns |
| RobinhoodMap | 10      |      4,919.8 ns |       603.36 ns |     1,740.8 ns |      4,900.0 ns |
| QuadMap      | 10      |      5,081.1 ns |       482.40 ns |     1,344.7 ns |      5,200.0 ns |
| Dictionary   | 10      |      5,515.5 ns |       545.89 ns |     1,583.7 ns |      5,400.0 ns |
**| DenseMap     | 100     |     12,181.5 ns |       720.59 ns |     2,032.4 ns |     12,500.0 ns |
| RobinhoodMap | 100     |     21,632.6 ns |     1,788.27 ns |     5,130.9 ns |     22,900.0 ns |
| QuadMap      | 100     |     18,255.6 ns |     1,261.82 ns |     3,517.4 ns |     18,700.0 ns |
| Dictionary   | 100     |     16,663.3 ns |     1,554.03 ns |     4,533.2 ns |     17,400.0 ns |
**| DenseMap     | 1000    |     94,464.9 ns |     3,463.32 ns |     9,480.8 ns |     95,250.0 ns |
| RobinhoodMap | 1000    |    118,771.4 ns |     4,147.15 ns |    11,629.0 ns |    121,100.0 ns |
| QuadMap      | 1000    |    110,090.8 ns |     3,741.11 ns |    10,241.2 ns |    110,900.0 ns |
| Dictionary   | 1000    |     96,078.4 ns |     6,379.54 ns |    18,508.2 ns |     98,900.0 ns |
**| DenseMap     | 10000   |    275,260.5 ns |     5,461.65 ns |    14,858.8 ns |    273,600.0 ns |
| RobinhoodMap | 10000   |    503,419.8 ns |    15,119.21 ns |    41,132.9 ns |    501,500.0 ns |
| QuadMap      | 10000   |    503,400.0 ns |    15,928.56 ns |    44,665.4 ns |    504,500.0 ns |
| Dictionary   | 10000   |    458,326.4 ns |    11,753.00 ns |    32,173.6 ns |    455,000.0 ns |
**| DenseMap     | 100000  |  1,631,648.4 ns |    87,381.77 ns |   250,714.7 ns |  1,526,200.0 ns |
| RobinhoodMap | 100000  |  3,210,866.7 ns |   155,254.54 ns |   455,334.7 ns |  3,116,900.0 ns |
| QuadMap      | 100000  |  3,528,469.8 ns |   223,885.48 ns |   645,960.8 ns |  3,329,650.0 ns |
| Dictionary   | 100000  |  3,306,410.6 ns |   325,141.24 ns |   953,583.0 ns |  3,210,550.0 ns |
**| DenseMap     | 1000000 | 28,019,881.0 ns |   548,003.32 ns |   652,359.0 ns | 28,021,500.0 ns |
| RobinhoodMap | 1000000 | 37,575,275.5 ns |   934,796.89 ns | 2,726,846.4 ns | 36,722,900.0 ns |
| QuadMap      | 1000000 | 37,394,676.8 ns | 1,114,431.71 ns | 3,268,435.3 ns | 35,979,100.0 ns |
| Dictionary   | 1000000 | 39,560,111.3 ns |   765,165.13 ns | 1,742,668.2 ns | 39,747,850.0 ns |
```

### Add string benchmark

```
| Method       | Length  | Mean              | Error             | StdDev            | Median            |
|------------- |-------- |------------------:|------------------:|------------------:|------------------:|
| DenseMap     | 1       |          9.534 ns |         0.0911 ns |         0.0852 ns |          9.520 ns |
| RobinhoodMap | 1       |          8.094 ns |         0.0495 ns |         0.0463 ns |          8.087 ns |
| QuadMap      | 1       |          8.284 ns |         0.0382 ns |         0.0338 ns |          8.281 ns |
**| Dictionary   | 1       |          6.540 ns |         0.0427 ns |         0.0356 ns |          6.537 ns |
| DenseMap     | 10      |         86.201 ns |         0.2760 ns |         0.2581 ns |         86.191 ns |
| RobinhoodMap | 10      |         75.904 ns |         0.3792 ns |         0.3362 ns |         75.894 ns |
| QuadMap      | 10      |         78.724 ns |         0.3896 ns |         0.3645 ns |         78.614 ns |
**| Dictionary   | 10      |         66.105 ns |         0.3433 ns |         0.3043 ns |         66.108 ns |
| DenseMap     | 100     |        867.770 ns |         5.5659 ns |         5.2063 ns |        866.843 ns |
| RobinhoodMap | 100     |        848.949 ns |         2.7100 ns |         2.4023 ns |        849.329 ns |
| QuadMap      | 100     |        784.314 ns |         2.0141 ns |         1.8840 ns |        784.156 ns |
**| Dictionary   | 100     |        728.455 ns |         6.1969 ns |         5.4934 ns |        727.939 ns |
| DenseMap     | 1000    |      9,852.036 ns |       107.0595 ns |        94.9055 ns |      9,851.495 ns |
| RobinhoodMap | 1000    |     10,572.727 ns |       197.1196 ns |       184.3858 ns |     10,549.770 ns |
| QuadMap      | 1000    |      9,469.102 ns |       182.0475 ns |       186.9494 ns |      9,480.386 ns |
**| Dictionary   | 1000    |      7,692.520 ns |        62.4050 ns |        55.3204 ns |      7,684.452 ns |
**| DenseMap     | 10000   |    109,577.209 ns |     1,776.7877 ns |     1,575.0760 ns |    109,789.410 ns |
| RobinhoodMap | 10000   |    140,143.656 ns |     2,692.7720 ns |     2,518.8206 ns |    139,842.725 ns |
| QuadMap      | 10000   |    134,709.660 ns |     1,863.5658 ns |     1,830.2708 ns |    134,956.970 ns |
| Dictionary   | 10000   |    117,525.238 ns |     1,942.3947 ns |     1,816.9172 ns |    117,623.718 ns |
**| DenseMap     | 100000  |  1,632,230.345 ns |    26,327.5833 ns |    24,626.8378 ns |  1,637,434.863 ns |
| RobinhoodMap | 100000  |  2,551,275.411 ns |    63,884.6466 ns |   184,321.8123 ns |  2,536,954.102 ns |
| QuadMap      | 100000  |  2,042,627.858 ns |    40,766.5562 ns |   103,763.9018 ns |  2,030,122.461 ns |
| Dictionary   | 100000  |  1,670,163.060 ns |    22,722.3308 ns |    21,254.4824 ns |  1,668,201.562 ns |
| DenseMap     | 1000000 | 55,786,472.500 ns | 1,111,405.5997 ns | 1,279,896.3414 ns | 55,947,450.000 ns |
| RobinhoodMap | 1000000 | 79,357,153.968 ns | 1,508,252.2381 ns | 1,613,812.9969 ns | 79,519,842.857 ns |
| QuadMap      | 1000000 | 56,244,588.889 ns | 1,108,052.9252 ns | 1,401,332.1664 ns | 56,028,077.778 ns |
**| Dictionary   | 1000000 | 36,925,961.538 ns |   707,157.9767 ns | 2,051,593.5154 ns | 36,273,561.538 ns |

```

### Create StringWrapperBenchmark (cache hashcode)

```
| Method       | Length  | Mean              | Error           | StdDev            | Allocated |
|------------- |-------- |------------------:|----------------:|------------------:|----------:|
| DenseMap     | 1       |          8.129 ns |       0.0911 ns |         0.0852 ns |         - |
**| RobinhoodMap | 1       |          7.145 ns |       0.0481 ns |         0.0450 ns |         - |
| QuadMap      | 1       |          7.226 ns |       0.0234 ns |         0.0219 ns |         - |
| Dictionary   | 1       |          8.049 ns |       0.0695 ns |         0.0616 ns |         - |
| DenseMap     | 10      |         75.948 ns |       0.4957 ns |         0.4140 ns |         - |
**| RobinhoodMap | 10      |         69.057 ns |       0.4161 ns |         0.3892 ns |         - |
| QuadMap      | 10      |         69.927 ns |       0.3973 ns |         0.3717 ns |         - |
| Dictionary   | 10      |         78.557 ns |       0.0981 ns |         0.0819 ns |         - |
| DenseMap     | 100     |        794.637 ns |       2.7903 ns |         2.3300 ns |         - |
**| RobinhoodMap | 100     |        690.626 ns |       5.9637 ns |         5.2866 ns |         - |
| QuadMap      | 100     |        703.788 ns |       4.0291 ns |         3.7688 ns |         - |
| Dictionary   | 100     |        792.077 ns |       0.9742 ns |         0.8636 ns |         - |
| DenseMap     | 1000    |      8,653.529 ns |      64.4654 ns |        60.3009 ns |         - |
**| RobinhoodMap | 1000    |      7,804.457 ns |     152.3995 ns |       156.5031 ns |         - |
| QuadMap      | 1000    |      8,007.786 ns |     101.1570 ns |        94.6223 ns |         - |
| Dictionary   | 1000    |      8,937.827 ns |     175.4253 ns |       245.9217 ns |         - |
**| DenseMap     | 10000   |    101,036.934 ns |   2,007.3691 ns |     3,065.4646 ns |         - |
| RobinhoodMap | 10000   |    102,530.713 ns |   1,926.3016 ns |     1,801.8637 ns |         - |
| QuadMap      | 10000   |    114,679.489 ns |   1,579.7764 ns |     1,477.7238 ns |         - |
| Dictionary   | 10000   |    137,041.383 ns |   1,489.4795 ns |     1,393.2600 ns |         - |
**| DenseMap     | 100000  |  1,472,379.245 ns |  15,148.0013 ns |    14,169.4498 ns |       2 B |
| RobinhoodMap | 100000  |  2,035,077.995 ns |  27,144.3013 ns |    25,390.7963 ns |       4 B |
| QuadMap      | 100000  |  1,896,537.734 ns |  23,636.4458 ns |    22,109.5461 ns |       4 B |
| Dictionary   | 100000  |  1,940,543.151 ns |  25,341.3286 ns |    23,704.2945 ns |       4 B |
| DenseMap     | 1000000 | 50,875,500.000 ns | 272,490.5983 ns |   241,555.8179 ns |     102 B |
| RobinhoodMap | 1000000 | 50,850,475.333 ns | 485,261.4822 ns |   453,913.8916 ns |     102 B |
| QuadMap      | 1000000 | 51,706,223.333 ns | 472,085.5960 ns |   441,589.1594 ns |     102 B |
**| Dictionary   | 1000000 | 44,046,395.076 ns | 870,288.8181 ns | 1,068,792.8255 ns |      85 B |
```