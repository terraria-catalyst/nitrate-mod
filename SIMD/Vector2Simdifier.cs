using System.Runtime.CompilerServices;

namespace Nitrate.SIMD;

internal sealed class Vector2Simdifier : AbstractSimdifier {
    public Vector2Simdifier() {
        ReplaceCall("Microsoft.Xna.Framework.Vector2 Microsoft.Xna.Framework.Vector2::op_Multiply(Microsoft.Xna.Framework.Vector2,System.Single)", nameof(op_Multiply_Vector2_int));
        ReplaceCall("Microsoft.Xna.Framework.Vector2 Microsoft.Xna.Framework.Vector2::op_Multiply(Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Vector2)", nameof(op_Multiply_Vector2_Vector2));
        ReplaceCall("Microsoft.Xna.Framework.Vector2 Microsoft.Xna.Framework.Vector2::op_Addition(Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Vector2)", nameof(op_Addition_Vector2_Vector2));
        ReplaceCall("Microsoft.Xna.Framework.Vector2 Microsoft.Xna.Framework.Vector2::op_Subtraction(Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Vector2)", nameof(op_Subtraction_Vector2_Vector2));
        ReplaceCall("Microsoft.Xna.Framework.Vector2 Microsoft.Xna.Framework.Vector2::op_Division(Microsoft.Xna.Framework.Vector2,System.Single)", nameof(op_Division_Vector2_float));
        ReplaceCall("Microsoft.Xna.Framework.Vector2 Microsoft.Xna.Framework.Vector2::op_Division(Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Vector2)", nameof(op_Division_Vector2_Vector2));
        ReplaceCall("System.Boolean Microsoft.Xna.Framework.Vector2::op_Inequality(Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Vector2)", nameof(op_Inequality_Vector2_Vector2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FnaVector2 op_Multiply_Vector2_int(FnaVector2 @this, float value) {
        return As(As(@this) * value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FnaVector2 op_Multiply_Vector2_Vector2(FnaVector2 @this, FnaVector2 value) {
        return As(As(@this) * As(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FnaVector2 op_Addition_Vector2_Vector2(FnaVector2 @this, FnaVector2 value) {
        return As(As(@this) + As(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FnaVector2 op_Subtraction_Vector2_Vector2(FnaVector2 @this, FnaVector2 value) {
        return As(As(@this) - As(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FnaVector2 op_Division_Vector2_float(FnaVector2 @this, float value) {
        return As(As(@this) / value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FnaVector2 op_Division_Vector2_Vector2(FnaVector2 @this, FnaVector2 value) {
        return As(As(@this) / As(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool op_Inequality_Vector2_Vector2(FnaVector2 @this, FnaVector2 value) {
        return As(@this) != As(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SimdVector2 As(FnaVector2 value) {
        return Unsafe.As<FnaVector2, SimdVector2>(ref value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FnaVector2 As(SimdVector2 value) {
        return Unsafe.As<SimdVector2, FnaVector2>(ref value);
    }
}
