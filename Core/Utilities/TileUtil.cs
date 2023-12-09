using static Terraria.GameContent.Drawing.TileDrawing;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria;
using Terraria.GameContent.Drawing;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;

namespace Nitrate.Core.Utilities;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal static class TileUtil
{
    public static void RenderTileAt(int i, int j, Vector2 screenPosition)
    {
        TileDrawing tileDrawing = Main.instance.TilesRenderer;

        Tile tile = Main.tile[i, j];

        Vector2 vector = Vector2.Zero;

        TileDrawInfo value = tileDrawing._currentTileDrawInfo.Value;

        bool solidLayer = true;
        int waterStyleOverride = -1;

        if (!tile.active() || tileDrawing.IsTileDrawLayerSolid(tile.type) != solidLayer)
        {
            return;
        }

        if (solidLayer)
        {
            tileDrawing.DrawTile_LiquidBehindTile(solidLayer, inFrontOfPlayers: false, waterStyleOverride, screenPosition, vector, i, j, tile);
        }

        ushort type = tile.type;
        short frameX = tile.frameX;
        short frameY = tile.frameY;

        bool flag = true;

        if (!TextureAssets.Tile[type].IsLoaded)
        {
            Main.instance.LoadTiles(type);
        }

        if (!TileLoader.PreDraw(i, j, type, Main.spriteBatch))
        {
            return;
        }

        switch (type)
        {
            case 52:
            case 62:
            case 115:
            case 205:
            case 382:
            case 528:
            case 636:
            /*case 638:
                if (flag)
                    CrawlToTopOfVineAndAddSpecialPoint(i, j);
                continue;
            case 549:
                if (flag)
                    CrawlToBottomOfReverseVineAndAddSpecialPoint(i, j);
                continue;*/
            case 34:
                if (frameX % 54 == 0 && frameY % 54 == 0 && flag)
                    tileDrawing.AddSpecialPoint(i, j, TileCounterType.MultiTileVine);
                break;
            case 454:
                if (frameX % 72 == 0 && frameY % 54 == 0 && flag)
                    tileDrawing.AddSpecialPoint(i, j, TileCounterType.MultiTileVine);
                break;
            case 42:
            case 270:
            case 271:
            case 572:
            case 581:
            case 660:
                if (frameX % 18 == 0 && frameY % 36 == 0 && flag)
                    tileDrawing.AddSpecialPoint(i, j, TileCounterType.MultiTileVine);
                break;
            case 91:
                if (frameX % 18 == 0 && frameY % 54 == 0 && flag)
                    tileDrawing.AddSpecialPoint(i, j, TileCounterType.MultiTileVine);
                break;
            case 95:
            case 126:
            case 444:
                if (frameX % 36 == 0 && frameY % 36 == 0 && flag)
                    tileDrawing.AddSpecialPoint(i, j, TileCounterType.MultiTileVine);
                break;
            case 465:
            case 591:
            case 592:
                if (frameX % 36 == 0 && frameY % 54 == 0 && flag)
                    tileDrawing.AddSpecialPoint(i, j, TileCounterType.MultiTileVine);
                break;
            case 27:
                if (frameX % 36 == 0 && frameY == 0 && flag)
                    tileDrawing.AddSpecialPoint(i, j, TileCounterType.MultiTileGrass);
                break;
            case 236:
            case 238:
                if (frameX % 36 == 0 && frameY == 0 && flag)
                    tileDrawing.AddSpecialPoint(i, j, TileCounterType.MultiTileGrass);
                break;
            case 233:
                if (frameY == 0 && frameX % 54 == 0 && flag)
                    tileDrawing.AddSpecialPoint(i, j, TileCounterType.MultiTileGrass);
                if (frameY == 34 && frameX % 36 == 0 && flag)
                    tileDrawing.AddSpecialPoint(i, j, TileCounterType.MultiTileGrass);
                break;
            case 652:
                if (frameX % 36 == 0 && flag)
                    tileDrawing.AddSpecialPoint(i, j, TileCounterType.MultiTileGrass);
                break;
            case 651:
                if (frameX % 54 == 0 && flag)
                    tileDrawing.AddSpecialPoint(i, j, TileCounterType.MultiTileGrass);
                break;
            case 530:
                if (frameX < 270)
                {
                    if (frameX % 54 == 0 && frameY == 0 && flag)
                        tileDrawing.AddSpecialPoint(i, j, TileCounterType.MultiTileGrass);

                    break;
                }
                break;
            case 485:
            case 489:
            case 490:
                if (frameY == 0 && frameX % 36 == 0 && flag)
                    tileDrawing.AddSpecialPoint(i, j, TileCounterType.MultiTileGrass);
                break;
            case 521:
            case 522:
            case 523:
            case 524:
            case 525:
            case 526:
            case 527:
                if (frameY == 0 && frameX % 36 == 0 && flag)
                    tileDrawing.AddSpecialPoint(i, j, TileCounterType.MultiTileGrass);
                break;
            case 493:
                if (frameY == 0 && frameX % 18 == 0 && flag)
                    tileDrawing.AddSpecialPoint(i, j, TileCounterType.MultiTileGrass);
                break;
            case 519:
                if (frameX / 18 <= 4 && flag)
                    tileDrawing.AddSpecialPoint(i, j, TileCounterType.MultiTileGrass);
                break;
            case 373:
            case 374:
            case 375:
            case 461:
                tileDrawing.EmitLiquidDrops(i, j, tile, type);
                break;
            case 491:
                if (flag && frameX == 18 && frameY == 18)
                    tileDrawing.AddSpecialPoint(i, j, TileCounterType.VoidLens);
                break;
            case 597:
                if (flag && frameX % 54 == 0 && frameY == 0)
                    tileDrawing.AddSpecialPoint(i, j, TileCounterType.TeleportationPylon);
                break;
            case 617:
                if (flag && frameX % 54 == 0 && frameY % 72 == 0)
                    tileDrawing.AddSpecialPoint(i, j, TileCounterType.MasterTrophy);
                break;
            case 184:
                if (flag)
                    tileDrawing.AddSpecialPoint(i, j, TileCounterType.AnyDirectionalGrass);
                break;
            default:
                if (tileDrawing.ShouldSwayInWind(i, j, tile))
                {
                    if (flag)
                        tileDrawing.AddSpecialPoint(i, j, TileCounterType.WindyGrass);

                    break;
                }
                break;
        }

        tileDrawing.DrawSingleTile(value, solidLayer, waterStyleOverride, screenPosition, vector, i, j);
    }
}
