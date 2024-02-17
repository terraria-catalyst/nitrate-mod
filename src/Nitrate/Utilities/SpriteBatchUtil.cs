using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Nitrate.Utilities;

internal static class SpriteBatchUtil {
    public readonly struct SpriteBatchSnapshot {
        public SpriteSortMode SortMode { get; }

        public BlendState BlendState { get; }

        public SamplerState SamplerState { get; }

        public DepthStencilState DepthStencilState { get; }

        public RasterizerState RasterizerState { get; }

        public Effect? Effect { get; }

        public FnaMatrix TransformMatrix { get; }

        public SpriteBatchSnapshot(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect? effect, FnaMatrix transformMatrix) {
            SortMode = sortMode;
            BlendState = blendState;
            SamplerState = samplerState;
            DepthStencilState = depthStencilState;
            RasterizerState = rasterizerState;
            Effect = effect;
            TransformMatrix = transformMatrix;
        }
    }

    private sealed class TemporaryRestartContext : IDisposable {
        private readonly SpriteBatch spriteBatch;
        private readonly GraphicsDevice graphicsDevice;
        private readonly SpriteBatchSnapshot snapshot;

        public TemporaryRestartContext(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, SpriteBatchSnapshot snapshot) {
            this.spriteBatch = spriteBatch;
            this.graphicsDevice = graphicsDevice;
            this.snapshot = snapshot;
        }

        public void Dispose() {
            spriteBatch.End();
            graphicsDevice.SetRenderTarget(null);

            spriteBatch.Begin(
                snapshot.SortMode,
                snapshot.BlendState,
                snapshot.SamplerState,
                snapshot.DepthStencilState,
                snapshot.RasterizerState,
                snapshot.Effect,
                snapshot.TransformMatrix
            );
        }
    }

    public static bool TryEnd(this SpriteBatch spriteBatch, out SpriteBatchSnapshot snapshot) {
        if (!spriteBatch.beginCalled) {
            snapshot = default;

            return false;
        }

        snapshot = new SpriteBatchSnapshot(
            spriteBatch.sortMode,
            spriteBatch.blendState,
            spriteBatch.samplerState,
            spriteBatch.depthStencilState,
            spriteBatch.rasterizerState,
            spriteBatch.customEffect,
            spriteBatch.transformMatrix
        );

        spriteBatch.End();

        return true;
    }

    public static IDisposable BeginDrawingToRenderTarget(this SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, RenderTarget2D renderTarget2D) {
        spriteBatch.TryEnd(out var snapshot);

        graphicsDevice.SetRenderTarget(renderTarget2D);
        graphicsDevice.Clear(Color.Transparent);

        spriteBatch.Begin(
            snapshot.SortMode,
            snapshot.BlendState,
            snapshot.SamplerState,
            snapshot.DepthStencilState,
            snapshot.RasterizerState,
            snapshot.Effect,
            snapshot.TransformMatrix
        );

        return new TemporaryRestartContext(spriteBatch, graphicsDevice, snapshot);
    }

    public static void BeginWithSnapshot(this SpriteBatch spriteBatch, SpriteBatchSnapshot snapshot) {
        spriteBatch.Begin(
            snapshot.SortMode,
            snapshot.BlendState,
            snapshot.SamplerState,
            snapshot.DepthStencilState,
            snapshot.RasterizerState,
            snapshot.Effect,
            snapshot.TransformMatrix
        );
    }
}
