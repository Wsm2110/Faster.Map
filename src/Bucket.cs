using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Faster.Map;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Bucket
{
    #region Fields

    private uint _next;
    private uint _signature;

    //private const uint ValueMask = 0x7FFFFFFF; // Mask for 31-bit values
    //private const uint HomeBucketMask = 1U << 31; // Mask for MSB (bit 31)

    #endregion




    /// <summary>
    /// Gets or sets the signature (secondary part of the hash).
    /// </summary>
    public uint Signature
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _signature;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _signature = value;
    }

    /// <summary>
    /// Gets or sets the Next value (all bits except the last one).
    /// </summary>
    public uint Next
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _next; // Extract only the first 31 bits
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _next = value; // Preserve bit 31, set lower 31 bits
    }

    /// <summary>
    /// Retrieves the original index from the signature and mask.
    /// </summary>
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public readonly uint RetrieveIndex(uint mask)
    //{
    //    return _signature & mask; // Extracts only the lower bits that represent the index
    //}
}
