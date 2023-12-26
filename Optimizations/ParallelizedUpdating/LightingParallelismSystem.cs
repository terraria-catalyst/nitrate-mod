using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Nitrate.API.Threading;
using ReLogic.Threading;
using System;
using Terraria;
using Terraria.Graphics.Light;
using Terraria.ModLoader;

namespace Nitrate.Optimizations.ParallelizedUpdating;

/// <summary>
///     Minor optimisation that makes some aspects of lighting use this mod's
///     improved <see cref="FasterParallel"/> over <see cref="FastParallel"/>.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class LightingParallelismSystem : ModSystem
{
    public override void OnModLoad()
    {
        base.OnModLoad();

        IL_LightMap.Blur += FasterBlur;
        IL_TileLightScanner.ExportTo += FasterExportTo;
    }

    private static void FasterBlur(ILContext il)
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

    // Exact copy of the vanilla method but with FasterParallel. Appears to significantly lower time spent waiting.
    private static void SingleParallelBlur(LightMap lightMap)
    {
        FasterParallel.For(0, lightMap.Width, delegate (int start, int end, object _)
        {
            for (int i = start; i < end; i++)
            {
                lightMap.BlurLine(lightMap.IndexOf(i, 0), lightMap.IndexOf(i, lightMap.Height - 1 - lightMap.NonVisiblePadding), 1);
                lightMap.BlurLine(lightMap.IndexOf(i, lightMap.Height - 1), lightMap.IndexOf(i, lightMap.NonVisiblePadding), -1);
            }
        });

        FasterParallel.For(0, lightMap.Height, delegate (int start, int end, object _)
        {
            for (int i = start; i < end; i++)
            {
                lightMap.BlurLine(lightMap.IndexOf(0, i), lightMap.IndexOf(lightMap.Width - 1 - lightMap.NonVisiblePadding, i), lightMap.Height);
                lightMap.BlurLine(lightMap.IndexOf(lightMap.Width - 1, i), lightMap.IndexOf(lightMap.NonVisiblePadding, i), -lightMap.Height);
            }
        });
    }

    private static void FasterExportTo(ILContext il)
    {
        ILCursor c = new(il);

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldarg_1);
        c.Emit(OpCodes.Ldarg_2);
        c.Emit(OpCodes.Ldarg_3);

        c.EmitDelegate<Action<TileLightScanner, Rectangle, LightMap, TileLightScannerOptions>>(
            (self, area, outputMap, options) =>
            {
                self._drawInvisibleWalls = options.DrawInvisibleWalls;

                FasterParallel.For(area.Left, area.Right, delegate (int start, int end, object _)
                {
                    for (int i = start; i < end; i++)
                    {
                        for (int j = area.Top; j <= area.Bottom; j++)
                        {
                            if (self.IsTileNullOrTouchingNull(i, j))
                            {
                                outputMap.SetMaskAt(i - area.X, j - area.Y, LightMaskMode.None);
                                outputMap[i - area.X, j - area.Y] = Vector3.Zero;
                            }
                            else
                            {
                                LightMaskMode tileMask = self.GetTileMask(Main.tile[i, j]);
                                outputMap.SetMaskAt(i - area.X, j - area.Y, tileMask);
                                self.GetTileLight(i, j, out Vector3 outputColor);
                                outputMap[i - area.X, j - area.Y] = outputColor;
                            }
                        }
                    }
                });
            }
        );

        c.Emit(OpCodes.Ret);
    }
}