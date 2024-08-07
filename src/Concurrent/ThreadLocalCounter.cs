using System;
using System.Collections.Generic;
using System.Linq;
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

        public int Sum()
        {
            return threadLocalCell.Values.Sum(i => i._value);
        }

        internal void Decrement()
        {
            threadLocalCell.Value.Decrement();
        }

        internal class Cell
        {
            internal int _value;

            public void Increment()
            {
                // Interlocked operations provide a full memory fence, meaning they ensure all preceding memory writes are completed and visible to other threads before the Interlocked operation completes.
                // This means that when you perform an Interlocked operation, it guarantees that any changes made to other variables(not just the variable involved in the Interlocked operation) are also visible to other threads.
                // Note this also means we dont need any explicit memorybarriers.
                // This code, using Interlocked operations, will also work correctly on ARM architectures without needing additional explicit memory barriers.The memory ordering and visibility are managed by the Interlocked methods.

                Interlocked.Increment(ref _value);
            }

            public void Decrement()
            {
                // Interlocked operations provide a full memory fence, meaning they ensure all preceding memory writes are completed and visible to other threads before the Interlocked operation completes.
                // This means that when you perform an Interlocked operation, it guarantees that any changes made to other variables(not just the variable involved in the Interlocked operation) are also visible to other threads.
                // Note this also means we dont need any explicit memorybarriers.
                // This code, using Interlocked operations, will also work correctly on ARM architectures without needing additional explicit memory barriers.The memory ordering and visibility are managed by the Interlocked methods.

                Interlocked.Decrement(ref _value);
            }

            public long Get()
            {
                return _value;
            }
        }
    }
}