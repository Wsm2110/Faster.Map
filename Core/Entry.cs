using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Faster.Map.Core
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    [DebuggerDisplay("hashcode  {Hashcode } - Key {Key} - value {Value} - next {Next}")]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Entry<TKey, TValue>
    {
        /// <summary>
        /// The hashcode
        /// </summary>
        public int Hashcode;

        /// <summary>
        /// Gets or sets the next.
        /// </summary>
        /// <value>
        /// The next.
        /// </value>
        public int Next;

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public TKey Key;

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public TValue Value;

    }
}
