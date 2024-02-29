using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nitrate.API.Listeners;
using Nitrate.API.Tiles;
using Nitrate.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.Capture;
using Terraria.ModLoader;

namespace Nitrate.Optimizations.Tiles;

internal sealed class TileChunkCollection : ChunkCollection {
    public bool SolidLayer { get; init; }

    private DynamicTileVisibilityListener.VisibilityType dynamicVisibilityTypes;

    public TileChunkCollection() {
        DynamicTileVisibilityListener.OnVisibilityChange += TrackTileVisibility;
    }

    public override void PopulateChunk(Point key) {
        var chunk = Loaded[key];
        var target = chunk.RenderTarget;

        chunk.AnimatedPoints.Clear();

        var device = Main.graphics.GraphicsDevice;

        device.SetRenderTarget(target);
        device.Clear(Color.Transparent);

        Vector2 chunkPositionWorld = new(key.X * ChunkSystem.CHUNK_SIZE, key.Y * ChunkSystem.CHUNK_SIZE);

        const int size_tiles = ChunkSystem.CHUNK_SIZE / 16;

        Point chunkPositionTile = new((int)chunkPositionWorld.X / 16, (int)chunkPositionWorld.Y / 16);

        Main.tileBatch.Begin();

        Main.spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone
        );

        for (var i = 0; i < size_tiles; i++) {
            for (var j = 0; j < size_tiles; j++) {
                var tileX = chunkPositionTile.X + i;
                var tileY = chunkPositionTile.Y + j;

                if (!WorldGen.InWorld(tileX, tileY)) {
                    continue;
                }

                var tile = Framing.GetTileSafely(tileX, tileY);
                
                if (tile.frameX == -1) {
                    if (!FailedPopulations.TryAdd(key, 0)) {
                        FailedPopulations[key]++;
                    }
                }

                if (FailedPopulations.TryGetValue(key, out byte value) && value < 6 && tile.frameX == -1) {
                    StopRender(device);
                    NeedsRePopulating.Add(key);
                    return;
                }

                if (!tile.HasTile || Main.instance.TilesRenderer.IsTileDrawLayerSolid(tile.type) != SolidLayer) {
                    continue;
                }

                if (AnimatedTileRegistry.IsTilePossiblyAnimated(tile.TileType)) {
                    chunk.AnimatedPoints.Add(new AnimatedPoint(tileX, tileY, AnimatedPointType.AnimatedTile));
                }
                /*else if (IsTileDynamic(tile, tileX, tileY))
                {
                    chunk.AnimatedPoints.Add(new AnimatedPoint(tileX, tileY, AnimatedPointType.AnimatedTile));
                }*/
                else {
                    ModifiedTileDrawing.DrawSingleTile(false, SolidLayer, tileX, tileY, chunkPositionWorld);
                }
            }
        }
        
        StopRender(device);
    }

    private void StopRender(GraphicsDevice device) {
        Main.tileBatch.End();
        Main.spriteBatch.End();

        device.SetRenderTargets(null);
    }
    
    public override void DrawChunksToChunkTarget(GraphicsDevice device) {
        if (ScreenTarget is null) {
            return;
        }

        var bindings = device.GetRenderTargets();

        foreach (var binding in bindings) {
            ((RenderTarget2D)binding.RenderTarget).RenderTargetUsage = RenderTargetUsage.PreserveContents;
        }

        device.SetRenderTarget(ScreenTarget);
        device.Clear(Color.Transparent);

        Main.spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            null,
            Main.GameViewMatrix.TransformationMatrix
        );

        var screenPosition = Main.screenPosition;

        Rectangle screenArea = new((int)screenPosition.X, (int)screenPosition.Y, Main.screenWidth, Main.screenHeight);

        foreach (var key in Loaded.Keys) {
            var chunk = Loaded[key];
            var target = chunk.RenderTarget;

            Rectangle chunkArea = new(key.X * ChunkSystem.CHUNK_SIZE, key.Y * ChunkSystem.CHUNK_SIZE, target.Width, target.Height);

            if (!chunkArea.Intersects(screenArea)) {
                continue;
            }

            // This should never happen, something catastrophic happened if it did.
            // The check here is because rendering disposed targets generally has strange behaviour and doesn't always throw exceptions.
            // Therefore this check needs to be made as it's more robust.
            if (target.IsDisposed) {
                throw new Exception("Attempted to render a disposed chunk.");
            }

            Main.spriteBatch.Draw(target, new Vector2(chunkArea.X, chunkArea.Y) - screenPosition, Color.White);
        }

        Main.spriteBatch.End();

        device.SetRenderTargets(bindings);
    }

    public void DoRenderTiles(GraphicsDevice graphicsDevice, RenderTarget2D? screenSizeLightingBuffer, RenderTarget2D? screenSizeOverrideBuffer, Lazy<Effect> lightMapRenderer, SpriteBatchUtil.SpriteBatchSnapshot? snapshot) {
        var unscaledPosition = Main.Camera.UnscaledPosition;
        var offscreenRange = Vector2.Zero; /*new(Main.offScreenRange, Main.offScreenRange);*/

        if (!SolidLayer) {
            Main.critterCage = true;
        }

        Main.instance.TilesRenderer.EnsureWindGridSize();
        Main.instance.TilesRenderer.ClearLegacyCachedDraws();

        Main.instance.TilesRenderer.ClearCachedTileDraws(SolidLayer);

        var martianWhite = (byte)(100f + 150f * Main.martianLight);
        Main.instance.TilesRenderer._martianGlow = new Color(martianWhite, martianWhite, martianWhite, 0);

        Main.tileBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        ModifiedTileDrawing.DrawLiquidBehindTiles();
        Main.tileBatch.End();

        DrawChunksToChunkTarget(graphicsDevice);
        RenderChunksWithLighting(screenSizeLightingBuffer, screenSizeOverrideBuffer, lightMapRenderer);

        if (snapshot.HasValue) {
            Main.spriteBatch.BeginWithSnapshot(snapshot.Value);
        }

        foreach (var key in Loaded.Keys) {
            var chunk = Loaded[key];

            foreach (var tilePoint in chunk.AnimatedPoints) {
                var tile = Framing.GetTileSafely(tilePoint.X, tilePoint.Y);

                if (tilePoint.Type == AnimatedPointType.AnimatedTile) {
                    if (!tile.HasTile) {
                        continue;
                    }

                    if (!TextureAssets.Tile[tile.type].IsLoaded) {
                        Main.instance.LoadTiles(tile.type);
                    }

                    ModifiedTileDrawing.DrawSingleTile(true, SolidLayer, tilePoint.X, tilePoint.Y, Main.screenPosition);
                }
            }
        }

        if (SolidLayer) {
            var drawToScreen = Main.drawToScreen;
            Main.drawToScreen = true;
            Main.instance.DrawTileCracks(1, Main.LocalPlayer.hitReplace);
            Main.instance.DrawTileCracks(1, Main.LocalPlayer.hitTile);
            Main.drawToScreen = drawToScreen;
        }

        Main.screenPosition += new Vector2(Main.offScreenRange, Main.offScreenRange);
        Main.instance.TilesRenderer.DrawSpecialTilesLegacy(unscaledPosition, offscreenRange);
        Main.screenPosition -= new Vector2(Main.offScreenRange, Main.offScreenRange);

        if (TileObject.objectPreview.Active && Main.LocalPlayer.cursorItemIconEnabled && Main.placementPreview && !CaptureManager.Instance.Active) {
            Main.instance.LoadTiles(TileObject.objectPreview.Type);
            TileObject.DrawPreview(Main.spriteBatch, TileObject.objectPreview, unscaledPosition - offscreenRange);
        }

        if (snapshot.HasValue) {
            Main.spriteBatch.TryEnd(out _);
        }
    }

    private void TrackTileVisibility(DynamicTileVisibilityListener.VisibilityType types) {
        dynamicVisibilityTypes = types;
    }

    public bool TryGetDynamicLighting(int x, int y, Color lightColor, ref Color color) {
        if (!WorldGen.InWorld(x, y)) {
            return false;
        }

        var tile = Main.tile[x, y];

        if (dynamicVisibilityTypes == 0) {
            return false;
        }

        if (dynamicVisibilityTypes.HasFlag(DynamicTileVisibilityListener.VisibilityType.Dangersense)) {
            if (Main.LocalPlayer.dangerSense && TileDrawing.IsTileDangerous(x, y, Main.LocalPlayer, tile, tile.TileType)) {
                color = new(255, Math.Max(lightColor.G, (byte)50), Math.Max(lightColor.B, (byte)50));

                return true;
            }
        }

        if (dynamicVisibilityTypes.HasFlag(DynamicTileVisibilityListener.VisibilityType.Spelunker)) {
            if (Main.LocalPlayer.findTreasure && Main.IsTileSpelunkable(x, y, tile.TileType, tile.TileFrameX, tile.TileFrameY)) {
                color = new(Math.Max(lightColor.R, (byte)200), Math.Max(lightColor.G, (byte)170), lightColor.B);

                return true;
            }
        }

        if (dynamicVisibilityTypes.HasFlag(DynamicTileVisibilityListener.VisibilityType.BiomeSight)) {
            var sightColor = Color.White;

            if (Main.IsTileBiomeSightable(x, y, tile.TileType, tile.TileFrameX, tile.TileFrameY, ref sightColor)) {
                color = new(Math.Max(lightColor.R, sightColor.R), Math.Max(lightColor.G, sightColor.G), Math.Max(lightColor.B, sightColor.B));

                return true;
            }
        }

        return false;
    }
}
