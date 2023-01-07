using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
