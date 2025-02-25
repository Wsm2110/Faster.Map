using System.Runtime.CompilerServices;

using System.Runtime.InteropServices;



[StructLayout(LayoutKind.Sequential, Pack = 1)]

public struct Bucket

{

    private const ushort OverflowMask = 0x7FFF;         // 15-bit mask (0b0111_1111_1111_1111)

    private const ushort HomeBucketFlag = 0x8000;   // MSB for HomeBucket (0b1000_0000_0000_0000)



    public ushort Next;   // Secondary collision resolution pointer

    public uint Signature;    // Precomputed hash/signature for ultra-fast lookups



    private ushort _overflow;     // 15-bit next index + 1-bit HomeBucket flag



    /// <summary>

    /// Gets or sets the 15-bit next bucket index (ensures MSB is not modified).

    /// </summary>

    public ushort Overflow

    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        get => (ushort)(_overflow & OverflowMask);



        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        set => _overflow = (ushort)((_overflow & HomeBucketFlag) | value);

    }



    /// <summary>

    /// Checks if this bucket is a HomeBucket.

    /// </summary>

    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    public bool IsHomeBucket() => (_overflow & HomeBucketFlag) != 0;



    /// <summary>

    /// Sets or clears the HomeBucket flag.

    /// </summary>

    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    public void SetHomeBucket()

    {

        _overflow |= HomeBucketFlag;  // Set MSB

    }

}