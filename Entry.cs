using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Faster
{
    [DebuggerDisplay("{Psl} - {Value}")]
    [StructLayout(LayoutKind.Auto)]
    public struct Entry<TKey, TValue>
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
            get => (byte)(_psl & 0x1F); // first 5 bits
            set
            {
                var b = SetBit(value); //set 7th bit to indicate this struct is not empty
                _psl = b;
            }
        } // 0 - 255 
        
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public TValue Value { get; set; }

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public TKey Key
        {
            get;
            set;
        }

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
        /// Determines whether this instance is empty.
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
