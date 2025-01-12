using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Map
{
    internal class CustomComparer<TKey> : IEqualityComparer<TKey> 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(TKey x, TKey y) 
        {
            return Unsafe.Equals(x, y);  
        }

        public int GetHashCode([DisallowNull] TKey obj)
        {
            throw new NotImplementedException();
        }
    }
}
