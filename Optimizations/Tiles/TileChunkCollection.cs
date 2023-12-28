using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nitrate.API.Tiles;
using Nitrate.Utilities;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Capture;
using Terraria.ModLoader;

namespace Nitrate.Optimizations.Tiles;

internal sealed class TileChunkCollection : ChunkCollection
{
    public bool SolidLayer { get; init; }

    public override void PopulateChunk(Point key)
    {
        Chunk chunk = Loaded[key];
        RenderTarget2D target = chunk.RenderTarget;

        GraphicsDevice device = Main.graphics.GraphicsDevice;

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

        for (int i = 0; i < size_tiles; i++)
        {
            for (int j = 0; j < size_tiles; j++)
            {
                int tileX = chunkPositionTile.X + i;
                int tileY = chunkPositionTile.Y + j;

                if (!WorldGen.InWorld(tileX, tileY))
                {
                    continue;
                }

                Tile tile = Framing.GetTileSafely(tileX, tileY);

                if (!tile.HasTile || Main.instance.TilesRenderer.IsTileDrawLayerSolid(tile.type) != SolidLayer)
                {
                    continue;
                }

                if (AnimatedTileRegistry.IsTilePossiblyAnimated(tile.TileType))
                {
                    chunk.AnimatedPoints.Add(new Point(tileX, tileY));
                }
                else
                {
                    ModifiedTileDrawing.DrawSingleTile(false, SolidLayer, tileX, tileY, chunkPositionWorld);
                }
            }
        }

        Main.tileBatch.End();
        Main.spriteBatch.End();

        device.SetRenderTargets(null);
    }

    public override void DrawChunksToChunkTarget(GraphicsDevice device)
    {
        if (ScreenTarget is null)
        {
            return;
        }

        RenderTargetBinding[] bindings = device.GetRenderTargets();

        foreach (RenderTargetBinding binding in bindings)
        {
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

        FnaVector2 screenPosition = Main.screenPosition;

        Rectangle screenArea = new((int)screenPosition.X, (int)screenPosition.Y, Main.screenWidth, Main.screenHeight);

        foreach (Point key in Loaded.Keys)
        {
            Chunk chunk = Loaded[key];
            RenderTarget2D target = chunk.RenderTarget;

            Rectangle chunkArea = new(key.X * ChunkSystem.CHUNK_SIZE, key.Y * ChunkSystem.CHUNK_SIZE, target.Width, target.Height);

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

            Main.spriteBatch.Draw(target, new Vector2(chunkArea.X, chunkArea.Y) - screenPosition, Color.White);
        }

        Main.spriteBatch.End();

        device.SetRenderTargets(bindings);
    }

    public void DoRenderTiles(GraphicsDevice graphicsGraphicsDevice, RenderTarget2D? screenSizeLightingBuffer, Lazy<Effect> lightMapRenderer, SpriteBatchUtil.SpriteBatchSnapshot? snapshot)
    {
        Vector2 unscaledPosition = Main.Camera.UnscaledPosition;
        Vector2 offscreenRange = Vector2.Zero; /*new(Main.offScreenRange, Main.offScreenRange);*/

        if (!SolidLayer)
        {
            Main.critterCage = true;
        }

        Main.instance.TilesRenderer.EnsureWindGridSize();
        Main.instance.TilesRenderer.ClearLegacyCachedDraws();

        byte martianWhite = (byte)(100f + 150f * Main.martianLight);
        Main.instance.TilesRenderer._martianGlow = new Color(martianWhite, martianWhite, martianWhite, 0);

        DrawChunksToChunkTarget(graphicsGraphicsDevice);
        RenderChunksWithLighting(screenSizeLightingBuffer, lightMapRenderer);

        if (snapshot.HasValue)
        {
            Main.spriteBatch.BeginWithSnapshot(snapshot.Value);
        }

        FnaVector2 screenPosition = Main.screenPosition;
        Rectangle screenArea = new((int)screenPosition.X, (int)screenPosition.Y, Main.screenWidth, Main.screenHeight);

        foreach (Point key in Loaded.Keys)
        {
            Chunk chunk = Loaded[key];
            RenderTarget2D target = chunk.RenderTarget;

            Rectangle chunkArea = new(key.X * ChunkSystem.CHUNK_SIZE, key.Y * ChunkSystem.CHUNK_SIZE, target.Width, target.Height);

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

            Main.spriteBatch.Draw(target, new Vector2(chunkArea.X, chunkArea.Y) - screenPosition, Color.White);

            foreach (Point tilePoint in chunk.AnimatedPoints)
            {
                Tile tile = Framing.GetTileSafely(tilePoint);

                if (!tile.HasTile)
                {
                    continue;
                }

                if (!TextureAssets.Tile[tile.type].IsLoaded)
                {
                    Main.instance.LoadTiles(tile.type);
                }

                if (TileLoader.PreDraw(tilePoint.X, tilePoint.Y, tile.type, Main.spriteBatch))
                {
                    // Main.NewText(new Vector2(tilePoint.X * 16, tilePoint.Y * 16));
                    ModifiedTileDrawing.DrawSingleTile(true, SolidLayer, tilePoint.X, tilePoint.Y, Main.screenPosition);
                }

                TileLoader.PostDraw(tilePoint.X, tilePoint.Y, tile.type, Main.spriteBatch);
            }
        }

        if (SolidLayer)
        {
            bool drawToScreen = Main.drawToScreen;
            Main.drawToScreen = true;
            Main.instance.DrawTileCracks(1, Main.LocalPlayer.hitReplace);
            Main.instance.DrawTileCracks(1, Main.LocalPlayer.hitTile);
            Main.drawToScreen = drawToScreen;
        }

        Main.instance.TilesRenderer.DrawSpecialTilesLegacy(unscaledPosition, offscreenRange);

        if (TileObject.objectPreview.Active && Main.LocalPlayer.cursorItemIconEnabled && Main.placementPreview && !CaptureManager.Instance.Active)
        {
            Main.instance.LoadTiles(TileObject.objectPreview.Type);
            TileObject.DrawPreview(Main.spriteBatch, TileObject.objectPreview, unscaledPosition - offscreenRange);
        }

        if (snapshot.HasValue)
        {
            Main.spriteBatch.TryEnd(out _);
        }
    }
}