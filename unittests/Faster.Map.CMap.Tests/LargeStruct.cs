using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Map.CMap.Tests
{
    public struct LargeStruct : IEquatable<LargeStruct>
    {
        public long Part1;
        public long Part2;
        public long Part3;
        public long Part4;

        public override bool Equals(object obj)
        {
            return obj is LargeStruct other && Equals(other);
        }

        public bool Equals(LargeStruct other)
        {
            return Part1 == other.Part1 &&
                   Part2 == other.Part2 &&
                   Part3 == other.Part3 &&
                   Part4 == other.Part4;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Part1, Part2, Part3, Part4);
        }
    }
}
