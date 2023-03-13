using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Faster.Map.Core
{
    /// <summary>
    /// Infobyte stores psl
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("psl - {Psl}")]
    public struct Metabyte
    {
        private byte _psl;

        /// <summary>
        /// Gets or sets the PSL.
        /// </summary>
        /// <value>
        /// The PSL.
        /// </value>
        public byte Psl
        {
            get => (byte)(_psl & 0x7F); // 127 // first (0 - 6) 7 bits
            set => _psl = value |= 1 << 7; // set 7th bit in byte, marking 'the not empty bit'
        } // 0 - 255

        /// <summary>
        /// Determines whether this Entry is empty.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl(256)]
        public bool IsEmpty()
        {
            return (_psl & (1 << 7)) == 0;
        }

    }
}