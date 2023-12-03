using MonoMod.Cil;

namespace Nitrate.Core.Utilities.Simdifier;

internal interface ISimdifier
{
    void Simdify(ILCursor c);
}