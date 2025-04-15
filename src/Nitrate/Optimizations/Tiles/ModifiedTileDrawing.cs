﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.Liquid;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace TeamCatalyst.Nitrate.Optimizations.Tiles;

internal static class ModifiedTileDrawing {
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
    public static void DrawSingleTile(bool vanilla, bool solid, int x, int y, FnaVector2 screenPosition) {
        var j = x;
        var i = y;

        // Covered by temporary check below.
        /*if (!WorldGen.InWorld(x, y))
        {
            return;
        }*/

        // TODO: Properly handle cases where this actually throws.
        // Currently, occasional crashes occur in methods like
        // EmitLivingTreeLeaf_Below because of an IOOB exception due to stupid,
        // direct indexing of Main::tile instead of using Framing.GetTileSafely.
        for (var xx = x - 2; xx <= x + 2; xx++) {
            for (var yy = y - 2; yy <= y + 2; yy++) {
                if (!WorldGen.InWorld(xx, yy)) {
                    return;
                }
            }
        }

        var tile = Framing.GetTileSafely(x, y);
        var type = tile.type;

        if (!tile.HasTile) {
            return;
        }

        if (!TextureAssets.Tile[type].IsLoaded) {
            Main.instance.LoadTiles(type);
        }

        // In tML and vanilla, certain walls/tiles automatically include a draw offset of Main.offScreenRange if drawToScreen is false.
        // This is because of the buffer zone around the world, although the offset is not needed with RTs.
        // This fixes the behaviour of this offset with Nitrate, by forcing it to always be zero in line with Nitrate's new drawing system.
        bool oldDrawToScreen = Main.drawToScreen;
        Main.drawToScreen = true;

        if (vanilla) {
            Main.screenPosition += new Vector2(Main.offScreenRange);

            if (TileLoader.PreDraw(j, i, type, Main.spriteBatch)) {
                Main.screenPosition -= new Vector2(Main.offScreenRange);
                AddSpecialPointsForTile(tile, x, y);
                DrawSingleTile_Inner(vanilla, solid, x, y, screenPosition, Vector2.Zero, tile, type);
                Main.screenPosition += new Vector2(Main.offScreenRange);
            }

            TileLoader.PostDraw(j, i, type, Main.spriteBatch);
            Main.screenPosition -= new Vector2(Main.offScreenRange);
        }
        else {
            DrawSingleTile_Inner(vanilla, solid, x, y, screenPosition, Vector2.Zero, tile, type);
        }

        Main.drawToScreen = oldDrawToScreen;
    }

    private static void DrawSingleTile_Inner(bool vanilla, bool solid, int x, int y, FnaVector2 screenPosition, FnaVector2 screenOffset, Tile tile, ushort type) {
        _ = solid;

        if (IsDrawnBySpecialPoint(tile, x, y)) {
            return;
        }

        var drawData = Main.instance.TilesRenderer._currentTileDrawInfo.Value!;
        drawData.tileCache = tile;
        drawData.typeCache = type;
        drawData.tileFrameX = tile.frameX;
        drawData.tileFrameY = tile.frameY;
        // drawData.tileLight = Lighting.GetColor(x, y);
        drawData.tileLight = vanilla ? Lighting.GetColor(x, y) : Color.White;

        if (tile is { liquid: > 0, type: 518 }) {
            return;
        }

        Main.instance.TilesRenderer.GetTileDrawData(
            x,
            y,
            tile,
            type,
            ref drawData.tileFrameX,
            ref drawData.tileFrameY,
            out drawData.tileWidth,
            out drawData.tileHeight,
            out drawData.tileTop,
            out drawData.halfBrickHeight,
            out drawData.addFrX,
            out drawData.addFrY,
            out drawData.tileSpriteEffect,
            out drawData.glowTexture,
            out drawData.glowSourceRect,
            out drawData.glowColor
        );

        drawData.drawTexture = Main.instance.TilesRenderer.GetTileDrawTexture(drawData.tileCache, x, y);
        Texture2D? highlightTexture = null;
        var highlightColor = Color.Transparent;

        if (TileID.Sets.HasOutlines[drawData.typeCache]) {
            Main.instance.TilesRenderer.GetTileOutlineInfo(x, y, drawData.typeCache, ref drawData.tileLight, ref highlightTexture, ref highlightColor);
        }

        if (vanilla) {
            if (Main.LocalPlayer.dangerSense && TileDrawing.IsTileDangerous(x, y, Main.LocalPlayer, drawData.tileCache, drawData.typeCache)) {
                if (drawData.tileLight.R < byte.MaxValue) {
                    drawData.tileLight.R = byte.MaxValue;
                }

                if (drawData.tileLight.G < 50) {
                    drawData.tileLight.G = 50;
                }

                if (drawData.tileLight.B < 50) {
                    drawData.tileLight.B = 50;
                }

                if (IsActiveAndNotPaused && Main.instance.TilesRenderer._rand.NextBool(30)) {
                    var dust = Dust.NewDustDirect(new Vector2(x * 16, y * 16), 16, 16, DustID.RedTorch, 0f, 0f, 100, default, 0.3f);
                    dust.fadeIn = 1f;
                    dust.velocity *= 0.1f;
                    dust.noLight = true;
                    dust.noGravity = true;
                }
            }

            if (Main.LocalPlayer.findTreasure && Main.IsTileSpelunkable(x, y, drawData.typeCache, drawData.tileFrameX, drawData.tileFrameY)) {
                if (drawData.tileLight.R < 200) {
                    drawData.tileLight.R = 200;
                }

                if (drawData.tileLight.G < 170) {
                    drawData.tileLight.G = 170;
                }

                if (IsActiveAndNotPaused && Main.instance.TilesRenderer._rand.NextBool(60)) {
                    var dust = Dust.NewDustDirect(new Vector2(x * 16, y * 16), 16, 16, DustID.TreasureSparkle, 0f, 0f, 150, default, 0.3f);
                    dust.fadeIn = 1f;
                    dust.velocity *= 0.1f;
                    dust.noLight = true;
                }
            }

            if (Main.LocalPlayer.biomeSight) {
                var sightColor = Color.White;

                if (Main.IsTileBiomeSightable(x, y, drawData.typeCache, drawData.tileFrameX, drawData.tileFrameY, ref sightColor)) {
                    if (drawData.tileLight.R < sightColor.R) {
                        drawData.tileLight.R = sightColor.R;
                    }

                    if (drawData.tileLight.G < sightColor.G) {
                        drawData.tileLight.G = sightColor.G;
                    }

                    if (drawData.tileLight.B < sightColor.B) {
                        drawData.tileLight.B = sightColor.B;
                    }

                    if (IsActiveAndNotPaused && Main.instance.TilesRenderer._rand.NextBool(480)) {
                        var dust = Dust.NewDustDirect(new Vector2(x * 16, y * 16), 16, 16, DustID.RainbowMk2, 0f, 0f, 150, sightColor, 1.5f);
                        dust.noGravity = true;
                        dust.fadeIn = 1f;
                        dust.velocity *= 0.1f;
                        dust.noLightEmittence = true;
                    }
                }
            }
        }

        if (IsActiveAndNotPaused) {
            if (!Lighting.UpdateEveryFrame || new FastRandom(Main.TileFrameSeed).WithModifier(x, y).Next(4) == 0) {
                Main.instance.TilesRenderer.DrawTiles_EmitParticles(y, x, drawData.tileCache, drawData.typeCache, drawData.tileFrameX, drawData.tileFrameY, drawData.tileLight);
            }

            drawData.tileLight = Main.instance.TilesRenderer.DrawTiles_GetLightOverride(x, y, drawData.tileCache, drawData.typeCache, drawData.tileFrameX, drawData.tileFrameY, drawData.tileLight);
        }

        // bool flag = drawData.tileLight.R >= 1 || drawData.tileLight.G >= 1 || drawData.tileLight.B >= 1 || drawData.tileCache.wall > 0 && (drawData.tileCache.wall == 318 || drawData.tileCache.fullbrightWall());
        // flag &= TileDrawing.IsVisible(drawData.tileCache);
        const bool flag = true;

        Main.instance.TilesRenderer.CacheSpecialDraws_Part1(x, y, drawData.typeCache, drawData.tileFrameX, drawData.tileFrameY, !flag);
        Main.instance.TilesRenderer.CacheSpecialDraws_Part2(x, y, drawData, !flag);

        if (drawData is { typeCache: 72, tileFrameX: >= 36 }) {
            var num15 = drawData.tileFrameY switch {
                18 => 1,
                36 => 2,
                _ => 0,
            };

            Main.spriteBatch.Draw(TextureAssets.ShroomCap.Value, new Vector2(x * 16 - (int)screenPosition.X - 22, y * 16 - (int)screenPosition.Y - 26) + screenOffset, new Rectangle(num15 * 62, 0, 60, 42), Lighting.GetColor(x, y), 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
        }

        Rectangle rectangle = new(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY, drawData.tileWidth, drawData.tileHeight - drawData.halfBrickHeight);
        var vector = new Vector2(x * 16 - (int)screenPosition.X - (drawData.tileWidth - 16f) / 2f, y * 16 - (int)screenPosition.Y + drawData.tileTop + drawData.halfBrickHeight) + screenOffset;
        TileLoader.DrawEffects(x, y, drawData.typeCache, Main.spriteBatch, ref drawData);

        /*if (!flag)
        {
            return;
        }*/

        drawData.colorTint = Color.White;
        drawData.finalColor = vanilla ? TileDrawing.GetFinalLight(drawData.tileCache, drawData.typeCache, drawData.tileLight, drawData.colorTint) : Color.White;

        switch (drawData.typeCache) {
            case 136:
                switch (drawData.tileFrameX / 18) {
                    case 1:
                        vector.X += -2f;

                        break;

                    case 2:
                        vector.X += 2f;

                        break;
                }

                break;

            case 442:
                if (drawData.tileFrameX / 22 == 3) {
                    vector.X += 2f;
                }

                break;

            case 51:
                if (vanilla) {
                    drawData.finalColor = drawData.tileLight * 0.5f;
                }

                break;

            case 160:
            case 692: {
                Color color = new(Main.DiscoR, Main.DiscoG, Main.DiscoB, 255);

                if (drawData.tileCache.inActive()) {
                    color = drawData.tileCache.actColor(color);
                }

                drawData.finalColor = color;

                break;
            }

            case 129: {
                drawData.finalColor = new Color(255, 255, 255, 100);
                const int num17 = 2;

                if (drawData.tileFrameX >= 324) {
                    drawData.finalColor = Color.Transparent;
                }

                if (drawData.tileFrameY < 36) {
                    vector.Y += num17 * (drawData.tileFrameY == 0).ToDirectionInt();
                }
                else {
                    vector.X += num17 * (drawData.tileFrameY == 36).ToDirectionInt();
                }

                break;
            }

            case 272: {
                var num16 = Main.tileFrame[drawData.typeCache];
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
                WorldGen.GetCactusType(x, y, drawData.tileFrameX, drawData.tileFrameY, out var evil, out var good, out var crimson);

                if (evil) {
                    rectangle.Y += 54;
                }

                if (good) {
                    rectangle.Y += 108;
                }

                if (crimson) {
                    rectangle.Y += 162;
                }

                break;
            }

            case 83:
                drawData.drawTexture = Main.instance.TilesRenderer.GetTileDrawTexture(drawData.tileCache, x, y);

                break;

            case 323:
                if (drawData.tileCache.frameX is <= 132 and >= 88) {
                    return;
                }

                vector.X += drawData.tileCache.frameY;

                break;

            case 114:
                if (drawData.tileFrameY > 0) {
                    rectangle.Height += 2;
                }

                break;
        }

        if (!vanilla) {
            drawData.tileLight = Color.White;
        }

        if (drawData.typeCache == 314) {
            Main.instance.TilesRenderer.DrawTile_MinecartTrack(screenPosition, screenOffset, x, y, drawData);
        }
        else if (drawData.typeCache == 171) {
            Main.instance.TilesRenderer.DrawXmasTree(screenPosition, screenOffset, x, y, drawData);
        }
        else {
            DrawBasicTile(vanilla, screenPosition, screenOffset, x, y, drawData, rectangle, vector);
        }

        if (Main.tileGlowMask[drawData.tileCache.type] != -1) {
            var num18 = Main.tileGlowMask[drawData.tileCache.type];

            if (TextureAssets.GlowMask.IndexInRange(num18)) {
                drawData.drawTexture = TextureAssets.GlowMask[num18].Value;
            }

            var num19 = Main.timeForVisualEffects * 0.08;
            var color2 = Color.White;
            var flag2 = false;

            switch (drawData.tileCache.type) {
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
                    if (drawData.tileFrameX < 324) {
                        flag2 = true;

                        break;
                    }

                    drawData.drawTexture = Main.instance.TilesRenderer.GetTileDrawTexture(drawData.tileCache, x, y);
                    color2 = Main.hslToRgb(0.7f + (float)Math.Sin((float)Math.PI * 2f * Main.GlobalTimeWrappedHourly * 0.16f + x * 0.3f + y * 0.7f) * 0.16f, 1f, 0.5f);
                    color2.A /= 2;
                    color2 *= 0.3f;
                    var num2 = 72;

                    for (var num3 = 0f; num3 < (float)Math.PI * 2f; num3 += (float)Math.PI / 2f) {
                        Main.spriteBatch.Draw(
                            drawData.drawTexture,
                            vector + num3.ToRotationVector2() * 2f,
                            new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY + num2, drawData.tileWidth, drawData.tileHeight),
                            color2,
                            0f,
                            Vector2.Zero,
                            1f,
                            SpriteEffects.None,
                            0f
                        );
                    }

                    color2 = new Color(255, 255, 255, 100);

                    break;
                }
            }

            if (!flag2) {
                if (drawData.tileCache.slope() == 0 && !drawData.tileCache.halfBrick()) {
                    Main.spriteBatch.Draw(drawData.drawTexture, vector, new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY, drawData.tileWidth, drawData.tileHeight), color2, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                }
                else if (drawData.tileCache.halfBrick()) {
                    Main.spriteBatch.Draw(drawData.drawTexture, vector, rectangle, color2, 0f, TileDrawing._zero, 1f, SpriteEffects.None, 0f);
                }
                else if (TileID.Sets.Platforms[drawData.tileCache.type]) {
                    Main.spriteBatch.Draw(drawData.drawTexture, vector, rectangle, color2, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);

                    if (drawData.tileCache.slope() == 1 && Main.tile[x + 1, y + 1].active() && Main.tileSolid[Main.tile[x + 1, y + 1].type] && Main.tile[x + 1, y + 1].slope() != 2 && !Main.tile[x + 1, y + 1].halfBrick() && (!Main.tile[x, y + 1].active() || Main.tile[x, y + 1].blockType() != 0 && Main.tile[x, y + 1].blockType() != 5 || !TileID.Sets.BlocksStairs[Main.tile[x, y + 1].type] && !TileID.Sets.BlocksStairsAbove[Main.tile[x, y + 1].type])) {
                        Rectangle value = new(198, drawData.tileFrameY, 16, 16);

                        if (TileID.Sets.Platforms[Main.tile[x + 1, y + 1].type] && Main.tile[x + 1, y + 1].slope() == 0) {
                            value.X = 324;
                        }

                        Main.spriteBatch.Draw(drawData.drawTexture, vector + new Vector2(0f, 16f), value, color2, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
                    }
                    else if (drawData.tileCache.slope() == 2 && Main.tile[x - 1, y + 1].active() && Main.tileSolid[Main.tile[x - 1, y + 1].type] && Main.tile[x - 1, y + 1].slope() != 1 && !Main.tile[x - 1, y + 1].halfBrick() && (!Main.tile[x, y + 1].active() || Main.tile[x, y + 1].blockType() != 0 && Main.tile[x, y + 1].blockType() != 4 || !TileID.Sets.BlocksStairs[Main.tile[x, y + 1].type] && !TileID.Sets.BlocksStairsAbove[Main.tile[x, y + 1].type])) {
                        Rectangle value2 = new (162, drawData.tileFrameY, 16, 16);

                        if (TileID.Sets.Platforms[Main.tile[x - 1, y + 1].type] && Main.tile[x - 1, y + 1].slope() == 0) {
                            value2.X = 306;
                        }

                        Main.spriteBatch.Draw(drawData.drawTexture, vector + new Vector2(0f, 16f), value2, color2, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
                    }
                }
                else if (TileID.Sets.HasSlopeFrames[drawData.tileCache.type]) {
                    Main.spriteBatch.Draw(drawData.drawTexture, vector, new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY, 16, 16), color2, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
                }
                else {
                    int num4 = drawData.tileCache.slope();
                    var num5 = 2;

                    for (var i = 0; i < 8; i++) {
                        var num6 = i * -2;
                        var num7 = 16 - i * 2;
                        var num8 = 16 - num7;
                        int num9;

                        switch (num4) {
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

                    var num10 = num4 <= 2 ? 14 : 0;
                    Main.spriteBatch.Draw(drawData.drawTexture, vector + new Vector2(0f, num10), new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY + num10, 16, 2), color2, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
                }
            }

            if (drawData.glowTexture != null) {
                var position = new Vector2(x * 16 - (int)screenPosition.X - (drawData.tileWidth - 16f) / 2f, y * 16 - (int)screenPosition.Y + drawData.tileTop) + screenOffset;

                if (TileID.Sets.Platforms[drawData.typeCache]) {
                    position = vector;
                }

                Main.spriteBatch.Draw(drawData.glowTexture, position, drawData.glowSourceRect, drawData.glowColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
            }

            if (highlightTexture != null) {
                Rectangle sourceRectangle = new(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY, drawData.tileWidth, drawData.tileHeight);
                const int num11 = 0;
                const int num13 = 0;

                Main.spriteBatch.Draw(
                    highlightTexture,
                    new Vector2(x * 16 - (int)screenPosition.X - (drawData.tileWidth - 16f) / 2f + num11, y * 16 - (int)screenPosition.Y + drawData.tileTop + num13) + screenOffset,
                    sourceRectangle,
                    highlightColor,
                    0f,
                    TileDrawing._zero,
                    1f,
                    drawData.tileSpriteEffect,
                    0f
                );
            }
        }
    }

    private static void DrawBasicTile(bool vanilla, FnaVector2 screenPosition, FnaVector2 screenOffset, int tileX, int tileY, TileDrawInfo drawData, Rectangle normalTileRect, FnaVector2 normalTilePosition) {
        var td = Main.instance.TilesRenderer;

        Tile tile;

        if (TileID.Sets.Platforms[drawData.typeCache] && WorldGen.IsRope(tileX, tileY) && Main.tile[tileX, tileY - 1] != null) {
            tile = Main.tile[tileX, tileY - 1];
            _ = ref tile.type;
            var y = (tileY + tileX) % 3 * 18;
            var tileDrawTexture = td.GetTileDrawTexture(Main.tile[tileX, tileY - 1], tileX, tileY);

            if (tileDrawTexture != null) {
                Main.spriteBatch.Draw(tileDrawTexture, new FnaVector2(tileX * 16 - (int)screenPosition.X, tileY * 16 - (int)screenPosition.Y) + screenOffset, new Rectangle(90, y, 16, 16), drawData.tileLight, 0f, default, 1f, drawData.tileSpriteEffect, 0f);
            }
        }

        if (drawData.tileCache.slope() > 0) {
            if (TileID.Sets.Platforms[drawData.tileCache.type]) {
                Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, normalTileRect, drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);

                if (drawData.tileCache.slope() == 1) {
                    tile = Main.tile[tileX + 1, tileY + 1];

                    if (tile.active()) {
                        var tileSolid = Main.tileSolid;
                        tile = Main.tile[tileX + 1, tileY + 1];

                        if (tileSolid[tile.type]) {
                            tile = Main.tile[tileX + 1, tileY + 1];

                            if (tile.slope() != 2) {
                                tile = Main.tile[tileX + 1, tileY + 1];

                                if (!tile.halfBrick()) {
                                    tile = Main.tile[tileX, tileY + 1];

                                    if (!tile.active()) {
                                        goto IL_0269;
                                    }

                                    tile = Main.tile[tileX, tileY + 1];

                                    if (tile.blockType() != 0) {
                                        tile = Main.tile[tileX, tileY + 1];

                                        if (tile.blockType() != 5) {
                                            goto IL_0269;
                                        }
                                    }

                                    var blocksStairs = TileID.Sets.BlocksStairs;
                                    tile = Main.tile[tileX, tileY + 1];

                                    if (!blocksStairs[tile.type]) {
                                        var blocksStairsAbove = TileID.Sets.BlocksStairsAbove;
                                        tile = Main.tile[tileX, tileY + 1];

                                        if (!blocksStairsAbove[tile.type]) {
                                            goto IL_0269;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (drawData.tileCache.slope() != 2) {
                    return;
                }

                tile = Main.tile[tileX - 1, tileY + 1];

                if (!tile.active()) {
                    return;
                }

                var tileSolid2 = Main.tileSolid;
                tile = Main.tile[tileX - 1, tileY + 1];

                if (!tileSolid2[tile.type]) {
                    return;
                }

                tile = Main.tile[tileX - 1, tileY + 1];

                if (tile.slope() == 1) {
                    return;
                }

                tile = Main.tile[tileX - 1, tileY + 1];

                if (tile.halfBrick()) {
                    return;
                }

                tile = Main.tile[tileX, tileY + 1];

                if (tile.active()) {
                    tile = Main.tile[tileX, tileY + 1];

                    if (tile.blockType() != 0) {
                        tile = Main.tile[tileX, tileY + 1];

                        if (tile.blockType() != 4) {
                            goto IL_043e;
                        }
                    }

                    var blocksStairs2 = TileID.Sets.BlocksStairs;
                    tile = Main.tile[tileX, tileY + 1];

                    if (blocksStairs2[tile.type]) {
                        return;
                    }

                    var blocksStairsAbove2 = TileID.Sets.BlocksStairsAbove;
                    tile = Main.tile[tileX, tileY + 1];

                    if (blocksStairsAbove2[tile.type]) {
                        return;
                    }
                }

                goto IL_043e;
            }

            if (TileID.Sets.HasSlopeFrames[drawData.tileCache.type]) {
                Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY, 16, 16), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);

                return;
            }

            int num = drawData.tileCache.slope();
            var num2 = 2;

            for (var i = 0; i < 8; i++) {
                var num3 = i * -2;
                var num4 = 16 - i * 2;
                var num5 = 16 - num4;
                int num6;

                switch (num) {
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

                Main.spriteBatch.Draw(
                    drawData.drawTexture,
                    normalTilePosition + new FnaVector2(num6, i * num2 + num3),
                    new Rectangle(drawData.tileFrameX + drawData.addFrX + num6, drawData.tileFrameY + drawData.addFrY + num5, num2, num4),
                    drawData.finalColor,
                    0f,
                    TileDrawing._zero,
                    1f,
                    drawData.tileSpriteEffect,
                    0f
                );
            }

            var num7 = num <= 2 ? 14 : 0;
            Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition + new FnaVector2(0f, num7), new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY + num7, 16, 2), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);

            return;
        }

        if (!TileID.Sets.Platforms[drawData.typeCache] && !TileID.Sets.IgnoresNearbyHalfbricksWhenDrawn[drawData.typeCache] && td._tileSolid[drawData.typeCache] && !TileID.Sets.NotReallySolid[drawData.typeCache] && !drawData.tileCache.halfBrick()) {
            if (tileX - 1 < 0) {
                return;
            }

            tile = Main.tile[tileX - 1, tileY];

            if (!tile.halfBrick()) {
                if (tileX + 1 >= Main.tile.Width) {
                    return;
                }

                tile = Main.tile[tileX + 1, tileY];

                if (!tile.halfBrick()) {
                    goto IL_0cc9;
                }
            }

            tile = Main.tile[tileX - 1, tileY];

            if (tile.halfBrick()) {
                tile = Main.tile[tileX + 1, tileY];

                if (tile.halfBrick()) {
                    Main.spriteBatch.Draw(
                        drawData.drawTexture,
                        normalTilePosition + new FnaVector2(0f, 8f),
                        new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.addFrY + drawData.tileFrameY + 8, drawData.tileWidth, 8),
                        drawData.finalColor,
                        0f,
                        TileDrawing._zero,
                        1f,
                        drawData.tileSpriteEffect,
                        0f
                    );

                    Rectangle value = new(126 + drawData.addFrX, drawData.addFrY, 16, 8);
                    tile = Main.tile[tileX, tileY - 1];

                    if (tile.active()) {
                        tile = Main.tile[tileX, tileY - 1];

                        if (!tile.bottomSlope()) {
                            tile = Main.tile[tileX, tileY - 1];

                            if (tile.type == drawData.typeCache) {
                                value = new Rectangle(90 + drawData.addFrX, drawData.addFrY, 16, 8);
                            }
                        }
                    }

                    Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, value, drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);

                    return;
                }
            }

            tile = Main.tile[tileX - 1, tileY];

            if (tile.halfBrick()) {
                var num8 = 4;

                if (TileID.Sets.AllBlocksWithSmoothBordersToResolveHalfBlockIssue[drawData.typeCache]) {
                    num8 = 2;
                }

                Main.spriteBatch.Draw(
                    drawData.drawTexture,
                    normalTilePosition + new FnaVector2(0f, 8f),
                    new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.addFrY + drawData.tileFrameY + 8, drawData.tileWidth, 8),
                    drawData.finalColor,
                    0f,
                    TileDrawing._zero,
                    1f,
                    drawData.tileSpriteEffect,
                    0f
                );

                Main.spriteBatch.Draw(
                    drawData.drawTexture,
                    normalTilePosition + new FnaVector2(num8, 0f),
                    new Rectangle(drawData.tileFrameX + num8 + drawData.addFrX, drawData.addFrY + drawData.tileFrameY, drawData.tileWidth - num8, drawData.tileHeight),
                    drawData.finalColor,
                    0f,
                    TileDrawing._zero,
                    1f,
                    drawData.tileSpriteEffect,
                    0f
                );

                Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, new Rectangle(144 + drawData.addFrX, drawData.addFrY, num8, 8), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);

                if (num8 == 2) {
                    Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, new Rectangle(148 + drawData.addFrX, drawData.addFrY, 2, 2), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
                }

                return;
            }

            tile = Main.tile[tileX + 1, tileY];

            if (tile.halfBrick()) {
                var num9 = 4;

                if (TileID.Sets.AllBlocksWithSmoothBordersToResolveHalfBlockIssue[drawData.typeCache]) {
                    num9 = 2;
                }

                Main.spriteBatch.Draw(
                    drawData.drawTexture,
                    normalTilePosition + new FnaVector2(0f, 8f),
                    new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.addFrY + drawData.tileFrameY + 8, drawData.tileWidth, 8),
                    drawData.finalColor,
                    0f,
                    TileDrawing._zero,
                    1f,
                    drawData.tileSpriteEffect,
                    0f
                );

                Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.addFrY + drawData.tileFrameY, drawData.tileWidth - num9, drawData.tileHeight), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
                Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition + new FnaVector2(16 - num9, 0f), new Rectangle(144 + (16 - num9), 0, num9, 8), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);

                if (num9 == 2) {
                    Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition + new FnaVector2(14f, 0f), new Rectangle(156, 0, 2, 2), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
                }
            }

            return;
        }

        goto IL_0cc9;

        IL_043e:
        Rectangle value2 = new(162, drawData.tileFrameY, 16, 16);
        var platforms = TileID.Sets.Platforms;
        tile = Main.tile[tileX - 1, tileY + 1];

        if (platforms[tile.type]) {
            tile = Main.tile[tileX - 1, tileY + 1];

            if (tile.slope() == 0) {
                value2.X = 306;
            }
        }

        Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition + new FnaVector2(0f, 16f), value2, drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);

        return;

        IL_0cc9:

        if (Lighting.NotRetro && td._tileSolid[drawData.typeCache] && !drawData.tileCache.halfBrick() && !TileID.Sets.DontDrawTileSliced[drawData.tileCache.type]) {
            if (vanilla) {
                td.DrawSingleTile_SlicedBlock(normalTilePosition, tileX, tileY, drawData);
            }
            else {
                Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY, drawData.tileWidth, drawData.tileHeight), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
            }

            return;
        }

        if (drawData.halfBrickHeight != 8) {
            goto IL_0e81;
        }

        tile = Main.tile[tileX, tileY + 1];

        if (tile.active()) {
            var tileSolid3 = td._tileSolid;
            tile = Main.tile[tileX, tileY + 1];

            if (tileSolid3[tile.type]) {
                tile = Main.tile[tileX, tileY + 1];

                if (!tile.halfBrick()) {
                    goto IL_0e81;
                }
            }
        }

        if (TileID.Sets.Platforms[drawData.typeCache]) {
            Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, normalTileRect, drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
        }
        else {
            Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, normalTileRect.Modified(0, 0, 0, -4), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
            Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition + new FnaVector2(0f, 4f), new Rectangle(144 + drawData.addFrX, 66 + drawData.addFrY, drawData.tileWidth, 4), drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
        }

        goto IL_101e;

        IL_0269:
        Rectangle value3 = new(198, drawData.tileFrameY, 16, 16);
        var platforms2 = TileID.Sets.Platforms;
        tile = Main.tile[tileX + 1, tileY + 1];

        if (platforms2[tile.type]) {
            tile = Main.tile[tileX + 1, tileY + 1];

            if (tile.slope() == 0) {
                value3.X = 324;
            }
        }

        Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition + new FnaVector2(0f, 16f), value3, drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);

        return;

        IL_101e:
        td.DrawSingleTile_Flames(screenPosition, screenOffset, tileX, tileY, drawData);

        return;

        IL_0e81:

        if (TileID.Sets.CritterCageLidStyle[drawData.typeCache] >= 0) {
            var num10 = TileID.Sets.CritterCageLidStyle[drawData.typeCache];

            if (num10 < 3 && normalTileRect.Y % 54 == 0 || num10 >= 3 && normalTileRect.Y % 36 == 0) {
                var position = normalTilePosition;
                position.Y += 8f;
                var value4 = normalTileRect;
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
            else {
                Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, normalTileRect, drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
            }
        }
        else {
            Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition, normalTileRect, drawData.finalColor, 0f, TileDrawing._zero, 1f, drawData.tileSpriteEffect, 0f);
        }

        goto IL_101e;
    }

    private static void AddSpecialPointsForTile(Tile tile, int x, int y) {
        var type = tile.type;
        var frameX = tile.frameX;
        var frameY = tile.frameY;

        switch (type) {
            case 52:
            case 62:
            case 115:
            case 205:
            case 382:
            case 528:
            case 636:
            case 638:
                if (true) {
                    CrawlToTopOfVineAndAddSpecialPoint(y, x);
                }

                break;

            case 549:
                if (true) {
                    Main.instance.TilesRenderer.CrawlToBottomOfReverseVineAndAddSpecialPoint(y, x);
                }

                break;

            case 34:
                if (frameX % 54 == 0 && frameY % 54 == 0) {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 454:
                if (frameX % 72 == 0 && frameY % 54 == 0) {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 42:
            case 270:
            case 271:
            case 572:
            case 581:
            case 660:
                if (frameX % 18 == 0 && frameY % 36 == 0) {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 91:
                if (frameX % 18 == 0 && frameY % 54 == 0) {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 95:
            case 126:
            case 444:
                if (frameX % 36 == 0 && frameY % 36 == 0) {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 465:
            case 591:
            case 592:
                if (frameX % 36 == 0 && frameY % 54 == 0) {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileVine);
                }

                break;

            case 27:
                if (frameX % 36 == 0 && frameY == 0) {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 236:
            case 238:
                if (frameX % 36 == 0 && frameY == 0) {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 233:
                switch (frameY) {
                    case 0 when frameX % 54 == 0:
                    case 34 when frameX % 36 == 0:
                        Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);

                        break;
                }

                break;

            case 652:
                if (frameX % 36 == 0) {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 651:
                if (frameX % 54 == 0) {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 530:
                if (frameX < 270) {
                    if (frameX % 54 == 0 && frameY == 0) {
                        Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                    }
                }

                break;

            case 485:
            case 489:
            case 490:
                if (frameY == 0 && frameX % 36 == 0) {
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
                if (frameY == 0 && frameX % 36 == 0) {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 493:
                if (frameY == 0 && frameX % 18 == 0) {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MultiTileGrass);
                }

                break;

            case 519:
                if (frameX / 18 <= 4) {
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
                if (frameX == 18 && frameY == 18) {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.VoidLens);
                }

                break;

            case 597:
                if (frameX % 54 == 0 && frameY == 0) {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.TeleportationPylon);
                }

                break;

            case 617:
                if (frameX % 54 == 0 && frameY % 72 == 0) {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.MasterTrophy);
                }

                break;

            case 184:
                if (true) {
                    Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.AnyDirectionalGrass);
                }

                break;

            default:
                if (Main.instance.TilesRenderer.ShouldSwayInWind(x, y, tile)) {
                    if (true) {
                        Main.instance.TilesRenderer.AddSpecialPoint(x, y, TileDrawing.TileCounterType.WindyGrass);
                    }
                }

                break;
        }
    }

    private static bool IsDrawnBySpecialPoint(Tile tile, int x, int y) {
        var type = tile.type;

        switch (type) {
            case 52:
            case 62:
            case 115:
            case 205:
            case 382:
            case 528:
            case 636:
            case 638:
                return true;

            case 549:
                return true;

            case 34:
                return true;

            case 454:
                return true;

            case 42:
            case 270:
            case 271:
            case 572:
            case 581:
            case 660:
                return true;

            case 91:
                return true;

            case 95:
            case 126:
            case 444:
                return true;

            case 465:
            case 591:
            case 592:
                return true;

            case 27:
                return true;

            case 236:
            case 238:
                return true;

            case 233:
                return true;

            case 652:
                return true;

            case 651:
                return true;

            case 530:
                return true;

            case 485:
            case 489:
            case 490:
                return true;

            case 521:
            case 522:
            case 523:
            case 524:
            case 525:
            case 526:
            case 527:
                return true;

            case 493:
                return true;

            case 519:
                return true;

            case 184:
                return true;

            default:
                return Main.instance.TilesRenderer.ShouldSwayInWind(x, y, tile);
        }
    }

    /// <summary>
    ///     Draws a single wall.
    /// </summary>
    /// <param name="vanilla"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="screenPosition"></param>
    /// <remarks>
    ///     This is extracted from <see cref="WallDrawing.DrawWalls"/>.
    /// </remarks>
    public static void DrawSingleWall(bool vanilla, int x, int y, FnaVector2 screenPosition) {
        var wd = Main.instance.WallsRenderer;
        wd._tileArray = Main.tile;
        var wallBlend = Main.wallBlend;
        Rectangle value = new(0, 0, 32, 32);
        var tile = wd._tileArray[x, y];
        var wall = tile.wall;

        // WallDrawing.FullTile accesses the tiles at (i - 1) and (i + 1)
        if (x - 1 < 0 || x + 1 >= wd._tileArray.Width) {
            return;
        }

        // Don't check FullTile because we want walls to draw behind tiles as
        // they lie on different chunk layers.
        if (wall <= 0 || vanilla && wd.FullTile(x, y) || wall == 318 && !wd._shouldShowInvisibleWalls || tile.invisibleWall() && !wd._shouldShowInvisibleWalls) {
            return;
        }

        // In tML and vanilla, certain walls/tiles automatically include a draw offset of Main.offScreenRange if drawToScreen is false.
        // This is because of the buffer zone around the world, although the offset is not needed with RTs.
        // This fixes the behaviour of this offset with Nitrate, by forcing it to always be zero in line with Nitrate's new drawing system.
        bool oldDrawToScreen = Main.drawToScreen;
        Main.drawToScreen = true;

        if (!vanilla || WallLoader.PreDraw(x, y, wall, Main.spriteBatch)) {
            var color = vanilla ? Lighting.GetColor(x, y) : Color.White;

            if (vanilla) {
                if (tile.fullbrightWall()) {
                    color = Color.White;
                }

                if (wall == 318) {
                    color = Color.White;
                }

                if (color is { R: 0, G: 0, B: 0 } && y < Main.UnderworldLayer) {
                    return;
                }
            }

            Main.instance.LoadWall(wall);

            value.X = tile.wallFrameX();
            value.Y = tile.wallFrameY() + Main.wallFrame[wall] * 180;

            var wall2 = tile.wall;

            if ((uint)(wall2 - 242) <= 1u) {
                var num11 = 20;
                var num12 = (Main.wallFrameCounter[wall] + x * 11 + y * 27) % (num11 * 8);
                value.Y = tile.wallFrameY() + 180 * (num12 / num11);
            }

            VertexColors vertices = default;

            if ((!vanilla || Lighting.NotRetro) && !Main.wallLight[wall] && tile.wall != 241 && (tile.wall < 88 || tile.wall > 93) && !WorldGen.SolidTile(tile)) {
                var tileDrawTexture = wd.GetTileDrawTexture(tile, x, y);

                if (tile.wall == 346) {
                    vertices.TopRightColor = vertices.TopLeftColor = vertices.BottomRightColor = vertices.BottomLeftColor = new Color((byte)Main.DiscoR, (byte)Main.DiscoG, (byte)Main.DiscoB);
                }
                else if (tile.wall == 44) {
                    vertices.TopRightColor = vertices.TopLeftColor = vertices.BottomRightColor = vertices.BottomLeftColor = new Color((byte)Main.DiscoR, (byte)Main.DiscoG, (byte)Main.DiscoB);
                }
                else {
                    Lighting.GetCornerColors(x, y, out vertices);

                    if ((uint)(tile.wall - 341) <= 4u) {
                        wd.LerpVertexColorsWithColor(ref vertices, Color.White, 0.5f);
                    }

                    if (tile.fullbrightWall()) {
                        vertices = WallDrawing._glowPaintColors;
                    }
                }

                if (!vanilla) {
                    vertices = new VertexColors(Color.White);
                }

                Main.tileBatch.Draw(tileDrawTexture, new Vector2(x * 16 - (int)screenPosition.X - 8, y * 16 - (int)screenPosition.Y - 8), value, vertices, Vector2.Zero, 1f, SpriteEffects.None);
            }
            else {
                var color2 = color;

                if (wall == 44 || wall == 346) {
                    color2 = new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB);
                }

                if ((uint)(wall - 341) <= 4u) {
                    color2 = Color.Lerp(color2, Color.White, 0.5f);
                }

                if (!vanilla) {
                    color2 = Color.White;
                }

                var tileDrawTexture2 = wd.GetTileDrawTexture(tile, x, y);
                Main.spriteBatch.Draw(tileDrawTexture2, new Vector2(x * 16 - (int)screenPosition.X - 8, y * 16 - (int)screenPosition.Y - 8), value, color2, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }

            var gfxQuality = Main.gfxQuality;
            var num = (int)(120f * (1f - gfxQuality) + 40f * gfxQuality);
            var num2 = (int)(num * 0.4f);
            var num3 = (int)(num * 0.35f);
            var num4 = (int)(num * 0.3f);

            if (!vanilla) {
                color = Color.White;
            }

            if (color.R > num2 || color.G > num3 || color.B > num4) {
                var num13 = wd._tileArray[x - 1, y].wall > 0 && wallBlend[wd._tileArray[x - 1, y].wall] != wallBlend[tile.wall];
                var flag = wd._tileArray[x + 1, y].wall > 0 && wallBlend[wd._tileArray[x + 1, y].wall] != wallBlend[tile.wall];
                var flag2 = wd._tileArray[x, y - 1].wall > 0 && wallBlend[wd._tileArray[x, y - 1].wall] != wallBlend[tile.wall];
                var flag3 = wd._tileArray[x, y + 1].wall > 0 && wallBlend[wd._tileArray[x, y + 1].wall] != wallBlend[tile.wall];

                if (num13) {
                    Main.spriteBatch.Draw(TextureAssets.WallOutline.Value, new Vector2(x * 16 - (int)screenPosition.X, y * 16 - (int)screenPosition.Y), new Rectangle(0, 0, 2, 16), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                }

                if (flag) {
                    Main.spriteBatch.Draw(TextureAssets.WallOutline.Value, new Vector2(x * 16 - (int)screenPosition.X + 14, y * 16 - (int)screenPosition.Y), new Rectangle(14, 0, 2, 16), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                }

                if (flag2) {
                    Main.spriteBatch.Draw(TextureAssets.WallOutline.Value, new Vector2(x * 16 - (int)screenPosition.X, y * 16 - (int)screenPosition.Y), new Rectangle(0, 0, 16, 2), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                }

                if (flag3) {
                    Main.spriteBatch.Draw(TextureAssets.WallOutline.Value, new Vector2(x * 16 - (int)screenPosition.X, y * 16 - (int)screenPosition.Y + 14), new Rectangle(0, 14, 16, 2), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                }
            }
        }

        if (!vanilla) {
            WallLoader.PostDraw(x, y, wall, Main.spriteBatch);
        }

        Main.drawToScreen = oldDrawToScreen;
    }

    public static void DrawLiquidBehindTiles() {
        var unscaledPosition = Main.Camera.UnscaledPosition;
        var vector = Vector2.Zero;
        Main.instance.TilesRenderer.GetScreenDrawArea(unscaledPosition, vector + (Main.Camera.UnscaledPosition - Main.Camera.ScaledPosition), out var firstTileX, out var lastTileX, out var firstTileY, out var lastTileY);

        for (var i = firstTileY; i < lastTileY + 4; i++) {
            for (var j = firstTileX - 2; j < lastTileX + 2; j++) {
                var tile = Main.tile[j, i];

                if (tile.HasTile) {
                    Main.instance.TilesRenderer.DrawTile_LiquidBehindTile(true, false, -1, unscaledPosition, vector, j, i, tile);
                }
            }
        }
    }

    private static void CrawlToTopOfVineAndAddSpecialPoint(int j, int i) {
        var y = j;

        for (var num = j - 1; num > 0; num--) {
            var tile = Main.tile[i, num];

            if (WorldGen.SolidTile(i, num) || !tile.active()) {
                y = num + 1;

                break;
            }
        }

        var item = new Point(i, y);

        if (!Main.instance.TilesRenderer._vineRootsPositions.Contains(item)) {
            Main.instance.TilesRenderer._vineRootsPositions.Add(item);
            Main.instance.TilesRenderer.AddSpecialPoint(i, y, TileDrawing.TileCounterType.Vine);
        }
    }
}
