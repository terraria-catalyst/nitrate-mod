using JetBrains.Annotations;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace Nitrate.Core.Listeners;

[ApiReleaseCandidate("1.0.0")]
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class TileStateChangedListener : ModSystem
{
    public delegate void SingleStateChange(int x, int y);

    public static event SingleStateChange? OnTileSingleStateChange;

    public static event SingleStateChange? OnWallSingleStateChange;

    public override void OnModLoad()
    {
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
    }

    private static bool PlaceTile(On_WorldGen.orig_PlaceTile orig, int i, int j, int type, bool mute, bool forced, int plr, int style)
    {
        bool result = orig(i, j, type, mute, forced, plr, style);
        OnTileSingleStateChange?.Invoke(i, j);

        return result;
    }

    private static void KillTile(On_WorldGen.orig_KillTile orig, int i, int j, bool fail, bool effectOnly, bool noItem)
    {
        orig(i, j, fail, effectOnly, noItem);
        OnTileSingleStateChange?.Invoke(i, j);
    }

    private static void TileFrame(On_WorldGen.orig_TileFrame orig, int i, int j, bool resetFrame, bool noBreak)
    {
        orig(i, j, resetFrame, noBreak);
        OnTileSingleStateChange?.Invoke(i, j);
    }

    private static bool PaintTile(On_WorldGen.orig_paintTile orig, int x, int y, byte color, bool broadcast)
    {
        bool result = orig(x, y, color, broadcast);
        OnTileSingleStateChange?.Invoke(x, y);

        return result;
    }

    private static bool CoatTile(On_WorldGen.orig_paintCoatTile orig, int x, int y, byte paintCoatId, bool broadcast)
    {
        bool result = orig(x, y, paintCoatId, broadcast);
        OnTileSingleStateChange?.Invoke(x, y);

        return result;
    }

    private static void PlaceWall(On_WorldGen.orig_PlaceWall orig, int i, int j, int type, bool mute)
    {
        orig(i, j, type, mute);
        OnWallSingleStateChange?.Invoke(i, j);
    }

    private static void KillWall(On_WorldGen.orig_KillWall orig, int i, int j, bool fail)
    {
        orig(i, j, fail);
        OnWallSingleStateChange?.Invoke(i, j);
    }

    private static void WallFrame(On_Framing.orig_WallFrame orig, int i, int j, bool resetFrame)
    {
        orig(i, j, resetFrame);
        OnWallSingleStateChange?.Invoke(i, j);
    }

    private static bool PaintWall(On_WorldGen.orig_paintWall orig, int x, int y, byte color, bool broadcast)
    {
        bool result = orig(x, y, color, broadcast);
        OnWallSingleStateChange?.Invoke(x, y);

        return result;
    }

    private static bool CoatWall(On_WorldGen.orig_paintCoatTile orig, int x, int y, byte paintCoatId, bool broadcast)
    {
        bool result = orig(x, y, paintCoatId, broadcast);
        OnWallSingleStateChange?.Invoke(x, y);

        return result;
    }

    private static void DecompressTileBlock(On_NetMessage.orig_DecompressTileBlock_Inner orig, BinaryReader reader, int xStart, int yStart, int width, int height)
    {
        orig(reader, xStart, yStart, width, height);

        for (int i = xStart; i < xStart + width; i++)
        {
            for (int j = yStart; j < yStart + height; j++)
            {
                OnTileSingleStateChange?.Invoke(i, j);
            }
        }
    }
}