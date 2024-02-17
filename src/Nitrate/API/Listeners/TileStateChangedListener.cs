using JetBrains.Annotations;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace Nitrate.API.Listeners;

/// <summary>
///     Provides event hooks for when the state of a tile or wall changes.
/// </summary>
public static class TileStateChangedListener {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
    private sealed class TileStateChangedListenerImpl : ModSystem {
        public override void OnModLoad() {
            base.OnModLoad();

            On_WorldGen.PlaceTile += PlaceTile;
            On_WorldGen.KillTile += KillTile;
            On_WorldGen.TileFrame += TileFrame;
            On_WorldGen.paintTile += PaintTile;
            On_WorldGen.paintCoatTile += CoatTile;

            On_WorldGen.PlaceWall += PlaceWall;
            On_WorldGen.KillWall += KillWall;
            On_Framing.WallFrame += WallFrame;
            On_WorldGen.paintWall += PaintWall;
            On_WorldGen.paintCoatTile += CoatWall;

            On_NetMessage.DecompressTileBlock_Inner += DecompressTileBlock;

            On_WorldGen.ReplaceTIle_DoActualReplacement += DoActualReplacement;

            On_Player.TileInteractionsUse += TileInteractionsUse;
        }

        private static bool PlaceTile(On_WorldGen.orig_PlaceTile orig, int i, int j, int type, bool mute, bool forced, int plr, int style) {
            var result = orig(i, j, type, mute, forced, plr, style);
            OnTileSingleStateChange?.Invoke(i, j);

            return result;
        }

        private static void KillTile(On_WorldGen.orig_KillTile orig, int i, int j, bool fail, bool effectOnly, bool noItem) {
            orig(i, j, fail, effectOnly, noItem);
            OnTileSingleStateChange?.Invoke(i, j);
        }

        private static void TileFrame(On_WorldGen.orig_TileFrame orig, int i, int j, bool resetFrame, bool noBreak) {
            orig(i, j, resetFrame, noBreak);
            OnTileSingleStateChange?.Invoke(i, j);
        }

        private static bool PaintTile(On_WorldGen.orig_paintTile orig, int x, int y, byte color, bool broadcast) {
            var result = orig(x, y, color, broadcast);
            OnTileSingleStateChange?.Invoke(x, y);

            return result;
        }

        private static bool CoatTile(On_WorldGen.orig_paintCoatTile orig, int x, int y, byte paintCoatId, bool broadcast) {
            var result = orig(x, y, paintCoatId, broadcast);
            OnTileSingleStateChange?.Invoke(x, y);

            return result;
        }

        private static void PlaceWall(On_WorldGen.orig_PlaceWall orig, int i, int j, int type, bool mute) {
            orig(i, j, type, mute);
            OnWallSingleStateChange?.Invoke(i, j);
        }

        private static void KillWall(On_WorldGen.orig_KillWall orig, int i, int j, bool fail) {
            orig(i, j, fail);
            OnWallSingleStateChange?.Invoke(i, j);
        }

        private static void WallFrame(On_Framing.orig_WallFrame orig, int i, int j, bool resetFrame) {
            orig(i, j, resetFrame);
            OnWallSingleStateChange?.Invoke(i, j);
        }

        private static bool PaintWall(On_WorldGen.orig_paintWall orig, int x, int y, byte color, bool broadcast) {
            var result = orig(x, y, color, broadcast);
            OnWallSingleStateChange?.Invoke(x, y);

            return result;
        }

        private static bool CoatWall(On_WorldGen.orig_paintCoatTile orig, int x, int y, byte paintCoatId, bool broadcast) {
            var result = orig(x, y, paintCoatId, broadcast);
            OnWallSingleStateChange?.Invoke(x, y);

            return result;
        }

        private static void DecompressTileBlock(On_NetMessage.orig_DecompressTileBlock_Inner orig, BinaryReader reader, int xStart, int yStart, int width, int height) {
            orig(reader, xStart, yStart, width, height);

            for (var i = xStart; i < xStart + width; i++) {
                for (var j = yStart; j < yStart + height; j++) {
                    OnTileSingleStateChange?.Invoke(i, j);
                }
            }
        }

        private static void DoActualReplacement(On_WorldGen.orig_ReplaceTIle_DoActualReplacement orig, ushort targetType, int targetStyle, int topLeftX, int topLeftY, Tile t) {
            orig(targetType, targetStyle, topLeftX, topLeftY, t);
            OnTileSingleStateChange?.Invoke(topLeftX, topLeftY);
        }

        private static void TileInteractionsUse(On_Player.orig_TileInteractionsUse orig, Player self, int myX, int myY) {
            orig(self, myX, myY);
            OnTileSingleStateChange?.Invoke(myX, myY);
        }
    }

    /// <summary>
    ///     A delegate for when the state of a tile or wall changes.
    /// </summary>
    public delegate void SingleStateChange(int x, int y);

    /// <summary>
    ///     An event that is raised when the state of a tile changes.
    /// </summary>
    public static event SingleStateChange? OnTileSingleStateChange;

    /// <summary>
    ///     An event that is raised when the state of a wall changes.
    /// </summary>
    public static event SingleStateChange? OnWallSingleStateChange;
}
