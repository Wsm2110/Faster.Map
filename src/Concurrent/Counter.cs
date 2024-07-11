using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Faster.Map.Concurrent
{
    // Uses multiple internal counters to reduce contention. Each thread updates its own counter, and the final value is obtained by summing all internal counters.
    public class Counter
    {        
        private static int NumStripes = Environment.ProcessorCount * 4; // Should be a power of 2
        private readonly Cell[] cells = new Cell[NumStripes];

        public Counter()
        {
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = new Cell();
            }
        }

        public void Increment()
        {
            int stripe = Thread.CurrentThread.ManagedThreadId & NumStripes - 1;
            Find(cells, stripe).Increment();
        }

        public long Sum()
        {
            long sum = 0;
            foreach (var cell in cells)
            {
                sum += cell.Get();
            }
            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref T Find<T>(T[] array, int index)
        {
            ref var arr0 = ref MemoryMarshal.GetArrayDataReference(array);
            return ref Unsafe.Add(ref arr0, index);
        }

        private class Cell
        {
            private int value;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Increment()
            {
                Interlocked.Increment(ref value);
            }
            
            public int Get()
            {
                return value;
            }
        }
    }
}
