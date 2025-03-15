using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace Faster.Map.Hash;

public static class WyHash
{
    private static readonly ulong[] Secret = {
        0xa0761d6478bd642fUL, 0xe7037ed1a0b428dbUL,
        0x8ebc6af09c88c6e3UL, 0x589965cc75374cc3UL
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Mum(ref ulong a, ref ulong b)
    {
#if NET7_0_OR_GREATER
        if (Bmi2.X64.IsSupported)
        {
            ulong low = Bmi2.X64.MultiplyNoFlags(a, b);  // ✅ Gets low 64 bits
            Math.BigMul(a, b, out ulong high);           // ✅ Gets high 64 bits
            a = low;
            b = high;
        }
        else
#endif
        {
            ulong ha = a >> 32, hb = b >> 32;
            ulong la = (uint)a, lb = (uint)b;
            ulong rh = ha * hb, rm0 = ha * lb, rm1 = hb * la, rl = la * lb;

            ulong t = rl + (rm0 << 32);
            ulong c = (t < rl) ? 1UL : 0UL;
            ulong lo = t + (rm1 << 32);
            c += (lo < t) ? 1UL : 0UL;
            ulong hi = rh + (rm0 >> 32) + (rm1 >> 32) + c;

            a = lo;
            b = hi;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Mix(ulong a, ulong b)
    {
        Mum(ref a, ref b);
        return a ^ b;
    }

    public static unsafe ulong Hash(ReadOnlySpan<byte> key)
    {
        ulong seed = Secret[0];
        ulong a = 0, b = 0;
        int len = key.Length;

        // Get reference to the start of the byte span to avoid redundant calls
        ref byte start = ref MemoryMarshal.GetReference(key);
        ref var start64 = ref Unsafe.As<byte, ulong>(ref start);
        ref var start32 = ref Unsafe.As<byte, uint>(ref start);

        if (len <= 16)
        {
            if (len >= 4)
            {
                a = ((ulong)Unsafe.Add(ref start32, 0) << 32) | Unsafe.Add(ref start32, (len >> 3));
                b = ((ulong)Unsafe.Add(ref start32, len / 4 - 1) << 32) | Unsafe.Add(ref start32, len / 4 - 2);
            }
            else if (len > 0)
            {
                a = ((ulong)start << 16) | ((ulong)Unsafe.Add(ref start, len >> 1) << 8) | Unsafe.Add(ref start, len - 1);
            }
        }
        else
        {
            int i = len;
            if (i > 48)
            {
                ulong see1 = seed, see2 = seed;
                do
                {
                    seed = Mix(Unsafe.Add(ref start64, 0) ^ Secret[1], Unsafe.Add(ref start64, 1) ^ seed);
                    see1 = Mix(Unsafe.Add(ref start64, 2) ^ Secret[2], Unsafe.Add(ref start64, 3) ^ see1);
                    see2 = Mix(Unsafe.Add(ref start64, 4) ^ Secret[3], Unsafe.Add(ref start64, 5) ^ see2);
                    start64 = ref Unsafe.Add(ref start64, 6);
                    i -= 48;
                } while (i > 48);
                seed ^= see1 ^ see2;
            }
            while (i > 16)
            {
                seed = Mix(Unsafe.Add(ref start64, 0) ^ Secret[1], Unsafe.Add(ref start64, 1) ^ seed);
                start64 = ref Unsafe.Add(ref start64, 2);
                i -= 16;
            }
            a = Unsafe.Add(ref start64, i / 8 - 2);
            b = Unsafe.Add(ref start64, i / 8 - 1);
        }

        return Mix(Secret[1] ^ (ulong)len, Mix(a ^ Secret[1], b ^ seed));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Hash(ulong x) =>
        Mix(x, 0x9E3779B97F4A7C15UL);
}
