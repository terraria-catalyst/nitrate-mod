using JetBrains.Annotations;
using MonoMod.Cil;
using Nitrate.Core.Utilities;
using Terraria.ModLoader;

namespace Nitrate.Content.Optimizations.MiscSimd;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class MiscSimdApplicationSystem : ModSystem
{
    public override void Load()
    {
        base.Load();

        // TODO: Add stuff here?!?!
    }

    private static void ApplySimdification(ILContext il)
    {
        ILCursor c = new(il);
        c.Simdify();
    }
}