using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nitrate.API.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Nitrate.Optimizations.Tiles;

internal abstract class ChunkCollection
{
    public Dictionary<Point, Chunk> Loaded { get; } = new();

    public readonly List<Point> NeedsPopulating = new();

    public RenderTarget2D? ScreenTarget { get; set; }

    public virtual void LoadChunk(Point key)
    {
        RenderTarget2D target = new(
            Main.graphics.GraphicsDevice,
            ChunkSystem.CHUNK_SIZE,
            ChunkSystem.CHUNK_SIZE,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.PreserveContents
        );

        Loaded[key] = new Chunk(target);
        NeedsPopulating.Add(key);
    }

    public virtual void UnloadChunk(Point key)
    {
        Loaded[key].Dispose();
        NeedsPopulating.Remove(key);
    }

    public abstract void PopulateChunk(Point key);

    public abstract void DrawChunksToChunkTarget(GraphicsDevice device);

    public virtual void RenderChunksWithLighting(RenderTarget2D? screenSizeLightingBuffer, Lazy<Effect> lightMapRenderer)
    {
        if (ScreenTarget is null || screenSizeLightingBuffer is null)
        {
            return;
        }

        Main.spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            lightMapRenderer.Value
        );

        lightMapRenderer.Value.Parameters["lightMap"].SetValue(screenSizeLightingBuffer);

        Main.spriteBatch.Draw(ScreenTarget, Vector2.Zero, Color.White);
        Main.spriteBatch.End();
    }

    public virtual void DisposeAllChunks()
    {
        Main.RunOnMainThread(() =>
        {
            List<Chunk> loadedCopy = Loaded.Values.ToList();

            // Capture a copy that wasn't cleared to avoid memory leaks.
            Main.RunOnMainThread(() =>
            {
                foreach (Chunk chunk in loadedCopy)
                {
                    chunk.Dispose();
                }

                ScreenTarget?.Dispose();
                ScreenTarget = null;
            });

            Loaded.Clear();
            NeedsPopulating.Clear();
        });
    }

    public virtual void RemoveOutOfBoundsAndPopulate(int topX, int bottomX, int topY, int bottomY)
    {
        List<Point> removeList = new();

        foreach (Point key in Loaded.Keys)
        {
            if (key.X >= topX && key.X <= bottomX && key.Y >= topY && key.Y <= bottomY)
            {
                continue;
            }

            UnloadChunk(key);
            removeList.Add(key);
        }

        foreach (Point key in removeList)
        {
            Loaded.Remove(key);
        }

        foreach (Point key in NeedsPopulating)
        {
            PopulateChunk(key);
        }

        NeedsPopulating.Clear();
    }
}

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
                    ModifiedTileDrawing.DrawSingleTile(chunkPositionWorld, Vector2.Zero, tileX, tileY);
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

            foreach (Point tilePoint in chunk.AnimatedPoints)
            {
                Tile tile = Framing.GetTileSafely(tilePoint);

                if (!tile.HasTile)
                {
                    continue;
                }

                // Main.instance.TilesRenderer.DrawSingleTile(new TileDrawInfo(), true, 0, new Vector2(tilePoint.X * 16, tilePoint.Y * 16), Vector2.Zero, tilePoint.X, tilePoint.Y);

                // TODO: Check IsTileDrawLayerSolid, solidLayer, DrawTile_LiquidBehindTile
                if (!TextureAssets.Tile[tile.type].IsLoaded)
                {
                    Main.instance.LoadTiles(tile.type);
                }

                if (TileLoader.PreDraw(tilePoint.X, tilePoint.Y, tile.type, Main.spriteBatch))
                {
                    ModifiedTileDrawing.StillHandleSpecialsBecauseTerrariaWasPoorlyProgrammed(tile.type, true, tilePoint.X, tilePoint.Y, tile.frameX, tile.frameY, tile);
                    Main.instance.TilesRenderer.DrawSingleTile(new TileDrawInfo(), true, 0, new Vector2(tilePoint.X * 16, tilePoint.Y * 16), Vector2.Zero, tilePoint.X, tilePoint.Y);
                }

                TileLoader.PostDraw(tilePoint.X, tilePoint.Y, tile.type, Main.spriteBatch);
            }
        }

        Main.spriteBatch.End();

        device.SetRenderTargets(bindings);
    }
}

internal sealed class WallChunkCollection : ChunkCollection
{
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

                if (!tile.HasTile)
                {
                    continue;
                }

                if (AnimatedTileRegistry.IsWallPossiblyAnimated(tile.WallType))
                {
                    chunk.AnimatedPoints.Add(new Point(tileX, tileY));
                }
                else
                {
                    ModifiedWallDrawing.DrawSingleWall(tileX, tileY, chunkPositionWorld);
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

            foreach (Point wallPoint in chunk.AnimatedPoints)
            {
                ModifiedWallDrawing.DrawSingleWallMostlyUnmodified(wallPoint.X, wallPoint.Y, new Vector2(key.X * ChunkSystem.CHUNK_SIZE, key.Y * ChunkSystem.CHUNK_SIZE));
            }
        }

        Main.spriteBatch.End();

        device.SetRenderTargets(bindings);
    }
}