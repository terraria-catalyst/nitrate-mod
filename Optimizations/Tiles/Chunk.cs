using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Nitrate.Optimizations.Tiles;

internal abstract class Chunk : IDisposable
{
    public RenderTarget2D RenderTarget { get; }

    protected Chunk(RenderTarget2D renderTarget)
    {
        RenderTarget = renderTarget;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            RenderTarget.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

internal sealed class TileChunk : Chunk
{
    public List<Point> AnimatedTiles { get; }

    public TileChunk(RenderTarget2D renderTarget, List<Point> animatedTiles) : base(renderTarget)
    {
        AnimatedTiles = animatedTiles;
    }
}

internal sealed class WallChunk : Chunk
{
    public List<Point> AnimatedWalls { get; }

    public WallChunk(RenderTarget2D renderTarget, List<Point> animatedWalls) : base(renderTarget)
    {
        AnimatedWalls = animatedWalls;
    }
}