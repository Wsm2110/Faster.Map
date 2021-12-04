using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Faster
{
    [StructLayout(LayoutKind.Explicit)]
    [DebuggerDisplay("offset  {Offset} - count {Count}")]
    public struct InfoByte
    {
        [FieldOffset(0)]
        private byte _offset;

        [FieldOffset(1)]
        private byte _count;

        public byte Offset
        {
            get => _offset;
            set => _offset = value;
        }

        public byte Count
        {
            get => _count;
            set => _count = value;
        }
    }
}
