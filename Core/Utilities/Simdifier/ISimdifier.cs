using MonoMod.Cil;

namespace Nitrate.Core.Utilities.Simdifier;

/// <summary>
///     Simdifies a method body.
/// </summary>
// Needs to be publicized once a proper system is in place for registering new Simdifiers.
[ApiReleaseCandidate("1.0.0")]
internal interface ISimdifier
{
    /// <summary>
    ///     Simdifies the given method body.
    /// </summary>
    /// <param name="c">The cursor to the method body.</param>
    void Simdify(ILCursor c);
}