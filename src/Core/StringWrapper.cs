using System;
using System.Collections.Generic;
using System.Linq;
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

        public override int GetHashCode()
        {
            return _hashcode;
        }

        public bool Equals(StringWrapper other)
        {
            return Value.Equals(other.Value);
        }
        public static explicit operator StringWrapper(string s) => new(s);
        public static implicit operator string(StringWrapper b) => b.Value;

    }
}
