# Fastest robinhood hashmap

    /// This hashmap uses the following
    /// - Open addressing
    /// - Uses linear probing
    /// - Robing hood hash
    /// - Upper limit on the probe sequence lenght(psl) which is Log2(size)

Usage:

private Map<uint, uint> _map = new Map<uint, uint>(16); 
 * _map.Emplace(1, 50); 
 * _map.Remove(1);
 * _map.Get(1, out var result);
 * _map.Update(1, 51);

private GenericMap<string, uint> _map = new GenericMap<uint, uint>(16); // is slower, still faster than IDictionary :)
 * _map.Emplace(1, 50); 
 * _map.Remove(1);
 * _map.Get(1, out var result);
 * _map.Update(1, 51);

if this isn`t enough you should probably implement your own fixedKeyMap 

| Method |   N   | Mean     | Error     | StdDev    |  BranchInstructionRetired/Op | CacheMisses/Op | LLCMisses/Op  |
|--------|-------|----------|-----------|-----------|------------------------------|----------------|---------------|
|FixedKeyMap |1000000|1.151 ms  |0.067s  |0.062ms  |3013435                  |125          |137          |
|Map     |1000000|1.451 ms  |0.0155s  |0.0145ms  |3015435                  |175          |232          |
|GenericMap |1000000|3.751 ms  |0.0102s  |0.0095ms  |8040841                |610          |3358          |
|Dictionary|1000000|6.902 ms  |0.1305 ms |0.1451 ms|  11,075,4822	           | 1050          |922            |


BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19042.631 (20H2/October2020Update)
Intel Core i7-4770 CPU 3.40GHz (Haswell), 1 CPU, 8 logical and 4 physical cores
.NET SDK=5.0.401
  [Host]     : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT
  DefaultJob : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT
  
