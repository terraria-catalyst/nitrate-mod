using MonoMod.Cil;

namespace TeamCatalyst.Nitrate.API.SIMD;

/// <summary>
///     Simdifies a method body.
/// </summary>
public interface ISimdifier {
    /// <summary>
    ///     Simdifies the given method body.
    /// </summary>
    /// <param name="c">The cursor to the method body.</param>
    void Simdify(ILCursor c);
}
