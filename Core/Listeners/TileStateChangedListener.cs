using JetBrains.Annotations;
using Terraria;
using Terraria.ModLoader;

namespace Nitrate.Core.Listeners;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class TileStateChangedListener : ModSystem
{
    public delegate void SingleStateChange(int x, int y);

    // public delegate void RangeStateChange(int fromX, int toX, int fromY, int toY);

    public static event SingleStateChange? OnTileSingleStateChange;

    // public static event RangeStateChange? OnTileRangeStateChange;

    public static event SingleStateChange? OnWallSingleStateChange;

    // public static event RangeStateChange? OnWallRangeStateChange;

    public override void OnModLoad()
    {
        base.OnModLoad();

        On_WorldGen.PlaceTile += PlaceTile;
        On_WorldGen.KillTile += KillTile;
        On_WorldGen.TileFrame += TileFrame;
        On_WorldGen.PlaceWall += PlaceWall;
        On_WorldGen.KillWall += KillWall;
        On_Framing.WallFrame += WallFrame;
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
}