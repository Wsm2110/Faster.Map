using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Map.BlitzMap.Tests;

public class FastRandom
{
    private ulong _state;

    public FastRandom(ulong seed)
    {
        _state = seed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Next()
    {
        _state += 0xa0761d6478bd642f;
        UInt128 product = (UInt128)_state * (_state ^ 0xe7037ed1a0b428db);
        return (ulong)(product >> 64) ^ (ulong)product;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Next(int minValue, int maxValue)
    {
        ulong range = (ulong)(maxValue - minValue);
        return minValue + (int)((Next() * range) >> 64);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double NextDouble()
    {
        return (Next() >> 11) * (1.0 / (1UL << 53));
    }
}


