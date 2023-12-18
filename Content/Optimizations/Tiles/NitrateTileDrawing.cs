using Terraria;
using Terraria.GameContent.Drawing;

namespace Nitrate.Content.Optimizations.Tiles;

/// <summary>
///     Reimplementation of <see cref="TileDrawing"/>.
/// </summary>
internal sealed class NitrateTileDrawing
{
    private static TileDrawing TileDrawing => Main.instance.TilesRenderer;
}