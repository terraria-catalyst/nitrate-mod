using System.Runtime.CompilerServices;

namespace Nitrate.Core.Utilities.Simdifier;

internal sealed class Vector2Simdifier : AbstractSimdifier<FnaVector2, SimdVector2>
{
    public Vector2Simdifier()
    {
        ReplaceCall("Microsoft.Xna.Framework.Vector2 Microsoft.Xna.Framework.Vector2::op_Multiply(Microsoft.Xna.Framework.Vector2,System.Single)", nameof(op_Multiply_Vector2_int));
        ReplaceCall("Microsoft.Xna.Framework.Vector2 Microsoft.Xna.Framework.Vector2::op_Multiply(Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Vector2)", nameof(op_Multiply_Vector2_Vector2));
        ReplaceNewobj("void Microsoft.Xna.Framework.Vector2::.ctor(System.Single,System.Single)", nameof(ctor_float_float));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SimdVector2 op_Multiply_Vector2_int(FnaVector2 @this, int value) => As(@this) * value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SimdVector2 op_Multiply_Vector2_Vector2(FnaVector2 @this, FnaVector2 value) => As(@this) * As(value);

    // TODO: Benchmark different approaches:
    // 1:
    // return new SimdVector2(x, y);
    // 2:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SimdVector2 ctor_float_float(float x, float y) => As(new FnaVector2(x, y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SimdVector2 As(FnaVector2 value) => Unsafe.As<FnaVector2, SimdVector2>(ref value);
}