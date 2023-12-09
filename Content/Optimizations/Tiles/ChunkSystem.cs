using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nitrate.Core.Utilities;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace Nitrate.Content.Optimizations.Tiles;

internal sealed class ChunkSystem : ModSystem
{
    // Good sizes include 20, 25, 40, 50, and 100 tiles, as these sizes all multiply evenly into every single default world size's width and height.
    private const int CHUNK_SIZE = 40 * 16;

    // The number of layers of additional chunks that stay loaded off-screen around the player. Could help improve performance when moving around in one location.
    private const int CHUNK_OFFSCREEN_BUFFER = 1;

    private readonly Dictionary<Point, RenderTarget2D> _loadedChunks = new();

    private readonly Dictionary<Point, RenderTarget2D> _needsPopulating = new();

    public override void OnWorldUnload()
    {
        base.OnWorldUnload();

        DisposeAllChunks();
    }

    public override void PostUpdateEverything()
    {
        base.PostUpdateEverything();

        Rectangle screenArea = new((int)Main.screenPosition.X, (int)Main.screenPosition.Y, Main.screenWidth, Main.screenHeight);

        // Chunk coordinates are incremented by 1 in each direction per chunk; 1 unit in chunk coordinates is equal to CHUNK_SIZE.
        // Chunk coordinates of the top-leftmost visible chunk.
        int topX = (int)Math.Floor((double)screenArea.X / CHUNK_SIZE) - CHUNK_OFFSCREEN_BUFFER;
        int topY = (int)Math.Floor((double)screenArea.Y / CHUNK_SIZE) - CHUNK_OFFSCREEN_BUFFER;

        // Chunk coordinates of the bottom-rightmost visible chunk.
        int bottomX = (int)Math.Floor((double)(screenArea.X + screenArea.Width) / CHUNK_SIZE) + CHUNK_OFFSCREEN_BUFFER;
        int bottomY = (int)Math.Floor((double)(screenArea.Y + screenArea.Height) / CHUNK_SIZE) + CHUNK_OFFSCREEN_BUFFER;

        // Make sure all chunks onscreen as well as the buffer are loaded.
        for (int x = topX; x <= bottomX; x++)
        {
            for (int y = topY; y <= bottomY; y++)
            {
                Point chunkKey = new(x, y);

                if (!_loadedChunks.ContainsKey(chunkKey))
                {
                    LoadChunk(chunkKey);
                }
            }
        }

        List<Point> removeList = new();

        foreach (Point key in _loadedChunks.Keys)
        {
            // If this chunk is outside the load range, unload it.
            if (key.X < topX || key.X > bottomX || key.Y < topY || key.Y > bottomY)
            {
                UnloadChunk(key);

                removeList.Add(key);
            }
        }

        foreach (Point key in removeList)
        {
            _loadedChunks.Remove(key);
        }
    }

    public override void PostDrawTiles()
    {
        base.PostDrawTiles();

        foreach (Point key in _needsPopulating.Keys)
        {
            PopulateChunk(key);
        }

        _needsPopulating.Clear();

        Main.spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            null,
            Main.GameViewMatrix.TransformationMatrix
        );

        Rectangle screenArea = new((int)Main.screenPosition.X, (int)Main.screenPosition.Y, Main.screenWidth, Main.screenHeight);

        foreach (Point key in _loadedChunks.Keys)
        {
            RenderTarget2D chunk = _loadedChunks[key];

            Rectangle chunkArea = new(key.X * CHUNK_SIZE, key.Y * CHUNK_SIZE, chunk.Width, chunk.Height);

            if (!chunkArea.Intersects(screenArea))
            {
                continue;
            }

            // This should never happen, something catastrophic happened if it did.
            // The check here is because rendering disposed targets generally has strange behaviour and doesn't always throw exceptions.
            // Therefore this check needs to be made as it's more robust.
            if (chunk.IsDisposed)
            {
                throw new Exception("Attempted to render a disposed chunk.");
            }

            Main.spriteBatch.Draw(chunk, new Vector2(chunkArea.X, chunkArea.Y) - Main.screenPosition, Color.White);
        }

        Main.spriteBatch.End();
    }

    private void DisposeAllChunks()
    {
        Main.RunOnMainThread(() =>
        {
            foreach (RenderTarget2D chunk in _loadedChunks.Values)
            {
                chunk.Dispose();
            }
        });

        _loadedChunks.Clear();
    }

    private void LoadChunk(Point chunkKey)
    {
        RenderTarget2D chunk = new(
            Main.graphics.GraphicsDevice,
            CHUNK_SIZE,
            CHUNK_SIZE,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.PreserveContents
        );

        _loadedChunks[chunkKey] = chunk;
        _needsPopulating[chunkKey] = chunk;
    }

    private void PopulateChunk(Point chunkKey)
    {
        RenderTarget2D chunk = _loadedChunks[chunkKey];

        GraphicsDevice device = Main.graphics.GraphicsDevice;

        RenderTargetBinding[] bindings = device.GetRenderTargets();

        device.SetRenderTarget(chunk);

        Main.spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            null,
            Main.GameViewMatrix.TransformationMatrix
        );

        Vector2 chunkPositionWorld = new(chunkKey.X * CHUNK_SIZE, chunkKey.Y * CHUNK_SIZE);

        int sizeTiles = CHUNK_SIZE / 16;

        Point chunkPositionTile = new((int)chunkPositionWorld.X / 16, (int)chunkPositionWorld.Y / 16);

        for (int i = 0; i < sizeTiles; i++)
        {
            for (int j = 0; j < sizeTiles; j++)
            {
                int tileX = chunkPositionTile.X + i;
                int tileY = chunkPositionTile.Y + j;

                TileUtil.RenderTileAt(tileX, tileY, chunkPositionWorld);
            }
        }

        Main.spriteBatch.End();

        device.SetRenderTargets(bindings);
    }

    private void UnloadChunk(Point chunkKey) => _loadedChunks[chunkKey].Dispose();
}
