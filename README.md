# Fastest robinhood hashmap

    /// This hashmap uses the following
    /// - Open addressing
    /// - Uses linear probing
    /// - Robing hood hash
    /// - Upper limit on the probe sequence lenght(psl) which is Log2(size)


BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19042.631 (20H2/October2020Update)
Intel Core i7-4770 CPU 3.40GHz (Haswell), 1 CPU, 8 logical and 4 physical cores
.NET SDK=5.0.401
  [Host]     : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT
  DefaultJob : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT
  
 Method	                 N	     Mean	  Error	     StdDev	    BranchInstructionRetired/Op	CacheMisses/Op	LLCMisses/Op
RunMap	               1000000	3.104 ms	0.0156 ms	0.0146 ms	6,031,893	                201            	188
RunDictionary 5.0	   1000000	6.653 ms	0.0255 ms	0.0226 ms	11,068,416	                420         	410
