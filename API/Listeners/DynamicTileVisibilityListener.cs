using JetBrains.Annotations;
using System;
using Terraria;
using Terraria.ModLoader;

namespace Nitrate.API.Listeners;

/// <summary>
///     Provides event hooks for when the state of dynamically visible tiles
///     change.
/// </summary>
public static class DynamicTileVisibilityListener
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
    private sealed class DynamicTileVisibilityListenerImpl : ModSystem
    {
        private static bool oldSpelunker;
        private static bool oldDangersense;
        private static bool oldEcho;
        private static bool oldBiomeSight;

        public override void PostUpdateEverything()
        {
            base.PostUpdateEverything();

            VisibilityType types = 0;

            types |= oldSpelunker != Main.LocalPlayer.findTreasure ? VisibilityType.Spelunker : 0;
            types |= oldDangersense != Main.LocalPlayer.dangerSense ? VisibilityType.Dangersense : 0;
            types |= oldEcho != (Main.LocalPlayer.CanSeeInvisibleBlocks || Main.SceneMetrics.EchoMonolith) ? VisibilityType.Echo : 0;
            types |= oldBiomeSight != Main.LocalPlayer.biomeSight ? VisibilityType.BiomeSight : 0;

            oldSpelunker = Main.LocalPlayer.findTreasure;
            oldDangersense = Main.LocalPlayer.dangerSense;
            oldEcho = Main.LocalPlayer.CanSeeInvisibleBlocks || Main.SceneMetrics.EchoMonolith;
            oldBiomeSight = Main.LocalPlayer.biomeSight;

            if (types != 0)
            {
                OnVisibilityChange?.Invoke(types);
            }
        }
    }

    [Flags]
    public enum VisibilityType
    {
        Spelunker   = 0b0001,
        Dangersense = 0b0010,
        Echo        = 0b0100,
        BiomeSight  = 0b1000,
    }

    public delegate void VisibilityChange(VisibilityType types);

    public static event VisibilityChange? OnVisibilityChange;
}