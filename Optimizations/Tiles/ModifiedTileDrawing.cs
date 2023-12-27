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
    public static void DrawSingleTile(FnaVector2 screenPosition, FnaVector2 screenOffset, int tileX, int tileY)
    {
        TileDrawing td = Main.instance.TilesRenderer;

        TileDrawInfo drawData = td._currentTileDrawInfo.Value!;

        drawData.tileCache = Main.tile[tileX, tileY];
        drawData.typeCache = drawData.tileCache.type;
        drawData.tileFrameX = drawData.tileCache.frameX;
        drawData.tileFrameY = drawData.tileCache.frameY;
        drawData.tileLight = Lighting.GetColor(tileX, tileY);

        if (drawData.tileCache is { liquid: > 0, type: 518 })
        {
            return;
        }

        td.GetTileDrawData(tileX, tileY, drawData.tileCache, drawData.typeCache, ref drawData.tileFrameX, ref drawData.tileFrameY, out drawData.tileWidth, out drawData.tileHeight, out drawData.tileTop, out drawData.halfBrickHeight, out drawData.addFrX, out drawData.addFrY,
            out drawData.tileSpriteEffect, out drawData.glowTexture, out drawData.glowSourceRect, out drawData.glowColor);

        drawData.drawTexture = td.GetTileDrawTexture(drawData.tileCache, tileX, tileY);
        Texture2D? highlightTexture = null;
        Color highlightColor = Color.Transparent;

        if (TileID.Sets.HasOutlines[drawData.typeCache])
        {
            td.GetTileOutlineInfo(tileX, tileY, drawData.typeCache, ref drawData.tileLight, ref highlightTexture, ref highlightColor);
        }

        if (td._localPlayer.dangerSense && TileDrawing.IsTileDangerous(tileX, tileY, td._localPlayer, drawData.tileCache, drawData.typeCache))
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

            if (td._isActiveAndNotPaused && td._rand.NextBool(30))
            {
                int num = Dust.NewDust(new FnaVector2(tileX * 16, tileY * 16), 16, 16, DustID.RedTorch, 0f, 0f, 100, default, 0.3f);
                td._dust[num].fadeIn = 1f;
                td._dust[num].velocity *= 0.1f;
                td._dust[num].noLight = true;
                td._dust[num].noGravity = true;
            }
        }

        if (td._localPlayer.findTreasure && Main.IsTileSpelunkable(tileX, tileY, drawData.typeCache, drawData.tileFrameX, drawData.tileFrameY))
        {
            if (drawData.tileLight.R < 200)
            {
                drawData.tileLight.R = 200;
            }

            if (drawData.tileLight.G < 170)
            {
                drawData.tileLight.G = 170;
            }

            if (td._isActiveAndNotPaused && td._rand.NextBool(60))
            {
                int num2 = Dust.NewDust(new FnaVector2(tileX * 16, tileY * 16), 16, 16, DustID.TreasureSparkle, 0f, 0f, 150, default, 0.3f);
                td._dust[num2].fadeIn = 1f;
                td._dust[num2].velocity *= 0.1f;
                td._dust[num2].noLight = true;
            }
        }

        if (td._localPlayer.biomeSight)
        {
            Color sightColor = Color.White;

            if (Main.IsTileBiomeSightable(tileX, tileY, drawData.typeCache, drawData.tileFrameX, drawData.tileFrameY, ref sightColor))
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

                if (td._isActiveAndNotPaused && td._rand.NextBool(480))
                {
                    int num3 = Dust.NewDust(new FnaVector2(tileX * 16, tileY * 16), 16, 16, DustID.RainbowMk2, 0f, 0f, 150, sightColor, 0.3f);
                    td._dust[num3].noGravity = true;
                    td._dust[num3].fadeIn = 1f;
                    td._dust[num3].velocity *= 0.1f;
                    td._dust[num3].noLightEmittence = true;
                }
            }
        }

        if (td._isActiveAndNotPaused)
        {
            if (!Lighting.UpdateEveryFrame || new FastRandom(Main.TileFrameSeed).WithModifier(tileX, tileY).Next(4) == 0)
            {
                td.DrawTiles_EmitParticles(tileY, tileX, drawData.tileCache, drawData.typeCache, drawData.tileFrameX, drawData.tileFrameY, drawData.tileLight);
            }
        }

        drawData.tileLight = Color.White;

        bool flag = true;

        flag &= TileDrawing.IsVisible(drawData.tileCache);

        td.CacheSpecialDraws_Part1(tileX, tileY, drawData.typeCache, drawData.tileFrameX, drawData.tileFrameY, !flag);
        td.CacheSpecialDraws_Part2(tileX, tileY, drawData, !flag);

        if (drawData is { typeCache: 72, tileFrameX: >= 36 })
        {
            int num4 = 0;

            if (drawData.tileFrameY == 18)
            {
                num4 = 1;
            }
            else if (drawData.tileFrameY == 36)
            {
                num4 = 2;
            }

            Main.spriteBatch.Draw(TextureAssets.ShroomCap.Value, new FnaVector2(tileX * 16 - (int)screenPosition.X - 22, tileY * 16 - (int)screenPosition.Y - 26) + screenOffset, new Rectangle(num4 * 62, 0, 60, 42), Lighting.GetColor(tileX, tileY), 0f, TileDrawing._zero, 1f,
                drawData.tileSpriteEffect,
                0f);
        }

        Rectangle rectangle = new(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY, drawData.tileWidth, drawData.tileHeight - drawData.halfBrickHeight);
        FnaVector2 vector = new FnaVector2(tileX * 16 - (int)screenPosition.X - (drawData.tileWidth - 16f) / 2f, tileY * 16 - (int)screenPosition.Y + drawData.tileTop + drawData.halfBrickHeight) + screenOffset;
        TileLoader.DrawEffects(tileX, tileY, drawData.typeCache, Main.spriteBatch, ref drawData);

        if (!flag)
        {
            return;
        }

        drawData.colorTint = Color.White;
        drawData.finalColor = Color.White;

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

            case 442: {
                int num7 = drawData.tileFrameX / 22;

                if (num7 == 3)
                {
                    vector.X += 2f;
                }

                break;
            }

            case 51:
                //drawData.finalColor = drawData.tileLight * 0.5f;
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
                int num6 = 2;

                if (drawData.tileFrameX >= 324)
                {
                    drawData.finalColor = Color.Transparent;
                }

                if (drawData.tileFrameY < 36)
                {
                    vector.Y += num6 * (drawData.tileFrameY == 0).ToDirectionInt();
                }
                else
                {
                    vector.X += num6 * (drawData.tileFrameY == 36).ToDirectionInt();
                }

                break;
            }

            case 272: {
                int num5 = Main.tileFrame[drawData.typeCache];
                num5 += tileX % 2;
                num5 += tileY % 2;
                num5 += tileX % 3;
                num5 += tileY % 3;
                num5 %= 2;
                num5 *= 90;
                drawData.addFrY += num5;
                rectangle.Y += num5;

                break;
            }

            case 80: {
                WorldGen.GetCactusType(tileX, tileY, drawData.tileFrameX, drawData.tileFrameY, out var evil, out var good, out var crimson);

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
                drawData.drawTexture = td.GetTileDrawTexture(drawData.tileCache, tileX, tileY);

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
            td.DrawTile_MinecartTrack(screenPosition, screenOffset, tileX, tileY, drawData);
        }
        else if (drawData.typeCache == 171)
        {
            td.DrawXmasTree(screenPosition, screenOffset, tileX, tileY, drawData);
        }
        else
        {
            DrawBasicTile(screenPosition, screenOffset, tileX, tileY, drawData, rectangle, vector);
        }

        if (Main.tileGlowMask[drawData.tileCache.type] != -1)
        {
            short num8 = Main.tileGlowMask[drawData.tileCache.type];

            if (TextureAssets.GlowMask.IndexInRange(num8))
            {
                drawData.drawTexture = TextureAssets.GlowMask[num8].Value;
            }

            double num9 = Main.timeForVisualEffects * 0.08;
            Color color2 = Color.White;
            bool flag2 = false;

            switch (drawData.tileCache.type)
            {
                case 633:
                    color2 = Color.Lerp(Color.White, drawData.finalColor, 0.75f);

                    break;

                case 659:
                case 667:
                    color2 = LiquidRenderer.GetShimmerGlitterColor(top: true, tileX, tileY);

                    break;

                case 350:
                    color2 = new Color(new Vector4((float)((0.0 - Math.Cos(((int)(num9 / 6.283) % 3 == 1) ? num9 : 0.0)) * 0.2 + 0.2)));

                    break;

                case 381:
                case 517:
                case 687:
                    color2 = td._lavaMossGlow;

                    break;

                case 534:
                case 535:
                case 689:
                    color2 = td._kryptonMossGlow;

                    break;

                case 536:
                case 537:
                case 690:
                    color2 = td._xenonMossGlow;

                    break;

                case 539:
                case 540:
                case 688:
                    color2 = td._argonMossGlow;

                    break;

                case 625:
                case 626:
                case 691:
                    color2 = td._violetMossGlow;

                    break;

                case 627:
                case 628:
                case 692:
                    color2 = new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB);

                    break;

                case 370:
                case 390:
                    color2 = td._meteorGlow;

                    break;

                case 391:
                    color2 = new Color(250, 250, 250, 200);

                    break;

                case 209:
                    color2 = PortalHelper.GetPortalColor(Main.myPlayer, (drawData.tileCache.frameX >= 288) ? 1 : 0);

                    break;

                case 429:
                case 445:
                    drawData.drawTexture = td.GetTileDrawTexture(drawData.tileCache, tileX, tileY);
                    drawData.addFrY = 18;

                    break;

                case 129: {
                    if (drawData.tileFrameX < 324)
                    {
                        flag2 = true;

                        break;
                    }

                    drawData.drawTexture = td.GetTileDrawTexture(drawData.tileCache, tileX, tileY);
                    color2 = Main.hslToRgb(0.7f + (float)Math.Sin((float)Math.PI * 2f * Main.GlobalTimeWrappedHourly * 0.16f + tileX * 0.3f + tileY * 0.7f) * 0.16f, 1f, 0.5f);
                    color2.A /= 2;
                    color2 *= 0.3f;
                    int num10 = 72;

                    for (float num11 = 0f; num11 < (float)Math.PI * 2f; num11 += (float)Math.PI / 2f)
                    {
                        Main.spriteBatch.Draw(drawData.drawTexture, vector + num11.ToRotationVector2() * 2f, new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY + num10, drawData.tileWidth, drawData.tileHeight), color2, 0f, FnaVector2.Zero, 1f,
                            SpriteEffects.None,
                            0f);
                    }

                    color2 = new Color(255, 255, 255, 100);

                    break;
                }
            }

            color2 = Color.White;

            if (!flag2)
            {
                if (drawData.tileCache.slope() == 0 && !drawData.tileCache.halfBrick())
                {
                    Main.spriteBatch.Draw(drawData.drawTexture, vector, new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY, drawData.tileWidth, drawData.tileHeight), color2, 0f, FnaVector2.Zero, 1f, SpriteEffects.None, 0f);
                }
                else if (drawData.tileCache.halfBrick())
                {
                    Main.spriteBatch.Draw(drawData.drawTexture, vector, rectangle, color2, 0f, TileDrawing._zero, 1f, SpriteEffects.None, 0f);
                }
                else if (TileID.Sets.Platforms[drawData.tileCache.type])
                {
                    Main.spriteBatch.Draw(drawData.drawTexture, vector, rectangle, color2, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);

                    if (drawData.tileCache.slope() == 1 &&
                        Main.tile[tileX + 1, tileY + 1].active() &&
                        Main.tileSolid[Main.tile[tileX + 1, tileY + 1].type] &&
                        Main.tile[tileX + 1, tileY + 1].slope() != 2 &&
                        !Main.tile[tileX + 1, tileY + 1].halfBrick() &&
                        (!Main.tile[tileX, tileY + 1].active() || (Main.tile[tileX, tileY + 1].blockType() != 0 && Main.tile[tileX, tileY + 1].blockType() != 5) || (!TileID.Sets.BlocksStairs[Main.tile[tileX, tileY + 1].type] && !TileID.Sets.BlocksStairsAbove[Main.tile[tileX, tileY + 1].type])))
                    {
                        Rectangle value = new(198, drawData.tileFrameY, 16, 16);

                        if (TileID.Sets.Platforms[Main.tile[tileX + 1, tileY + 1].type] && Main.tile[tileX + 1, tileY + 1].slope() == 0)
                        {
                            value.X = 324;
                        }

                        Main.spriteBatch.Draw(drawData.drawTexture, vector + new FnaVector2(0f, 16f), value, color2, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
                    }
                    else if (drawData.tileCache.slope() == 2 &&
                             Main.tile[tileX - 1, tileY + 1].active() &&
                             Main.tileSolid[Main.tile[tileX - 1, tileY + 1].type] &&
                             Main.tile[tileX - 1, tileY + 1].slope() != 1 &&
                             !Main.tile[tileX - 1, tileY + 1].halfBrick() &&
                             (!Main.tile[tileX, tileY + 1].active() || (Main.tile[tileX, tileY + 1].blockType() != 0 && Main.tile[tileX, tileY + 1].blockType() != 4) || (!TileID.Sets.BlocksStairs[Main.tile[tileX, tileY + 1].type] && !TileID.Sets.BlocksStairsAbove[Main.tile[tileX, tileY + 1].type])))
                    {
                        Rectangle value2 = new(162, drawData.tileFrameY, 16, 16);

                        if (TileID.Sets.Platforms[Main.tile[tileX - 1, tileY + 1].type] && Main.tile[tileX - 1, tileY + 1].slope() == 0)
                        {
                            value2.X = 306;
                        }

                        Main.spriteBatch.Draw(drawData.drawTexture, vector + new FnaVector2(0f, 16f), value2, color2, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
                    }
                }
                else if (TileID.Sets.HasSlopeFrames[drawData.tileCache.type])
                {
                    Main.spriteBatch.Draw(drawData.drawTexture, vector, new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY, 16, 16), color2, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
                }
                else
                {
                    int num12 = drawData.tileCache.slope();
                    int num13 = 2;

                    for (int i = 0; i < 8; i++)
                    {
                        int num14 = i * -2;
                        int num15 = 16 - i * 2;
                        int num16 = 16 - num15;
                        int num17;

                        switch (num12)
                        {
                            case 1:
                                num14 = 0;
                                num17 = i * 2;
                                num15 = 14 - i * 2;
                                num16 = 0;

                                break;

                            case 2:
                                num14 = 0;
                                num17 = 16 - i * 2 - 2;
                                num15 = 14 - i * 2;
                                num16 = 0;

                                break;

                            case 3:
                                num17 = i * 2;

                                break;

                            default:
                                num17 = 16 - i * 2 - 2;

                                break;
                        }

                        Main.spriteBatch.Draw(drawData.drawTexture, vector + new FnaVector2(num17, i * num13 + num14), new Rectangle(drawData.tileFrameX + drawData.addFrX + num17, drawData.tileFrameY + drawData.addFrY + num16, num13, num15), color2, 0f, TileDrawing._zero, 1f,
                            drawData.tileSpriteEffect,
                            0f);
                    }

                    int num18 = ((num12 <= 2) ? 14 : 0);
                    Main.spriteBatch.Draw(drawData.drawTexture, vector + new FnaVector2(0f, num18), new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY + num18, 16, 2), color2, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
                }
            }
        }

        if (drawData.glowTexture != null)
        {
            FnaVector2 position = new FnaVector2(tileX * 16 - (int)screenPosition.X - (drawData.tileWidth - 16f) / 2f, tileY * 16 - (int)screenPosition.Y + drawData.tileTop) + screenOffset;

            if (TileID.Sets.Platforms[drawData.typeCache])
            {
                position = vector;
            }

            Main.spriteBatch.Draw(drawData.glowTexture, position, drawData.glowSourceRect, drawData.glowColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
        }

        if (highlightTexture != null)
        {
            Rectangle empty = new(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY, drawData.tileWidth, drawData.tileHeight);
            int num19 = 0;
            int num20 = 0;

            Main.spriteBatch.Draw(highlightTexture, new FnaVector2(tileX * 16 - (int)screenPosition.X - (drawData.tileWidth - 16f) / 2f + num19, tileY * 16 - (int)screenPosition.Y + drawData.tileTop + num20) + screenOffset, empty, highlightColor, 0f, TileDrawing._zero, 1f,
                drawData.tileSpriteEffect, 0f);
        }
    }

    private static void DrawBasicTile(FnaVector2 screenPosition, FnaVector2 screenOffset, int tileX, int tileY, TileDrawInfo drawData, Rectangle normalTileRect, FnaVector2 normalTilePosition)
    {
        TileDrawing td = Main.instance.TilesRenderer;

        Tile tile;

        if (TileID.Sets.Platforms[drawData.typeCache] && WorldGen.IsRope(tileX, tileY) && Main.tile[tileX, tileY - 1] != null)
        {
            tile = Main.tile[tileX, tileY - 1];
            _ = ref tile.type;
            int y = (tileY + tileX) % 3 * 18;
            Texture2D tileDrawTexture = td.GetTileDrawTexture(Main.tile[tileX, tileY - 1], tileX, tileY);

            if (tileDrawTexture != null)
            {
                Main.spriteBatch.Draw(tileDrawTexture, new FnaVector2(tileX * 16 - (int)screenPosition.X, tileY * 16 - (int)screenPosition.Y) + screenOffset, new Rectangle(90, y, 16, 16), drawData.tileLight, 0f, default, 1f, drawData.tileSpriteEffect, 0f);
            }
        }

        if (drawData.tileCache.slope() > 0)
        {
            if (TileID.Sets.Platforms[drawData.tileCache.type])
            {
                Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, normalTileRect, drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);

                if (drawData.tileCache.slope() == 1)
                {
                    tile = Main.tile[tileX + 1, tileY + 1];

                    if (tile.active())
                    {
                        bool[] tileSolid = Main.tileSolid;
                        tile = Main.tile[tileX + 1, tileY + 1];

                        if (tileSolid[tile.type])
                        {
                            tile = Main.tile[tileX + 1, tileY + 1];

                            if (tile.slope() != 2)
                            {
                                tile = Main.tile[tileX + 1, tileY + 1];

                                if (!tile.halfBrick())
                                {
                                    tile = Main.tile[tileX, tileY + 1];

                                    if (!tile.active())
                                    {
                                        goto IL_0269;
                                    }

                                    tile = Main.tile[tileX, tileY + 1];

                                    if (tile.blockType() != 0)
                                    {
                                        tile = Main.tile[tileX, tileY + 1];

                                        if (tile.blockType() != 5)
                                        {
                                            goto IL_0269;
                                        }
                                    }

                                    bool[] blocksStairs = TileID.Sets.BlocksStairs;
                                    tile = Main.tile[tileX, tileY + 1];

                                    if (!blocksStairs[tile.type])
                                    {
                                        bool[] blocksStairsAbove = TileID.Sets.BlocksStairsAbove;
                                        tile = Main.tile[tileX, tileY + 1];

                                        if (!blocksStairsAbove[tile.type])
                                        {
                                            goto IL_0269;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (drawData.tileCache.slope() != 2)
                {
                    return;
                }

                tile = Main.tile[tileX - 1, tileY + 1];

                if (!tile.active())
                {
                    return;
                }

                bool[] tileSolid2 = Main.tileSolid;
                tile = Main.tile[tileX - 1, tileY + 1];

                if (!tileSolid2[tile.type])
                {
                    return;
                }

                tile = Main.tile[tileX - 1, tileY + 1];

                if (tile.slope() == 1)
                {
                    return;
                }

                tile = Main.tile[tileX - 1, tileY + 1];

                if (tile.halfBrick())
                {
                    return;
                }

                tile = Main.tile[tileX, tileY + 1];

                if (tile.active())
                {
                    tile = Main.tile[tileX, tileY + 1];

                    if (tile.blockType() != 0)
                    {
                        tile = Main.tile[tileX, tileY + 1];

                        if (tile.blockType() != 4)
                        {
                            goto IL_043e;
                        }
                    }

                    bool[] blocksStairs2 = TileID.Sets.BlocksStairs;
                    tile = Main.tile[tileX, tileY + 1];

                    if (blocksStairs2[tile.type])
                    {
                        return;
                    }

                    bool[] blocksStairsAbove2 = TileID.Sets.BlocksStairsAbove;
                    tile = Main.tile[tileX, tileY + 1];

                    if (blocksStairsAbove2[tile.type])
                    {
                        return;
                    }
                }

                goto IL_043e;
            }

            if (TileID.Sets.HasSlopeFrames[drawData.tileCache.type])
            {
                Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY, 16, 16), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);

                return;
            }

            int num = drawData.tileCache.slope();
            int num2 = 2;

            for (int i = 0; i < 8; i++)
            {
                int num3 = i * -2;
                int num4 = 16 - i * 2;
                int num5 = 16 - num4;
                int num6;

                switch (num)
                {
                    case 1:
                        num3 = 0;
                        num6 = i * 2;
                        num4 = 14 - i * 2;
                        num5 = 0;

                        break;

                    case 2:
                        num3 = 0;
                        num6 = 16 - i * 2 - 2;
                        num4 = 14 - i * 2;
                        num5 = 0;

                        break;

                    case 3:
                        num6 = i * 2;

                        break;

                    default:
                        num6 = 16 - i * 2 - 2;

                        break;
                }

                Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition + new FnaVector2(num6, i * num2 + num3), new Rectangle(drawData.tileFrameX + drawData.addFrX + num6, drawData.tileFrameY + drawData.addFrY + num5, num2, num4), drawData.finalColor, 0f, TileDrawing._zero, 1f,
                    drawData.tileSpriteEffect, 0f);
            }

            int num7 = ((num <= 2) ? 14 : 0);
            Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition + new FnaVector2(0f, num7), new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY + num7, 16, 2), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);

            return;
        }

        if (!TileID.Sets.Platforms[drawData.typeCache] && !TileID.Sets.IgnoresNearbyHalfbricksWhenDrawn[drawData.typeCache] && td._tileSolid[drawData.typeCache] && !TileID.Sets.NotReallySolid[drawData.typeCache] && !drawData.tileCache.halfBrick())
        {
            if ((tileX - 1) < 0)
            {
                return;
            }

            tile = Main.tile[tileX - 1, tileY];

            if (!tile.halfBrick())
            {
                if ((tileX + 1) >= Main.tile.Width)
                {
                    return;
                }

                tile = Main.tile[tileX + 1, tileY];

                if (!tile.halfBrick())
                {
                    goto IL_0cc9;
                }
            }

            tile = Main.tile[tileX - 1, tileY];

            if (tile.halfBrick())
            {
                tile = Main.tile[tileX + 1, tileY];

                if (tile.halfBrick())
                {
                    Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition + new FnaVector2(0f, 8f), new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.addFrY + drawData.tileFrameY + 8, drawData.tileWidth, 8), drawData.finalColor, 0f, TileDrawing._zero, 1f,
                        drawData.tileSpriteEffect,
                        0f);

                    Rectangle value = new(126 + drawData.addFrX, drawData.addFrY, 16, 8);
                    tile = Main.tile[tileX, tileY - 1];

                    if (tile.active())
                    {
                        tile = Main.tile[tileX, tileY - 1];

                        if (!tile.bottomSlope())
                        {
                            tile = Main.tile[tileX, tileY - 1];

                            if (tile.type == drawData.typeCache)
                            {
                                value = new Rectangle(90 + drawData.addFrX, drawData.addFrY, 16, 8);
                            }
                        }
                    }

                    Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, value, drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);

                    return;
                }
            }

            tile = Main.tile[tileX - 1, tileY];

            if (tile.halfBrick())
            {
                int num8 = 4;

                if (TileID.Sets.AllBlocksWithSmoothBordersToResolveHalfBlockIssue[drawData.typeCache])
                {
                    num8 = 2;
                }

                Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition + new FnaVector2(0f, 8f), new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.addFrY + drawData.tileFrameY + 8, drawData.tileWidth, 8), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect,
                    0f);

                Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition + new FnaVector2(num8, 0f), new Rectangle(drawData.tileFrameX + num8 + drawData.addFrX, drawData.addFrY + drawData.tileFrameY, drawData.tileWidth - num8, drawData.tileHeight), drawData.finalColor, 0f, TileDrawing._zero,
                    1f,
                    drawData.tileSpriteEffect, 0f);

                Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, new Rectangle(144 + drawData.addFrX, drawData.addFrY, num8, 8), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);

                if (num8 == 2)
                {
                    Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, new Rectangle(148 + drawData.addFrX, drawData.addFrY, 2, 2), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
                }

                return;
            }

            tile = Main.tile[tileX + 1, tileY];

            if (tile.halfBrick())
            {
                int num9 = 4;

                if (TileID.Sets.AllBlocksWithSmoothBordersToResolveHalfBlockIssue[drawData.typeCache])
                {
                    num9 = 2;
                }

                Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition + new FnaVector2(0f, 8f), new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.addFrY + drawData.tileFrameY + 8, drawData.tileWidth, 8), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect,
                    0f);

                Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.addFrY + drawData.tileFrameY, drawData.tileWidth - num9, drawData.tileHeight), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
                Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition + new FnaVector2(16 - num9, 0f), new Rectangle(144 + (16 - num9), 0, num9, 8), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);

                if (num9 == 2)
                {
                    Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition + new FnaVector2(14f, 0f), new Rectangle(156, 0, 2, 2), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
                }
            }

            return;
        }

        goto IL_0cc9;

        IL_043e:
        Rectangle value2 = new(162, drawData.tileFrameY, 16, 16);
        bool[] platforms = TileID.Sets.Platforms;
        tile = Main.tile[tileX - 1, tileY + 1];

        if (platforms[tile.type])
        {
            tile = Main.tile[tileX - 1, tileY + 1];

            if (tile.slope() == 0)
            {
                value2.X = 306;
            }
        }

        Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition + new FnaVector2(0f, 16f), value2, drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);

        return;

        IL_0cc9:

        if (Lighting.NotRetro && td._tileSolid[drawData.typeCache] && !drawData.tileCache.halfBrick() && !TileID.Sets.DontDrawTileSliced[drawData.tileCache.type])
        {
            //td.DrawSingleTile_SlicedBlock(normalTilePosition, tileX, tileY, drawData);
            Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY, drawData.tileWidth, drawData.tileHeight), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);

            return;
        }

        if (drawData.halfBrickHeight != 8)
        {
            goto IL_0e81;
        }

        tile = Main.tile[tileX, tileY + 1];

        if (tile.active())
        {
            bool[] tileSolid3 = td._tileSolid;
            tile = Main.tile[tileX, tileY + 1];

            if (tileSolid3[tile.type])
            {
                tile = Main.tile[tileX, tileY + 1];

                if (!tile.halfBrick())
                {
                    goto IL_0e81;
                }
            }
        }

        if (TileID.Sets.Platforms[drawData.typeCache])
        {
            Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, normalTileRect, drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
        }
        else
        {
            Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, normalTileRect.Modified(0, 0, 0, -4), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
            Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition + new FnaVector2(0f, 4f), new Rectangle(144 + drawData.addFrX, 66 + drawData.addFrY, drawData.tileWidth, 4), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
        }

        goto IL_101e;

        IL_0269:
        Rectangle value3 = new(198, drawData.tileFrameY, 16, 16);
        bool[] platforms2 = TileID.Sets.Platforms;
        tile = Main.tile[tileX + 1, tileY + 1];

        if (platforms2[tile.type])
        {
            tile = Main.tile[tileX + 1, tileY + 1];

            if (tile.slope() == 0)
            {
                value3.X = 324;
            }
        }

        Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition + new FnaVector2(0f, 16f), value3, drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);

        return;

        IL_101e:
        td.DrawSingleTile_Flames(screenPosition, screenOffset, tileX, tileY, drawData);

        return;

        IL_0e81:

        if (TileID.Sets.CritterCageLidStyle[drawData.typeCache] >= 0)
        {
            int num10 = TileID.Sets.CritterCageLidStyle[drawData.typeCache];

            if ((num10 < 3 && normalTileRect.Y % 54 == 0) || (num10 >= 3 && normalTileRect.Y % 36 == 0))
            {
                FnaVector2 position = normalTilePosition;
                position.Y += 8f;
                Rectangle value4 = normalTileRect;
                value4.Y += 8;
                value4.Height -= 8;
                Main.spriteBatch.Draw(drawData.drawTexture, position, value4, drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
                position = normalTilePosition;
                position.Y -= 2f;
                value4 = normalTileRect;
                value4.Y = 0;
                value4.Height = 10;
                Main.spriteBatch.Draw(TextureAssets.CageTop[num10].Value, position, value4, drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
            }
            else
            {
                Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, normalTileRect, drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
            }
        }
        else
        {
            Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, normalTileRect, drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
        }

        goto IL_101e;
    }

    public static void StillHandleSpecialsBecauseTerrariaWasPoorlyProgrammed(int type, bool flag, int j, int i, int frameX, int frameY, Tile tile)
    {
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
                if (flag)
                {
                    Main.instance.TilesRenderer.CrawlToTopOfVineAndAddSpecialPoint(i, j);
                }

                break;

            case 549:
                if (flag)
                {
                    Main.instance.TilesRenderer.CrawlToBottomOfReverseVineAndAddSpecialPoint(i, j);
                }

                break;

            case 34:
                if (frameX % 54 == 0 && frameY % 54 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 454:
                if (frameX % 72 == 0 && frameY % 54 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 42:
            case 270:
            case 271:
            case 572:
            case 581:
            case 660:
                if (frameX % 18 == 0 && frameY % 36 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 91:
                if (frameX % 18 == 0 && frameY % 54 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 95:
            case 126:
            case 444:
                if (frameX % 36 == 0 && frameY % 36 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 465:
            case 591:
            case 592:
                if (frameX % 36 == 0 && frameY % 54 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 27:
                if (frameX % 36 == 0 && frameY == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 236:
            case 238:
                if (frameX % 36 == 0 && frameY == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 233:
                if (frameY == 0 && frameX % 54 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.MultiTileGrass);
                }

                if (frameY == 34 && frameX % 36 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 652:
                if (frameX % 36 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 651:
                if (frameX % 54 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 530:
                if (frameX < 270)
                {
                    if (frameX % 54 == 0 && frameY == 0 && flag)
                    {
                        Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.MultiTileGrass);
                    }

                    break;
                }

                break;

            case 485:
            case 489:
            case 490:
                if (frameY == 0 && frameX % 36 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 521:
            case 522:
            case 523:
            case 524:
            case 525:
            case 526:
            case 527:
                if (frameY == 0 && frameX % 36 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 493:
                if (frameY == 0 && frameX % 18 == 0 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 519:
                if (frameX / 18 <= 4 && flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 373:
            case 374:
            case 375:
            case 461:
                Main.instance.TilesRenderer.EmitLiquidDrops(i, j, tile, (ushort)type);

                break;

            case 491:
                if (flag && frameX == 18 && frameY == 18)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.VoidLens);
                }

                break;

            case 597:
                if (flag && frameX % 54 == 0 && frameY == 0)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.TeleportationPylon);
                }

                break;

            case 617:
                if (flag && frameX % 54 == 0 && frameY % 72 == 0)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.MasterTrophy);
                }

                break;

            case 184:
                if (flag)
                {
                    Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.AnyDirectionalGrass);
                }

                break;

            default:
                if (Main.instance.TilesRenderer.ShouldSwayInWind(j, i, tile))
                {
                    if (flag)
                    {
                        Main.instance.TilesRenderer.AddSpecialPoint(j, i, TileDrawing.TileCounterType.WindyGrass);
                    }

                    break;
                }

                break;
        }
    }
}