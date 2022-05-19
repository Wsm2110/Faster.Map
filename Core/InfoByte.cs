using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Faster.Map.Core
{
    /// <summary>
    /// Infobyte stores psl
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [DebuggerDisplay("psl - {Psl}")]
    public struct InfoByte
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
            set
            {
                var b = SetBit(value); //set 7th bit to indicate this struct is not empty
                _psl = b;
            }
        } // 0 - 255

        /// <summary>
        /// Sets the bit.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <returns></returns>
        public static byte SetBit(byte b)
        {
            b |= 1 << 7;
            return b;
        }

        /// <summary>
        /// Determines whether this Entry is empty.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
        /// </returns>
        public bool IsEmpty()
        {
            return (_psl & (1 << 7)) == 0;
        }

    }
}
