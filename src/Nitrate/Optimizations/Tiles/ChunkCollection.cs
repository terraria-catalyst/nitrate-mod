using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace Nitrate.Optimizations.Tiles;

internal abstract class ChunkCollection {
    public Dictionary<Point, Chunk> Loaded { get; } = new();

    public readonly List<Point> NeedsPopulating = new();
    protected readonly List<Point> NeedsRePopulating = new();
    protected readonly Dictionary<Point, byte> FailedPopulations = new();

    public RenderTarget2D? ScreenTarget { get; set; }

    public virtual bool ApplyOverride => Main.LocalPlayer.dangerSense || Main.LocalPlayer.findTreasure || Main.LocalPlayer.biomeSight;

    public virtual void LoadChunk(Point key) {
        if (NeedsPopulating.Contains(key)) return;
        
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

    public virtual void UnloadChunk(Point key) {
        Loaded[key].Dispose();
        NeedsPopulating.Remove(key);
    }

    public abstract void PopulateChunk(Point key);

    public abstract void DrawChunksToChunkTarget(GraphicsDevice device);

    public virtual void RenderChunksWithLighting(RenderTarget2D? screenSizeLightingBuffer, RenderTarget2D? screenSizeOverrideBuffer, Lazy<Effect> lightMapRenderer) {
        if (ScreenTarget is null || screenSizeLightingBuffer is null) {
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

        lightMapRenderer.Value.Parameters["applyOverride"].SetValue(ApplyOverride);

        lightMapRenderer.Value.Parameters["gameViewMatrix"].SetValue(Main.GameViewMatrix.TransformationMatrix);

        // If not set it will default to being empty which will not apply any override colors.
        if (screenSizeOverrideBuffer is not null) {
            lightMapRenderer.Value.Parameters["overrideMap"].SetValue(screenSizeOverrideBuffer);
        }

        lightMapRenderer.Value.Parameters["size"].SetValue(new Vector2(screenSizeLightingBuffer.Width, screenSizeLightingBuffer.Height));

        // The offset vector is the amount of pixels from the corner the first tile is.
        lightMapRenderer.Value.Parameters["offset"].SetValue(new Vector2(16) - new Vector2(Main.screenPosition.X % 16, Main.screenPosition.Y % 16));

        Main.spriteBatch.Draw(ScreenTarget, Vector2.Zero, Color.White);
        Main.spriteBatch.End();
    }

    public virtual void DisposeAllChunks() {
        var loadedCopy = Loaded.Values;

        // Capture a copy that wasn't cleared to avoid memory leaks.
        Main.RunOnMainThread(
            () => {
                foreach (var chunk in loadedCopy) {
                    chunk.Dispose();
                }

                ScreenTarget?.Dispose();
                ScreenTarget = null;
            }
        );

        Loaded.Clear();
        NeedsPopulating.Clear();
    }

    public virtual void RemoveOutOfBoundsAndPopulate(int topX, int bottomX, int topY, int bottomY) {
        var copy = Loaded;
        foreach (var key in copy.Keys) {
            if ((key.X >= topX && key.X <= bottomX && key.Y >= topY && key.Y <= bottomY) || NeedsPopulating.Contains(key)) {
                continue;
            }

            UnloadChunk(key);
            Loaded.Remove(key);
            FailedPopulations.Remove(key);
        }

        // Only repopulate chunks once every 4 frames, like vanilla does with tiles.
        if (Main.GameUpdateCount % 4 == 0) {
            foreach (var key in NeedsPopulating) {
                PopulateChunk(key);
            }
            
            foreach (var key in NeedsPopulating.Except(NeedsRePopulating)) {
                if (!FailedPopulations.TryAdd(key, 6)) {
                    FailedPopulations[key] = 6;
                }
            }
            
            NeedsPopulating.Clear();
            NeedsPopulating.AddRange(NeedsRePopulating);
            NeedsRePopulating.Clear();
        }
    }
}
