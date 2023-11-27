using System.Runtime.CompilerServices;

namespace Zenith.Core.Utilities;

public static class TypeConversion
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SimdMatrix ToSimd(this FnaMatrix matrix) => Unsafe.As<FnaMatrix, SimdMatrix>(ref matrix);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FnaMatrix ToFna(this SimdMatrix matrix) => Unsafe.As<SimdMatrix, FnaMatrix>(ref matrix);
}