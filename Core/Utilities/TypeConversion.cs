using System.Runtime.CompilerServices;

namespace Nitrate.Core.Utilities;

/// <summary>
///     Basic type conversion utilities.
/// </summary>
internal static class TypeConversion
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SimdMatrix ToSimd(this FnaMatrix matrix) => Unsafe.As<FnaMatrix, SimdMatrix>(ref matrix);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FnaMatrix ToFna(this SimdMatrix matrix) => Unsafe.As<SimdMatrix, FnaMatrix>(ref matrix);
}