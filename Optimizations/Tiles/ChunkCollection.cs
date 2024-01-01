using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

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
        lightMapRenderer.Value.Parameters["size"].SetValue(new Vector2(screenSizeLightingBuffer.Width, screenSizeLightingBuffer.Height));

        // The offset vector is the amount of pixels from the corner the first tile is.
        lightMapRenderer.Value.Parameters["offset"].SetValue(new Vector2(16) - new Vector2(Main.screenPosition.X % 16, Main.screenPosition.Y % 16));

        Main.spriteBatch.Draw(ScreenTarget, Vector2.Zero, Color.White);
        Main.spriteBatch.End();
    }

    public virtual void DisposeAllChunks()
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

        // Only repopulate chunks once every 4 frames, like vanilla does with tiles.
        if (Main.GameUpdateCount % 4 == 0)
        {
            foreach (Point key in NeedsPopulating)
            {
                PopulateChunk(key);
            }

            NeedsPopulating.Clear();
        }
    }
}