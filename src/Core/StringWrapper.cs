using System;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Faster.Map.Core
{

    public record struct StringWrapper: IEquatable<StringWrapper>
    {
        int hashcode;
        public string Unwrapped { get; set; }

        public StringWrapper(string unwrapped)
        {
            Unwrapped = unwrapped;

            var span = unwrapped.AsSpan();
            var result = XxHash3.HashToUInt64(MemoryMarshal.AsBytes(span)) >> 32;
            hashcode = Unsafe.As<ulong, int>(ref result);         
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return hashcode;
        }

        public static implicit operator string(StringWrapper source)
        {
            return source.Unwrapped;
        }
                
        public static implicit operator StringWrapper(string source)
        {
            return new StringWrapper(source);
        }

    }
}
