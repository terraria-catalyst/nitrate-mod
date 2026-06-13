using System;
using System.Runtime.InteropServices;

namespace NotQuiteNitrate.Utilities.Numerics;

[StructLayout(LayoutKind.Explicit)]
internal readonly struct PackedPoint16(byte x, byte y) : IEquatable<PackedPoint16>
{
    [FieldOffset(0)]
    private readonly ushort data;

    [FieldOffset(0)]
    public readonly byte X = x;

    [FieldOffset(4)]
    public readonly byte Y = y;

    public override int GetHashCode()
    {
        return data;
    }

    public bool Equals(PackedPoint16 other)
    {
        return data == other.data;
    }

    public override bool Equals(object? obj)
    {
        return obj is PackedPoint16 other && Equals(other);
    }
}
