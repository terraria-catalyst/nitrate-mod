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

    public RenderTarget2D? ScreenTarget_AffectedByLighting { get; set; }

    public RenderTarget2D? ScreenTarget_NotAffectedByLighting { get; set; }

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
        if (ScreenTarget_AffectedByLighting is null || ScreenTarget_NotAffectedByLighting is null || screenSizeLightingBuffer is null)
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

        Main.spriteBatch.Draw(ScreenTarget_AffectedByLighting, Vector2.Zero, Color.White);
        Main.spriteBatch.End();

        Main.spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone
        );

        Main.spriteBatch.Draw(ScreenTarget_NotAffectedByLighting, Vector2.Zero, Color.White);
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

                ScreenTarget_AffectedByLighting?.Dispose();
                ScreenTarget_AffectedByLighting = null;
                
                ScreenTarget_NotAffectedByLighting?.Dispose();
                ScreenTarget_NotAffectedByLighting = null;
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