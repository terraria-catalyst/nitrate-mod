using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.Liquid;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

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
    /// <param name="x">Tile x-coordinate.</param>
    /// <param name="y">Tile y-coordinate.</param>
    /// <param name="screenPosition">The screen position.</param>
    public static void DrawSingleTile(bool vanilla, bool solid, int x, int y, FnaVector2 screenPosition)
    {
        int j = x;
        int i = y;

        if (!WorldGen.InWorld(x, y))
        {
            return;
        }

        Tile tile = Framing.GetTileSafely(x, y);
        ushort type = tile.type;
        short frameX = tile.frameX;
        short frameY = tile.frameY;

        if (!tile.HasTile)
        {
            return;
        }

        if (!TextureAssets.Tile[type].IsLoaded)
        {
            Main.instance.LoadTiles(type);
        }

        if (vanilla)
        {
            if (TileLoader.PreDraw(j, i, type, Main.spriteBatch))
            {
                AddSpecialPointsForTile(tile, x, y);
                DrawSingleTile_Vanilla(solid, x, y, screenPosition, Vector2.Zero, tile, type);
            }

            TileLoader.PostDraw(j, i, type, Main.spriteBatch);
        }
        else
        {
            DrawSingleTile_Nitrate(solid, x, y, screenPosition, tile, type, frameX, frameY);
        }
    }

    private static void DrawSingleTile_Nitrate(bool solid, int x, int y, FnaVector2 screenPosition, Tile tile, ushort type, short frameX, short frameY)
    {
    }

    private static void DrawSingleTile_Vanilla(bool solid, int x, int y, FnaVector2 screenPosition, FnaVector2 screenOffset, Tile tile, ushort type)
    {
        TileDrawInfo drawData = Main.instance.TilesRenderer._currentTileDrawInfo.Value!;
        drawData.tileCache = tile;
        drawData.typeCache = type;
        drawData.tileFrameX = tile.frameX;
        drawData.tileFrameY = tile.frameY;
        drawData.tileLight = Lighting.GetColor(x, y);

        if (tile is { liquid: > 0, type: 518 })
        {
            return;
        }

        Main.instance.TilesRenderer.GetTileDrawData(x, y, tile, type, ref drawData.tileFrameX, ref drawData.tileFrameY, out drawData.tileWidth, out drawData.tileHeight, out drawData.tileTop, out drawData.halfBrickHeight, out drawData.addFrX, out drawData.addFrY, out drawData.tileSpriteEffect,
            out drawData.glowTexture, out drawData.glowSourceRect, out drawData.glowColor);

        drawData.drawTexture = Main.instance.TilesRenderer.GetTileDrawTexture(drawData.tileCache, x, y);
        Texture2D highlightTexture = null;
        Rectangle empty = Rectangle.Empty;
        Color highlightColor = Color.Transparent;

        if (TileID.Sets.HasOutlines[drawData.typeCache])
        {
            Main.instance.TilesRenderer.GetTileOutlineInfo(x, y, drawData.typeCache, ref drawData.tileLight, ref highlightTexture, ref highlightColor);
        }

        if (Main.LocalPlayer.dangerSense && TileDrawing.IsTileDangerous(x, y, Main.LocalPlayer, drawData.tileCache, drawData.typeCache))
        {
            if (drawData.tileLight.R < byte.MaxValue)
            {
                drawData.tileLight.R = byte.MaxValue;
            }

            if (drawData.tileLight.G < 50)
            {
                drawData.tileLight.G = 50;
            }

            if (drawData.tileLight.B < 50)
            {
                drawData.tileLight.B = 50;
            }

            if (IsActiveAndNotPaused && Main.instance.TilesRenderer._rand.NextBool(30))
            {
                Dust dust = Dust.NewDustDirect(new Vector2(x * 16, y * 16), 16, 16, DustID.RedTorch, 0f, 0f, 100, default, 0.3f);
                dust.fadeIn = 1f;
                dust.velocity *= 0.1f;
                dust.noLight = true;
                dust.noGravity = true;
            }
        }

        if (Main.LocalPlayer.findTreasure && Main.IsTileSpelunkable(x, y, drawData.typeCache, drawData.tileFrameX, drawData.tileFrameY))
        {
            if (drawData.tileLight.R < 200)
            {
                drawData.tileLight.R = 200;
            }

            if (drawData.tileLight.G < 170)
            {
                drawData.tileLight.G = 170;
            }

            if (IsActiveAndNotPaused && Main.instance.TilesRenderer._rand.NextBool(60))
            {
                Dust dust = Dust.NewDustDirect(new Vector2(x * 16, y * 16), 16, 16, DustID.TreasureSparkle, 0f, 0f, 150, default, 0.3f);
                dust.fadeIn = 1f;
                dust.velocity *= 0.1f;
                dust.noLight = true;
            }
        }

        if (Main.LocalPlayer.biomeSight)
        {
            Color sightColor = Color.White;

            if (Main.IsTileBiomeSightable(x, y, drawData.typeCache, drawData.tileFrameX, drawData.tileFrameY, ref sightColor))
            {
                if (drawData.tileLight.R < sightColor.R)
                {
                    drawData.tileLight.R = sightColor.R;
                }

                if (drawData.tileLight.G < sightColor.G)
                {
                    drawData.tileLight.G = sightColor.G;
                }

                if (drawData.tileLight.B < sightColor.B)
                {
                    drawData.tileLight.B = sightColor.B;
                }

                if (IsActiveAndNotPaused && Main.instance.TilesRenderer._rand.NextBool(480))
                {
                    Dust dust = Dust.NewDustDirect(new Vector2(x * 16, y * 16), 16, 16, DustID.RainbowMk2, 0f, 0f, 150, sightColor, 1.5f);
                    dust.noGravity = true;
                    dust.fadeIn = 1f;
                    dust.velocity *= 0.1f;
                    dust.noLightEmittence = true;
                }
            }
        }

        if (IsActiveAndNotPaused)
        {
            if (!Lighting.UpdateEveryFrame || new FastRandom(Main.TileFrameSeed).WithModifier(x, y).Next(4) == 0)
            {
                Main.instance.TilesRenderer.DrawTiles_EmitParticles(x, y, drawData.tileCache, drawData.typeCache, drawData.tileFrameX, drawData.tileFrameY, drawData.tileLight);
            }

            drawData.tileLight = Main.instance.TilesRenderer.DrawTiles_GetLightOverride(x, y, drawData.tileCache, drawData.typeCache, drawData.tileFrameX, drawData.tileFrameY, drawData.tileLight);
        }

        bool flag = drawData.tileLight.R >= 1 || drawData.tileLight.G >= 1 || drawData.tileLight.B >= 1 || drawData.tileCache.wall > 0 && (drawData.tileCache.wall == 318 || drawData.tileCache.fullbrightWall());
        flag &= TileDrawing.IsVisible(drawData.tileCache);

        Main.instance.TilesRenderer.CacheSpecialDraws_Part1(x, y, drawData.typeCache, drawData.tileFrameX, drawData.tileFrameY, !flag);
        Main.instance.TilesRenderer.CacheSpecialDraws_Part2(x, y, drawData, !flag);

        if (drawData is { typeCache: 72, tileFrameX: >= 36 })
        {
            int num15 = drawData.tileFrameY switch
            {
                18 => 1,
                36 => 2,
                _ => 0,
            };

            Main.spriteBatch.Draw(TextureAssets.ShroomCap.Value, new Vector2(x * 16 - (int)screenPosition.X - 22, y * 16 - (int)screenPosition.Y - 26) + screenOffset, new Rectangle(num15 * 62, 0, 60, 42), Lighting.GetColor(x, y), 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
        }

        Rectangle rectangle = new(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY, drawData.tileWidth, drawData.tileHeight - drawData.halfBrickHeight);
        Vector2 vector = new Vector2(x * 16 - (int)screenPosition.X - (drawData.tileWidth - 16f) / 2f, y * 16 - (int)screenPosition.Y + drawData.tileTop + drawData.halfBrickHeight) + screenOffset;
        TileLoader.DrawEffects(x, y, drawData.typeCache, Main.spriteBatch, ref drawData);

        if (!flag)
        {
            return;
        }

        drawData.colorTint = Color.White;
        drawData.finalColor = TileDrawing.GetFinalLight(drawData.tileCache, drawData.typeCache, drawData.tileLight, drawData.colorTint);

        switch (drawData.typeCache)
        {
            case 136:
                switch (drawData.tileFrameX / 18)
                {
                    case 1:
                        vector.X += -2f;

                        break;

                    case 2:
                        vector.X += 2f;

                        break;
                }

                break;

            case 442:
                if (drawData.tileFrameX / 22 == 3)
                {
                    vector.X += 2f;
                }

                break;

            case 51:
                drawData.finalColor = drawData.tileLight * 0.5f;

                break;

            case 160:
            case 692: {
                Color color = new(Main.DiscoR, Main.DiscoG, Main.DiscoB, 255);

                if (drawData.tileCache.inActive())
                {
                    color = drawData.tileCache.actColor(color);
                }

                drawData.finalColor = color;

                break;
            }

            case 129: {
                drawData.finalColor = new Color(255, 255, 255, 100);
                const int num17 = 2;

                if (drawData.tileFrameX >= 324)
                {
                    drawData.finalColor = Color.Transparent;
                }

                if (drawData.tileFrameY < 36)
                {
                    vector.Y += num17 * (drawData.tileFrameY == 0).ToDirectionInt();
                }
                else
                {
                    vector.X += num17 * (drawData.tileFrameY == 36).ToDirectionInt();
                }

                break;
            }

            case 272: {
                int num16 = Main.tileFrame[drawData.typeCache];
                num16 += x % 2;
                num16 += y % 2;
                num16 += x % 3;
                num16 += y % 3;
                num16 %= 2;
                num16 *= 90;
                drawData.addFrY += num16;
                rectangle.Y += num16;

                break;
            }

            case 80: {
                WorldGen.GetCactusType(x, y, drawData.tileFrameX, drawData.tileFrameY, out bool evil, out bool good, out bool crimson);

                if (evil)
                {
                    rectangle.Y += 54;
                }

                if (good)
                {
                    rectangle.Y += 108;
                }

                if (crimson)
                {
                    rectangle.Y += 162;
                }

                break;
            }

            case 83:
                drawData.drawTexture = Main.instance.TilesRenderer.GetTileDrawTexture(drawData.tileCache, x, y);

                break;

            case 323:
                if (drawData.tileCache.frameX is <= 132 and >= 88)
                {
                    return;
                }

                vector.X += drawData.tileCache.frameY;

                break;

            case 114:
                if (drawData.tileFrameY > 0)
                {
                    rectangle.Height += 2;
                }

                break;
        }

        if (drawData.typeCache == 314)
        {
            Main.instance.TilesRenderer.DrawTile_MinecartTrack(screenPosition, screenOffset, x, y, drawData);
        }
        else if (drawData.typeCache == 171)
        {
            Main.instance.TilesRenderer.DrawXmasTree(screenPosition, screenOffset, x, y, drawData);
        }
        else
        {
            Main.instance.TilesRenderer.DrawBasicTile(screenPosition, screenOffset, x, y, drawData, rectangle, vector);
        }

        if (Main.tileGlowMask[drawData.tileCache.type] != -1)
        {
            short num18 = Main.tileGlowMask[drawData.tileCache.type];

            if (TextureAssets.GlowMask.IndexInRange(num18))
            {
                drawData.drawTexture = TextureAssets.GlowMask[num18].Value;
            }

            double num19 = Main.timeForVisualEffects * 0.08;
            Color color2 = Color.White;
            bool flag2 = false;

            switch (drawData.tileCache.type)
            {
                case 633:
                    color2 = Color.Lerp(Color.White, drawData.finalColor, 0.75f);

                    break;

                case 659:
                case 667:
                    color2 = LiquidRenderer.GetShimmerGlitterColor(top: true, x, y);

                    break;

                case 350:
                    color2 = new Color(new Vector4((float)((0.0 - Math.Cos((int)(num19 / 6.283) % 3 == 1 ? num19 : 0.0)) * 0.2 + 0.2)));

                    break;

                case 381:
                case 517:
                case 687:
                    color2 = Main.instance.TilesRenderer._lavaMossGlow;

                    break;

                case 534:
                case 535:
                case 689:
                    color2 = Main.instance.TilesRenderer._kryptonMossGlow;

                    break;

                case 536:
                case 537:
                case 690:
                    color2 = Main.instance.TilesRenderer._xenonMossGlow;

                    break;

                case 539:
                case 540:
                case 688:
                    color2 = Main.instance.TilesRenderer._argonMossGlow;

                    break;

                case 625:
                case 626:
                case 691:
                    color2 = Main.instance.TilesRenderer._violetMossGlow;

                    break;

                case 627:
                case 628:
                case 692:
                    color2 = new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB);

                    break;

                case 370:
                case 390:
                    color2 = Main.instance.TilesRenderer._meteorGlow;

                    break;

                case 391:
                    color2 = new Color(250, 250, 250, 200);

                    break;

                case 209:
                    color2 = PortalHelper.GetPortalColor(Main.myPlayer, drawData.tileCache.frameX >= 288 ? 1 : 0);

                    break;

                case 429:
                case 445:
                    drawData.drawTexture = Main.instance.TilesRenderer.GetTileDrawTexture(drawData.tileCache, x, y);
                    drawData.addFrY = 18;

                    break;

                case 129: {
                    if (drawData.tileFrameX < 324)
                    {
                        flag2 = true;

                        break;
                    }

                    drawData.drawTexture = Main.instance.TilesRenderer.GetTileDrawTexture(drawData.tileCache, x, y);
                    color2 = Main.hslToRgb(0.7f + (float)Math.Sin((float)Math.PI * 2f * Main.GlobalTimeWrappedHourly * 0.16f + x * 0.3f + y * 0.7f) * 0.16f, 1f, 0.5f);
                    color2.A /= 2;
                    color2 *= 0.3f;
                    int num2 = 72;

                    for (float num3 = 0f; num3 < (float)Math.PI * 2f; num3 += (float)Math.PI / 2f)
                    {
                        Main.spriteBatch.Draw(drawData.drawTexture, vector + num3.ToRotationVector2() * 2f, new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY + num2, drawData.tileWidth, drawData.tileHeight), color2, 0f, Vector2.Zero, 1f, SpriteEffects.None,
                            0f);
                    }

                    color2 = new Color(255, 255, 255, 100);

                    break;
                }
            }

            if (!flag2)
            {
                if (drawData.tileCache.slope() == 0 && !drawData.tileCache.halfBrick())
                {
                    Main.spriteBatch.Draw(drawData.drawTexture, vector, new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY, drawData.tileWidth, drawData.tileHeight), color2, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                }
                else if (drawData.tileCache.halfBrick())
                {
                    Main.spriteBatch.Draw(drawData.drawTexture, vector, rectangle, color2, 0f, TileDrawing._zero, 1f, SpriteEffects.None, 0f);
                }
                else if (TileID.Sets.Platforms[drawData.tileCache.type])
                {
                    Main.spriteBatch.Draw(drawData.drawTexture, vector, rectangle, color2, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);

                    if (drawData.tileCache.slope() == 1 &&
                        Main.tile[x + 1, y + 1].active() &&
                        Main.tileSolid[Main.tile[x + 1, y + 1].type] &&
                        Main.tile[x + 1, y + 1].slope() != 2 &&
                        !Main.tile[x + 1, y + 1].halfBrick() &&
                        (!Main.tile[x, y + 1].active() || Main.tile[x, y + 1].blockType() != 0 && Main.tile[x, y + 1].blockType() != 5 || !TileID.Sets.BlocksStairs[Main.tile[x, y + 1].type] && !TileID.Sets.BlocksStairsAbove[Main.tile[x, y + 1].type]))
                    {
                        Rectangle value = new(198, drawData.tileFrameY, 16, 16);

                        if (TileID.Sets.Platforms[Main.tile[x + 1, y + 1].type] && Main.tile[x + 1, y + 1].slope() == 0)
                        {
                            value.X = 324;
                        }

                        Main.spriteBatch.Draw(drawData.drawTexture, vector + new Vector2(0f, 16f), value, color2, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
                    }
                    else if (drawData.tileCache.slope() == 2 &&
                             Main.tile[x - 1, y + 1].active() &&
                             Main.tileSolid[Main.tile[x - 1, y + 1].type] &&
                             Main.tile[x - 1, y + 1].slope() != 1 &&
                             !Main.tile[x - 1, y + 1].halfBrick() &&
                             (!Main.tile[x, y + 1].active() || Main.tile[x, y + 1].blockType() != 0 && Main.tile[x, y + 1].blockType() != 4 || !TileID.Sets.BlocksStairs[Main.tile[x, y + 1].type] && !TileID.Sets.BlocksStairsAbove[Main.tile[x, y + 1].type]))
                    {
                        Rectangle value2 = new (162, drawData.tileFrameY, 16, 16);

                        if (TileID.Sets.Platforms[Main.tile[x - 1, y + 1].type] && Main.tile[x - 1, y + 1].slope() == 0)
                        {
                            value2.X = 306;
                        }

                        Main.spriteBatch.Draw(drawData.drawTexture, vector + new Vector2(0f, 16f), value2, color2, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
                    }
                }
                else if (TileID.Sets.HasSlopeFrames[drawData.tileCache.type])
                {
                    Main.spriteBatch.Draw(drawData.drawTexture, vector, new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY, 16, 16), color2, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
                }
                else
                {
                    int num4 = drawData.tileCache.slope();
                    int num5 = 2;

                    for (int i = 0; i < 8; i++)
                    {
                        int num6 = i * -2;
                        int num7 = 16 - i * 2;
                        int num8 = 16 - num7;
                        int num9;

                        switch (num4)
                        {
                            case 1:
                                num6 = 0;
                                num9 = i * 2;
                                num7 = 14 - i * 2;
                                num8 = 0;

                                break;

                            case 2:
                                num6 = 0;
                                num9 = 16 - i * 2 - 2;
                                num7 = 14 - i * 2;
                                num8 = 0;

                                break;

                            case 3:
                                num9 = i * 2;

                                break;

                            default:
                                num9 = 16 - i * 2 - 2;

                                break;
                        }

                        Main.spriteBatch.Draw(drawData.drawTexture, vector + new Vector2(num9, i * num5 + num6), new Rectangle(drawData.tileFrameX + drawData.addFrX + num9, drawData.tileFrameY + drawData.addFrY + num8, num5, num7), color2, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
                    }

                    int num10 = num4 <= 2 ? 14 : 0;
                    Main.spriteBatch.Draw(drawData.drawTexture, vector + new Vector2(0f, num10), new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY + num10, 16, 2), color2, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
                }
            }

            if (drawData.glowTexture != null)
            {
                Vector2 position = new Vector2(x * 16 - (int)screenPosition.X - (drawData.tileWidth - 16f) / 2f, y * 16 - (int)screenPosition.Y + drawData.tileTop) + screenOffset;

                if (TileID.Sets.Platforms[drawData.typeCache])
                {
                    position = vector;
                }

                Main.spriteBatch.Draw(drawData.glowTexture, position, drawData.glowSourceRect, drawData.glowColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
            }

            if (highlightTexture != null)
            {
                empty = new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY, drawData.tileWidth, drawData.tileHeight);
                const int num11 = 0;
                const int num13 = 0;

                Main.spriteBatch.Draw(highlightTexture, new Vector2(x * 16 - (int)screenPosition.X - (drawData.tileWidth - 16f) / 2f + num11, y * 16 - (int)screenPosition.Y + drawData.tileTop + num13) + screenOffset, empty, highlightColor, 0f, TileDrawing._zero, 1f,
                    drawData.tileSpriteEffect, 0f);
            }
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