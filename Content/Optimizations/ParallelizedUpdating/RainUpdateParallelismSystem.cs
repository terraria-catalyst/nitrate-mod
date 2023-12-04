using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Nitrate.Core.Features.Threading;
using Terraria;
using Terraria.ModLoader;

namespace Nitrate.Content.Optimizations.ParallelizedUpdating;

/// <summary>
///     Rewrites the rain update method to use parallelism since rain updating
///     (typically) isn't dependent on the states of other rain.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal class RainUpdateParallelismSystem : ModSystem
{
    public override void Load()
    {
        IL_Main.DrawRain += ParalleliseRain;
    }

    private void ParalleliseRain(ILContext il)
    {
        ILCursor c = new(il);

        c.EmitDelegate(() =>
        {
            FasterParallel.For(0, Main.maxRain, (inclusive, exclusive, _) =>
            {
                for (int i = inclusive; i < exclusive; i++)
                {
                    Rain rain = Main.rain[i];

                    rain.Update();
                }
            });
        });

        c.Emit(OpCodes.Ret);
    }
}
