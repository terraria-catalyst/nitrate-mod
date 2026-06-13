using System;
using System.Runtime.InteropServices;

namespace NotQuiteNitrate.Utilities.Numerics;

[StructLayout(LayoutKind.Explicit)]
internal readonly struct PackedPoint32(short x, short y) : IEquatable<PackedPoint32>
{
    [FieldOffset(0)]
    private readonly int data;

    [FieldOffset(0)]
    public readonly short X = x;

    [FieldOffset(4)]
    public readonly short Y = y;

    public override int GetHashCode()
    {
        return data;
    }

    public bool Equals(PackedPoint32 other)
    {
        return data == other.data;
    }

    public override bool Equals(object? obj)
    {
        return obj is PackedPoint32 other && Equals(other);
    }
}
