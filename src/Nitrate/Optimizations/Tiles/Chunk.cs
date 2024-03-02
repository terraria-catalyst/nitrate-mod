using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace TeamCatalyst.Nitrate.Optimizations.Tiles;

internal sealed class Chunk : IDisposable {
    public Chunk(RenderTarget2D renderTarget) {
        RenderTarget = renderTarget;
    }

    public RenderTarget2D RenderTarget { get; }

    public List<AnimatedPoint> AnimatedPoints { get; } = new();

    public void Dispose() {
        RenderTarget.Dispose();
    }
}
