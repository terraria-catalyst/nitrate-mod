using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Nitrate.Core.Features.Threading;
using System;
using Terraria.Graphics.Light;
using Terraria.ModLoader;

namespace Nitrate.Content.Optimizations.ParallelizedUpdating;

/// <summary>
///     Rewrites various Lighting methods to use better parallelism.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class LightingParallelismFixes : ModSystem
{
    public override void OnModLoad()
    {
        base.OnModLoad();

        //IL_LightMap.Blur += FasterBlur;
    }

    private void FasterBlur(ILContext il)
    {
        ILCursor c = new(il);

        // Load LightMap instance.
        c.Emit(OpCodes.Ldarg_0);

        // TODO: This delegate replicates vanilla behaviour by blurring twice - maybe add option to remove one blur pass to improve performance?
        c.EmitDelegate<Action<LightMap>>(lightMap =>
        {
            SingleParallelBlur(lightMap);
            SingleParallelBlur(lightMap);

            lightMap._random.NextSeed();
        });

        c.Emit(OpCodes.Ret);
    }

    private void SingleParallelBlur(LightMap lightMap)
    {
        /*int length = lightMap.Width * lightMap.Height;

        FasterParallel.For(0, length, (inclusive, exclusive, _) =>
        {
            for (int i = inclusive; i < exclusive; i++)
            {
                
            }
        });*/
    }
}
