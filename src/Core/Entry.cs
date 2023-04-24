using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Faster.Map.Core
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    [DebuggerDisplay("hashcode  {Key.GetHashCode() }")]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Entry<TKey, TValue>
    {
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