using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Nitrate.Core.Features.Threading;
using System;
using Terraria.Graphics.Light;
using Terraria.ModLoader;

namespace Nitrate.Content.Optimizations.ParallelizedUpdating;

/// <summary>
///     Minor optimisation that makes some aspects of lighting use this mod's improved FasterParallel over FastParallel.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class LightingParallelismSystem : ModSystem
{
    public override void OnModLoad()
    {
        base.OnModLoad();

        IL_LightMap.Blur += FasterBlur;
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
        FasterParallel.For(0, lightMap.Width, delegate (int start, int end, object context) {
            for (int j = start; j < end; j++)
            {
                lightMap.BlurLine(lightMap.IndexOf(j, 0), lightMap.IndexOf(j, lightMap.Height - 1 - lightMap.NonVisiblePadding), 1);
                lightMap.BlurLine(lightMap.IndexOf(j, lightMap.Height - 1), lightMap.IndexOf(j, lightMap.NonVisiblePadding), -1);
            }
        });

        FasterParallel.For(0, lightMap.Height, delegate (int start, int end, object context) {
            for (int i = start; i < end; i++)
            {
                lightMap.BlurLine(lightMap.IndexOf(0, i), lightMap.IndexOf(lightMap.Width - 1 - lightMap.NonVisiblePadding, i), lightMap.Height);
                lightMap.BlurLine(lightMap.IndexOf(lightMap.Width - 1, i), lightMap.IndexOf(lightMap.NonVisiblePadding, i), -lightMap.Height);
            }
        });
    }
}
