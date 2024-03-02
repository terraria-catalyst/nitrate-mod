using System;
using JetBrains.Annotations;
using Terraria;
using Terraria.ModLoader;

namespace TeamCatalyst.Nitrate.API.Listeners;

/// <summary>
///     Provides event hooks for when the state of dynamically visible tiles
///     change.
/// </summary>
public static class DynamicTileVisibilityListener {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
    private sealed class DynamicTileVisibilityListenerImpl : ModSystem {
        private static bool oldSpelunker;
        private static bool oldDangersense;
        private static bool oldEcho;
        private static bool oldBiomeSight;

        public override void PostUpdateEverything() {
            base.PostUpdateEverything();

            VisibilityType types = 0;

            var echo = Main.LocalPlayer.CanSeeInvisibleBlocks || Main.SceneMetrics.EchoMonolith;
            types |= oldSpelunker   != Main.LocalPlayer.findTreasure ? VisibilityType.Spelunker   : 0;
            types |= oldDangersense != Main.LocalPlayer.dangerSense  ? VisibilityType.Dangersense : 0;
            types |= oldEcho        != echo                          ? VisibilityType.Echo        : 0;
            types |= oldBiomeSight  != Main.LocalPlayer.biomeSight   ? VisibilityType.BiomeSight  : 0;

            oldSpelunker   = Main.LocalPlayer.findTreasure;
            oldDangersense = Main.LocalPlayer.dangerSense;
            oldEcho        = echo;
            oldBiomeSight  = Main.LocalPlayer.biomeSight;

            if (types != 0)
                OnVisibilityChange?.Invoke(types);
        }
    }

    /// <summary>
    ///     Tile visibility types.
    /// </summary>
    [Flags]
    public enum VisibilityType {
        /// <summary>
        ///     Spelunker highlighting.
        /// </summary>
        Spelunker   = 1 << 0,

        /// <summary>
        ///     Dangersense highlighting.
        /// </summary>
        Dangersense = 1 << 1,

        /// <summary>
        ///     Echo visibility toggling.
        /// </summary>
        Echo        = 1 << 2,

        /// <summary>
        ///     Biome sight highlighting.
        /// </summary>
        BiomeSight  = 1 << 3,
    }

    /// <summary>
    ///     Event for when the visibility of tiles changes.
    /// </summary>
    public delegate void VisibilityChange(VisibilityType types);

    /// <summary>
    ///     Event for when the visibility of tiles changes.
    /// </summary>
    public static event VisibilityChange? OnVisibilityChange;
}
