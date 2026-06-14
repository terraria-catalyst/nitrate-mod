using System;
using System.Diagnostics;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nitrate.API.Listeners;
using Nitrate.API.Tiles;
using Nitrate.Core;
using Nitrate.Utilities;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.Liquid;
using Terraria.Graphics.Capture;

namespace Nitrate.Optimizations;

internal sealed class TileChunkCollection : ChunkCollection
{
    private DynamicTileVisibilityListener.VisibilityType dynamicVisibilityTypes;

    public TileChunkCollection()
    {
        DynamicTileVisibilityListener.OnVisibilityChange += TrackTileVisibility;
    }

    public bool SolidLayer { get; init; }

    public override void PopulateChunk(Point key)
    {
        var chunk = Loaded[key];
        var target = chunk.RenderTarget;

        chunk.AnimatedPoints.Clear();

        using (target.Scope(clearColor: Color.Transparent))
        {
            Vector2 chunkPositionWorld = new(key.X * ChunkSystem.ChunkSize, key.Y * ChunkSystem.ChunkSize);

            var sizeTiles = ChunkSystem.ChunkSize / 16;

            Point chunkPositionTile = new((int)chunkPositionWorld.X / 16, (int)chunkPositionWorld.Y / 16);

            Main.tileBatch.Begin();

            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone
            );

            for (var i = 0; i < sizeTiles; i++)
            {
                for (var j = 0; j < sizeTiles; j++)
                {
                    var tileX = chunkPositionTile.X + i;
                    var tileY = chunkPositionTile.Y + j;

                    if (!WorldGen.InWorld(tileX, tileY))
                    {
                        continue;
                    }

                    var tile = Framing.GetTileSafely(tileX, tileY);

                    if (tile.frameX == -1)
                    {
                        if (!FailedPopulations.TryAdd(key, 0))
                        {
                            FailedPopulations[key]++;
                        }
                    }

                    if (FailedPopulations.TryGetValue(key, out var value) && value < 6 && tile.frameX == -1)
                    {
                        Main.tileBatch.End();
                        Main.spriteBatch.End();
                        NeedsRePopulating.Add(key);
                        return;
                    }

                    if (!tile.HasTile || Main.instance.TilesRenderer.IsTileDrawLayerSolid(tile.type) != SolidLayer)
                    {
                        continue;
                    }

                    if (AnimatedTileRegistry.IsTilePossiblyAnimated(tile.TileType))
                    {
                        chunk.AnimatedPoints.Add(new AnimatedPoint(tileX, tileY, AnimatedPointType.AnimatedTile));
                    }
                    /*else if (IsTileDynamic(tile, tileX, tileY))
                    {
                        chunk.AnimatedPoints.Add(new AnimatedPoint(tileX, tileY, AnimatedPointType.AnimatedTile));
                    }*/
                    else
                    {
                        ModifiedTileDrawing.DrawSingleTile(false, SolidLayer, tileX, tileY, chunkPositionWorld);
                    }
                }
            }

            Main.tileBatch.End();
            Main.spriteBatch.End();
        }
    }

    public override void DrawChunksToChunkTarget(GraphicsDevice device)
    {
        if (ScreenTarget is null)
        {
            return;
        }

        using (ScreenTarget.Scope(clearColor: Color.Transparent))
        {
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null
            );

            var screenPosition = Main.screenPosition;

            Rectangle screenArea = new((int)screenPosition.X, (int)screenPosition.Y, Main.screenWidth, Main.screenHeight);
            {
                screenArea.Inflate(40 * 16, 40 * 16);
            }

            foreach (var key in Loaded.Keys)
            {
                var chunk = Loaded[key];
                var target = chunk.RenderTarget;

                Rectangle chunkArea = new(key.X * ChunkSystem.ChunkSize, key.Y * ChunkSystem.ChunkSize, target.Width, target.Height);

                if (!chunkArea.Intersects(screenArea))
                {
                    continue;
                }

                // This should never happen, something catastrophic happened if it did.
                // The check here is because rendering disposed targets generally has strange behaviour and doesn't always throw exceptions.
                // Therefore this check needs to be made as it's more robust.
                if (target.IsDisposed)
                {
                    throw new Exception("Attempted to render a disposed chunk.");
                }

                Main.spriteBatch.Draw(target, new Vector2(chunkArea.X, chunkArea.Y) - screenPosition + new Vector2(Main.offScreenRange), Color.White);
            }

            Main.spriteBatch.End();
        }
    }

    public void DoRenderTiles(GraphicsDevice graphicsDevice, RenderTarget2D? screenSizeLightingBuffer, RenderTarget2D? screenSizeOverrideBuffer, WrapperShaderData<Assets.Effects.LightMapRenderer.Parameters> lightMapShader)
    {
        var stopwatch = Stopwatch.StartNew();

        var unscaledPosition = Main.Camera.UnscaledPosition;
        var offscreenRange = new Vector2(Main.drawToScreen ? 0 : Main.offScreenRange);

        if (!SolidLayer)
        {
            Main.critterCage = true;
        }

        Main.instance.TilesRenderer.EnsureWindGridSize();
        Main.instance.TilesRenderer.ClearLegacyCachedDraws();

        Main.instance.TilesRenderer.ClearCachedTileDraws(SolidLayer);

        var martianWhite = (byte)(100f + 150f * Main.martianLight);
        Main.instance.TilesRenderer._martianGlow = new Color(martianWhite, martianWhite, martianWhite, 0);

        if (SolidLayer && !LiquidEdgeRenderer.Active)
        {
            ModifiedTileDrawing.DrawLiquidBehindTiles();
        }

        Main.tileBatch.End();
        using (Main.spriteBatch.Scope())
        {
            RemoveOutOfBoundsAndPopulate();
            DrawChunksToChunkTarget(graphicsDevice);
            RenderChunksWithLighting(screenSizeLightingBuffer, screenSizeOverrideBuffer, lightMapShader, offscreenRange);
        }
        Main.tileBatch.Begin();

        foreach (var key in Loaded.Keys)
        {
            var chunk = Loaded[key];

            foreach (var tilePoint in chunk.AnimatedPoints)
            {
                var tile = Framing.GetTileSafely(tilePoint.X, tilePoint.Y);

                if (tilePoint.Type == AnimatedPointType.AnimatedTile)
                {
                    if (!tile.HasTile)
                    {
                        continue;
                    }

                    if (!TextureAssets.Tile[tile.type].IsLoaded)
                    {
                        Main.instance.LoadTiles(tile.type);
                    }

                    ModifiedTileDrawing.DrawSingleTile(true, SolidLayer, tilePoint.X, tilePoint.Y, Main.screenPosition - offscreenRange);
                }
            }
        }

        if (SolidLayer)
        {
            Main.instance.DrawTileCracks(1, Main.LocalPlayer.hitReplace);
            Main.instance.DrawTileCracks(1, Main.LocalPlayer.hitTile);
        }

        Main.instance.TilesRenderer.DrawSpecialTilesLegacy(unscaledPosition, offscreenRange);

        if (TileObject.objectPreview.Active && Main.LocalPlayer.cursorItemIconEnabled && Main.placementPreview && !CaptureManager.Instance.Active)
        {
            Main.instance.LoadTiles(TileObject.objectPreview.Type);
            TileObject.DrawPreview(Main.spriteBatch, TileObject.objectPreview, unscaledPosition - offscreenRange);
        }

        TimeLogger.DrawTime(SolidLayer ? 0 : 1, stopwatch.Elapsed.TotalMilliseconds);
    }

    private void TrackTileVisibility(DynamicTileVisibilityListener.VisibilityType types)
    {
        dynamicVisibilityTypes = types;
    }

    public bool TryGetDynamicLighting(int x, int y, Color lightColor, ref Color color)
    {
        if (!WorldGen.InWorld(x, y))
        {
            return false;
        }

        var tile = Main.tile[x, y];

        if (dynamicVisibilityTypes == 0)
        {
            return false;
        }

        if (dynamicVisibilityTypes.HasFlag(DynamicTileVisibilityListener.VisibilityType.Dangersense))
        {
            if (Main.LocalPlayer.dangerSense && TileDrawing.IsTileDangerous(x, y, Main.LocalPlayer, tile, tile.TileType))
            {
                color = new Color(255, Math.Max(lightColor.G, (byte)50), Math.Max(lightColor.B, (byte)50));

                return true;
            }
        }

        if (dynamicVisibilityTypes.HasFlag(DynamicTileVisibilityListener.VisibilityType.Spelunker))
        {
            if (Main.LocalPlayer.findTreasure && Main.IsTileSpelunkable(x, y, tile.TileType, tile.TileFrameX, tile.TileFrameY))
            {
                color = new Color(Math.Max(lightColor.R, (byte)200), Math.Max(lightColor.G, (byte)170), lightColor.B);

                return true;
            }
        }

        if (dynamicVisibilityTypes.HasFlag(DynamicTileVisibilityListener.VisibilityType.BiomeSight))
        {
            var sightColor = Color.White;

            if (Main.IsTileBiomeSightable(x, y, tile.TileType, tile.TileFrameX, tile.TileFrameY, ref sightColor))
            {
                color = new Color(Math.Max(lightColor.R, sightColor.R), Math.Max(lightColor.G, sightColor.G), Math.Max(lightColor.B, sightColor.B));

                return true;
            }
        }

        return false;
    }
}
