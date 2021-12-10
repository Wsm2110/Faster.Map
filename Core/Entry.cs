using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Faster.Core
{
    [DebuggerDisplay("Key {Key} - value {Value}")]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Entry<TValue>
    {
        public int Key { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public TValue Value { get; set; }
    }
}
