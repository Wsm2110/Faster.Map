using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Faster.Map.Concurrent
{
    internal class ThreadLocalCounter
    {
        private ThreadLocal<Cell> threadLocalCell;

        public ThreadLocalCounter()
        {
            threadLocalCell = new ThreadLocal<Cell>(() => new Cell(),true);
        }

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

        private class Cell
        {
            private int value;

            public void Increment()
            {
                Interlocked.Increment(ref value);
            }

            public long Get()
            {
                return value;
            }
        }
    }
}