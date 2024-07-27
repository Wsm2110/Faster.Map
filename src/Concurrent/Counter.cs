using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Faster.Map.Concurrent
{
    // Uses multiple internal counters to reduce contention. Each thread updates its own counter, and the final value is obtained by summing all internal counters.
    public class Counter
    {
        private int NumStripes; // Should be a power of 2
        private int NumStripedMinusOne;
        public readonly Cell[] cells;

        public Counter()
        {
            NumStripes = Environment.ProcessorCount * 4;
            NumStripedMinusOne = NumStripes - 1;
            cells = new Cell[NumStripes];
            for (int i = 0; i < NumStripes; i++)
            {
                cells[i] = new Cell();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref Cell Find(Cell[] array, int index)
        {
            ref var arr0 = ref MemoryMarshal.GetArrayDataReference(array);
            return ref Unsafe.Add(ref arr0, index);
        }

        public void Increment()
        {
            int stripe = Thread.CurrentThread.ManagedThreadId & NumStripedMinusOne;
            Find(cells, stripe).Increment();
        }

        public void Decrement()
        {
            int stripe = Thread.CurrentThread.ManagedThreadId & NumStripedMinusOne;
            cells[stripe].Decrement();
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

        [DebuggerDisplay("{value}")]
        public class Cell
        {
            private long Value;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Increment()
            {
                Interlocked.Increment(ref Value);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Decrement()
            {
                Interlocked.Decrement(ref Value);
            }

            public long Get()
            {
                return Interlocked.Read(ref Value);
            }
        }
    }
}
