using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Faster.Map.Core
{
    /// <summary>
    /// Used to cache hashcode of string, caching the hashcode will make string lookups alot faster
    /// </summary>
    public struct StringWrapper : IEquatable<StringWrapper>
    {
        int _hashcode = 0;

        public string Value { get; set; }

        public StringWrapper(string s)
        {
            Value = s;
            _hashcode = s.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return _hashcode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(StringWrapper other)
        {
            return string.Equals(Value, other.Value);           
        }
   
        public static implicit operator StringWrapper(string s) => new(s);

        public static explicit operator string(StringWrapper b) => b.Value;

    }
}
