using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Nitrate.API.Threading;
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
    public override void OnModLoad()
    {
        base.OnModLoad();

        IL_Main.DrawRain += ParallelizeRain;
    }

    private static void ParallelizeRain(ILContext il)
    {
        ILCursor c = new(il);

        c.EmitDelegate(() =>
        {
            if (!Main.raining)
            {
                return;
            }

            FasterParallel.For(0, Main.maxRain, (inclusive, exclusive, _) =>
            {
                for (int i = inclusive; i < exclusive; i++)
                {
                    Main.rain[i].Update();
                }
            });
        });

        c.Emit(OpCodes.Ret);
    }
}