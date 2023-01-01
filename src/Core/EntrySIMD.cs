using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Faster.Map.Core
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    [DebuggerDisplay("key  {Key}")]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct EntrySIMD<TKey, TValue>
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
