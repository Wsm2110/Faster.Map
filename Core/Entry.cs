using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Faster.Map.Core
{
    [DebuggerDisplay("Key {Key} - value {Value}")]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Entry<TKey, TValue>
    {
        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public TKey Key { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public TValue Value { get; set; }
    }
}
