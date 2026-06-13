using Terraria;

namespace NotQuiteNitrate.Utilities;

internal static class TilemapHelper
{
    public static (int x, int y) GetTilePosition(Tile tile, Tilemap tilemap)
    {
        var id = tile.TileId;
        return ((int)(id / tilemap.Height), (int)(id % tilemap.Height));
    }
}
