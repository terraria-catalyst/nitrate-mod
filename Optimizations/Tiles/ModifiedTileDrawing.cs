using Terraria;
using Terraria.GameContent.Drawing;

namespace Nitrate.Optimizations.Tiles;

internal static class ModifiedTileDrawing
{
    private static bool IsActiveAndNotPaused => Main.instance.IsActive && !Main.gamePaused;

    /// <summary>
    ///     Renders a single tile.
    /// </summary>
    /// <param name="vanilla">
    ///     Whether drawing should be handled like vanilla (colors for lighting,
    ///     etc.).
    /// </param>
    /// <param name="solid">Whether this is the solid layer.</param>
    public static void DrawSingleTile(bool vanilla, bool solid, int x, int y)
    {
        if (!WorldGen.InWorld(x, y))
        {
            return;
        }
        
        Tile tile = Framing.GetTileSafely(x, y);

        if (!tile.HasTile)
        {
            return;
        }
        
        if (vanilla)
        {
            AddSpecialPointsForTile(tile, x, y);
        }
    }

    private static void AddSpecialPointsForTile(Tile tile, int x, int y)
    {
        ushort type = tile.type;
        short frameX = tile.frameX;
        short frameY = tile.frameY;

        switch (type)
        {
            case 52:
            case 62:
            case 115:
            case 205:
            case 382:
            case 528:
            case 636:
            case 638:
                if (true)
                {
                    Main.instance.TilesRenderer.CrawlToTopOfVineAndAddSpecialPoint(y, x);
                }

                break;

            case 549:
                if (true)
                {
                    Main.instance.TilesRenderer.CrawlToBottomOfReverseVineAndAddSpecialPoint(y, x);
                }

                break;

            case 34:
                if (frameX % 54 == 0 && frameY % 54 == 0)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 454:
                if (frameX % 72 == 0 && frameY % 54 == 0)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 42:
            case 270:
            case 271:
            case 572:
            case 581:
            case 660:
                if (frameX % 18 == 0 && frameY % 36 == 0)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 91:
                if (frameX % 18 == 0 && frameY % 54 == 0)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 95:
            case 126:
            case 444:
                if (frameX % 36 == 0 && frameY % 36 == 0)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 465:
            case 591:
            case 592:
                if (frameX % 36 == 0 && frameY % 54 == 0)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 27:
                if (frameX % 36 == 0 && frameY == 0)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 236:
            case 238:
                if (frameX % 36 == 0 && frameY == 0)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 233:
                switch (frameY)
                {
                    case 0 when frameX % 54 == 0:
                    case 34 when frameX % 36 == 0:
                        Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);

                        break;
                }

                break;

            case 652:
                if (frameX % 36 == 0)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 651:
                if (frameX % 54 == 0)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 530:
                if (frameX < 270)
                {
                    if (frameX % 54 == 0 && frameY == 0)
                    {
                        Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                    }
                }

                break;

            case 485:
            case 489:
            case 490:
                if (frameY == 0 && frameX % 36 == 0)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 521:
            case 522:
            case 523:
            case 524:
            case 525:
            case 526:
            case 527:
                if (frameY == 0 && frameX % 36 == 0)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 493:
                if (frameY == 0 && frameX % 18 == 0)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 519:
                if (frameX / 18 <= 4)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 373:
            case 374:
            case 375:
            case 461:
                Main.instance.TilesRenderer.EmitLiquidDrops(y, x, tile, type);

                break;

            case 491:
                if (frameX == 18 && frameY == 18)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.VoidLens);
                }

                break;

            case 597:
                if (frameX % 54 == 0 && frameY == 0)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.TeleportationPylon);
                }

                break;

            case 617:
                if (frameX % 54 == 0 && frameY % 72 == 0)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MasterTrophy);
                }

                break;

            case 184:
                if (true)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.AnyDirectionalGrass);
                }

                break;

            default:
                if (Main.instance.TilesRenderer.ShouldSwayInWind(x, y, tile))
                {
                    if (true)
                    {
                        Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.WindyGrass);
                    }
                }

                break;
        }
    }

    /// <summary>
    ///     Draws a single wall.
    /// </summary>
    /// <param name="vanilla"></param>
    /// <remarks>
    ///     This is extracted from <see cref="WallDrawing.DrawWalls"/>.
    /// </remarks>
    public static void DrawSingleWall(bool vanilla)
    {
    }
}