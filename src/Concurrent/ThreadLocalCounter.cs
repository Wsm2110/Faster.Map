using System;
using System.Collections.Generic;
using System.Threading;

namespace Faster.Map.Concurrent
{
    // The ThreadLocalCounter class leverages ThreadLocal<T> to keep a separate counter for each thread, thereby avoiding contention.Since each thread has its own instance of the counter, the need for atomic operations like Interlocked.Increment and Interlocked.Decrement is eliminated.
    // In a ThreadLocal context, each thread accesses its own isolated instance of the counter, which ensures that no other thread will interfere with its operations.As a result, the increment and decrement operations can safely be performed without the need for Interlocked operations.
    internal class ThreadLocalCounter
    {
        private ThreadLocal<Cell> threadLocalCell;


        public ThreadLocalCounter()
        {
            threadLocalCell = new ThreadLocal<Cell>(() => new Cell(), true);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Increment()
        {
            threadLocalCell.Value.Increment();
        }

        public long Sum()
        {
            long sum = 0;
            var uniqueCells = new HashSet<Cell>(threadLocalCell.Values);
            foreach (var cell in uniqueCells)
            {
                sum += cell.Get();
            }
            return sum;
        }

        internal void Decrement()
        {
            threadLocalCell.Value.Decrement();
        }

        private class Cell
        {
            private int value;

            public void Increment()
            {
                ++value;
            }

            public void Decrement()
            {
                --value;
            }

            public long Get()
            {
                return value;
            }
        }
    }
}