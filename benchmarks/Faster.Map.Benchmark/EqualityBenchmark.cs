using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Faster.Map.Benchmark;

[MemoryDiagnoser]
[DisassemblyDiagnoser(printSource: true)]
public class EqualityBenchmark
{
    private readonly int _i1 = 42, _i2 = 42;
    private readonly uint _ui1 = 42u, _ui2 = 42u;
    private readonly long _l1 = 42L, _l2 = 42L;
    private readonly ulong _ul1 = 42UL, _ul2 = 42UL;
    private readonly short _s1 = 42, _s2 = 42;
    private readonly ushort _us1 = 42, _us2 = 42;
    private readonly byte _b1 = 42, _b2 = 42;
    private readonly sbyte _sb1 = 42, _sb2 = 42;
    private readonly char _c1 = 'A', _c2 = 'A';
    private readonly bool _bo1 = true, _bo2 = true;

    // --- Int32 ---
    [Benchmark] public bool Default_Int() => EqualityComparer<int>.Default.Equals(_i1, _i2);
    [Benchmark] public bool Optimized_Int() => OptimizedEquals(_i1, _i2);

    // --- UInt32 ---
    [Benchmark] public bool Default_UInt() => EqualityComparer<uint>.Default.Equals(_ui1, _ui2);
    [Benchmark] public bool Optimized_UInt() => OptimizedEquals(_ui1, _ui2);

    // --- Int64 ---
    [Benchmark] public bool Default_Long() => EqualityComparer<long>.Default.Equals(_l1, _l2);
    [Benchmark] public bool Optimized_Long() => OptimizedEquals(_l1, _l2);

    // --- UInt64 ---
    [Benchmark] public bool Default_ULong() => EqualityComparer<ulong>.Default.Equals(_ul1, _ul2);
    [Benchmark] public bool Optimized_ULong() => OptimizedEquals(_ul1, _ul2);

    // --- Small Types ---
    [Benchmark] public bool Default_Byte() => EqualityComparer<byte>.Default.Equals(_b1, _b2);
    [Benchmark] public bool Optimized_Byte() => OptimizedEquals(_b1, _b2);

    [Benchmark] public bool Default_Char() => EqualityComparer<char>.Default.Equals(_c1, _c2);
    [Benchmark] public bool Optimized_Char() => OptimizedEquals(_c1, _c2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool OptimizedEquals<T>(T x, T y)
    {
        if (typeof(T) == typeof(int)) return Unsafe.As<T, int>(ref x) == Unsafe.As<T, int>(ref y);
        if (typeof(T) == typeof(uint)) return Unsafe.As<T, uint>(ref x) == Unsafe.As<T, uint>(ref y);
        if (typeof(T) == typeof(long)) return Unsafe.As<T, long>(ref x) == Unsafe.As<T, long>(ref y);
        if (typeof(T) == typeof(ulong)) return Unsafe.As<T, ulong>(ref x) == Unsafe.As<T, ulong>(ref y);
        if (typeof(T) == typeof(short)) return Unsafe.As<T, short>(ref x) == Unsafe.As<T, short>(ref y);
        if (typeof(T) == typeof(ushort)) return Unsafe.As<T, ushort>(ref x) == Unsafe.As<T, ushort>(ref y);
        if (typeof(T) == typeof(byte)) return Unsafe.As<T, byte>(ref x) == Unsafe.As<T, byte>(ref y);
        if (typeof(T) == typeof(sbyte)) return Unsafe.As<T, sbyte>(ref x) == Unsafe.As<T, sbyte>(ref y);
        if (typeof(T) == typeof(char)) return Unsafe.As<T, char>(ref x) == Unsafe.As<T, char>(ref y);
        if (typeof(T) == typeof(bool)) return Unsafe.As<T, bool>(ref x) == Unsafe.As<T, bool>(ref y);

        return EqualityComparer<T>.Default.Equals(x!, y!);
    }
}

