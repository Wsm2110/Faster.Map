using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Faster.Core
{
    [DebuggerDisplay("key {Key} - value {Value}")]
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct GenericEntry<TKey, TValue>
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

        /// <summary>
        /// Gets or sets the hashcode.
        /// </summary>
        /// <value>
        /// The hashcode.
        /// </value>
        public byte Hashcode { get; set; }

    }
}
