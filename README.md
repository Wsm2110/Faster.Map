# Faster.Map

The goal of Faster.Map is to create a more efficient and performant alternative to the Dictionary and ConcurrentDictionary classes provided by .NET.
These standard collections are widely used for key-value pair storage in .NET applications, but they have certain limitations, particularly in high-performance and concurrent scenarios.

## Available Implementations:

* DenseMap with SIMD Instructions:
        Harnesses SIMD (Single Instruction, Multiple Data) instructions for parallel processing, resulting in accelerated lookup times.
        Ideal for scenarios demanding high throughput and optimal CPU utilization.
* RobinHoodMap with Linear Probing:
        Employs linear probing to resolve hash collisions, reducing the likelihood of clustering and improving access speed.
        Suitable for applications where a balance between performance and simplicity is required. 
* CMap  is a high-performance, thread-safe, lockfree concurrent hashmap that uses open addressing, quadratic probing, and Fibonacci hashing to manage key-value pairs. The default load factor is set to 0.5, meaning the hash map will resize when it is half full. Note: this hashmap will only allocate once while resizing.

* Installation:

You can include Faster.Map in your C# project via NuGet Package Manager:
```
Install-Package Faster.Map
```

## Basic Usage

```csharp
var map = new DenseMap<int, string>();
map.Emplace(1, "Value One");
map.Emplace(2, "Value Two");

if (map.Get(1, out var value))
{
    Console.WriteLine($"Key 1 has value: {value}");
}

map.Remove(1);
```

 ## Tested on platforms:
* x86
* x64
* arm
* arm64

## Benchmark

``` ini
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3880/23H2/2023Update/SunValley3)
12th Gen Intel Core i5-12500H, 1 CPU, 16 logical and 12 physical cores
.NET SDK 9.0.100-preview.2.24157.14
  [Host]     : .NET 9.0.0 (9.0.24.12805), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.0 (9.0.24.12805), X64 RyuJIT AVX2
```

# **Get uint Benchmark**

The **"Get Benchmark"** evaluates the performance of four data structures—**BlitzMap**, **DenseMap**, **RobinhoodMap**, and **Dictionary**—under varying load factors. The benchmark measures the time required to retrieve elements in a collection of **134,217,728** entries, focusing on how each method handles increased data density.

---

## **Key Observations**

### **Low Load Factors (0.1 - 0.3)**
- All methods perform competitively, with **RobinhoodMap** and **BlitzMap** showing the fastest execution times.
- **RobinhoodMap** is particularly effective at a **0.1** load factor (**183.7 ms**), leveraging low collision rates for quick retrievals.
- **BlitzMap** demonstrates consistent performance, offering a strong balance between memory usage and retrieval speed.

### **Medium Load Factors (0.4 - 0.6)**
- **DenseMap** begins to show its SIMD (*Single Instruction, Multiple Data*) advantage as the load factor increases.
- SIMD enables **DenseMap** to handle high data densities efficiently, minimizing cache misses and maintaining fast retrieval times.
- **Dictionary**, which uses a **chaining** mechanism, starts to show slower performance at a **0.6** load factor (**2,638.9 ms**).

### **High Load Factors (0.7 - 0.8)**
- At high load factors, **DenseMap's** SIMD acceleration becomes a critical advantage, outperforming both **Dictionary** and **RobinhoodMap**.
- **RobinhoodMap** faces significant performance issues at a **0.8** load factor, with retrieval times spiking to **11,291.2 ms**, indicating inefficiencies under heavy collision conditions.
- **Dictionary**, while more stable than **RobinhoodMap**, still struggles with a mean retrieval time of **3,920.2 ms** at a **0.8** load factor.
- **BlitzMap** maintains a balanced approach, keeping performance relatively stable even at high load factors.

---

## **Chart: Get Benchmark by Load Factor**

![Get Benchmark by Load Factor](Assets/Charts/get_benchmark_by_loadfactor.png)

---

## **Conclusion**
- **DenseMap** demonstrates the best performance at high load factors, benefiting from SIMD optimizations that handle dense data effectively.
- **BlitzMap** offers reliable performance across all load factors, showcasing versatility and stability.
- **RobinhoodMap** is best suited for low load factors, as its performance degrades significantly with increased data density.
- **Dictionary** provides consistent performance but may not be the optimal choice when high load factors are expected, due to the overhead of its chaining mechanism.

# **Insert uint Benchmark**

The **"Insert Benchmark"** evaluates the performance of four data structures—**BlitzMap**, **DenseMap**, **RobinhoodMap**, and **Dictionary**—under varying load factors. The benchmark measures the time required to insert elements into a collection of **134,217,728** entries, focusing on how each method handles increased data density.

---

## **Key Observations**

### **Low Load Factors (0.1 - 0.3)**
- All methods show strong performance, with **BlitzMap** and **RobinhoodMap** leading in speed.
- **BlitzMap** demonstrates its efficiency in memory management and insertion speed, achieving **304.3 ms** at a **0.1** load factor.
- **RobinhoodMap** remains competitive but begins to show signs of slower insertion as the load factor increases.

### **Medium Load Factors (0.4 - 0.6)**
- **DenseMap** starts to leverage its SIMD (*Single Instruction, Multiple Data*) advantage, improving performance in densely populated scenarios.
- **BlitzMap** maintains a stable insertion speed, while **Dictionary** starts to lag due to its **chaining** mechanism, reaching **3,228.3 ms** at a **0.6** load factor.
- **RobinhoodMap** performs well up to a **0.6** load factor but struggles beyond this threshold.

### **High Load Factors (0.7 - 0.8)**
- **DenseMap's** SIMD capabilities shine, allowing it to manage high insertion loads more effectively than **Dictionary** and **RobinhoodMap**.
- **RobinhoodMap** experiences a steep decline in performance at a **0.8** load factor, showing a mean insertion time of **6,367.2 ms**, highlighting its difficulty in handling high collision scenarios.
- **Dictionary**'s chaining mechanism introduces additional overhead, resulting in an insertion time of **4,802.5 ms** at a **0.8** load factor.
- **BlitzMap** continues to demonstrate balanced performance, managing insertions efficiently even under high load conditions.

---

## **Chart: Insert Benchmark by Load Factor**

![Insert Benchmark by Load Factor](Assets/Charts/insert_benchmark_by_loadfactor.png)

---

## **Conclusion**
- **BlitzMap** offers the most consistent insertion performance across all load factors, demonstrating versatility and stability.
- **DenseMap** is particularly effective at high load factors, utilizing SIMD to maintain fast insertion times.
- **RobinhoodMap** is suitable for low load factors but encounters performance issues as collisions increase.
- **Dictionary** provides stable but slower performance, with its chaining method becoming a bottleneck under heavy load conditions.


# **Update uint Benchmark**

The **"Update Benchmark"** evaluates the performance of four data structures—**BlitzMap**, **DenseMap**, **RobinhoodMap**, and **Dictionary**—under varying load factors. The benchmark measures the time required to update elements in a collection of **134,217,728** entries, focusing on how each method handles increased data density.

---

## **Key Observations**

### **Low Load Factors (0.1 - 0.3)**
- All methods perform competitively, with **RobinhoodMap** and **BlitzMap** showing the fastest execution times.
- **RobinhoodMap** is particularly effective at a **0.1** load factor (**197.1 ms**), leveraging low collision rates for quick updates.
- **BlitzMap** maintains steady performance, showing an early advantage in memory allocation and update speed.

### **Medium Load Factors (0.4 - 0.6)**
- As the load factor increases, **DenseMap** starts to benefit from its **SIMD** (*Single Instruction, Multiple Data*) optimization.
- SIMD allows **DenseMap** to handle more data simultaneously, minimizing the performance hit from higher collision rates.
- **Dictionary**, which uses a **chaining** mechanism, starts to experience performance degradation, especially at a **0.6** load factor, reaching **3,123.2 ms**.

### **High Load Factors (0.7 - 0.8)**
- **DenseMap's** SIMD advantage becomes apparent, outperforming **Dictionary** and **RobinhoodMap** at high load factors.
- **RobinhoodMap** struggles significantly at a **0.8** load factor, reaching **11,387.8 ms**, indicating a severe performance bottleneck under heavy collision scenarios.
- **Dictionary**, with its **chaining** method, also struggles but is more stable than **RobinhoodMap**, with a mean execution time of **4,676.8 ms** at **0.8** load factor.
- **BlitzMap** manages to keep performance relatively consistent, highlighting its balanced approach to memory management and collision handling.

---

## **Chart: Update Benchmark by Load Factor**

![Update Benchmark by Load Factor](Assets/Charts/update_benchmark_by_loadfactor.png)

---

## **Conclusion**
For scenarios with high load factors, **DenseMap** is a strong choice due to its SIMD advantage. **BlitzMap** offers incredible performance across all conditions, while **RobinhoodMap** and **Dictionary** are better suited for low to moderate load factors.

