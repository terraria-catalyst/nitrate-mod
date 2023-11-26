using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace Zenith.Core.Features.PrimitiveRendering;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class PrimitiveRenderingSystem : ModSystem
{
    private readonly struct RenderingStepData : IDisposable
    {
        public List<Action> RenderEntries { get; } = new();

        public RenderTarget2D RenderTarget { get; } = new(
            Main.graphics.GraphicsDevice,
            Main.screenWidth,
            Main.screenHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.PreserveContents
        );

        public RenderingStepData()
        {
        }

        public void Dispose()
        {
            RenderTarget.Dispose();
        }
    }

    private readonly Dictionary<string, RenderingStepData> _renderData = new();

    public override void Load()
    {
        On_Main.DrawProjectiles += DrawRenderTargets;
        Main.OnResolutionChanged += TargetsNeedResizing;
    }

    public override void Unload()
    {
        On_Main.DrawProjectiles -= DrawRenderTargets;
        Main.OnResolutionChanged -= TargetsNeedResizing;

        Main.RunOnMainThread(() =>
        {
            foreach (RenderingStepData data in _renderData.Values)
            {
                data.RenderTarget.Dispose();
            }
        });
    }

    public override void PostUpdateEverything()
    {
        if (Main.gameMenu || Main.dedServ)
        {
            return;
        }

        GraphicsDevice device = Main.graphics.GraphicsDevice;

        foreach (string id in _renderData.Keys)
        {
            RenderTargetBinding[] bindings = device.GetRenderTargets();

            device.SetRenderTarget(_renderData[id].RenderTarget);
            device.Clear(Color.Transparent);

            foreach (Action action in _renderData[id].RenderEntries)
            {
                action.Invoke();
            }

            device.SetRenderTargets(bindings);
            Finish(id);
        }
    }

    private void DrawRenderTargets(On_Main.orig_DrawProjectiles orig, Main self)
    {
        orig(self);

        foreach (string id in _renderData.Keys)
        {
            Main.spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.AlphaBlend,
                SamplerState.PointWrap,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );

            Main.spriteBatch.Draw(_renderData[id].RenderTarget, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        }
    }

    private void TargetsNeedResizing(Vector2 _)
    {
        Main.RunOnMainThread(() =>
        {
            foreach (RenderingStepData data in _renderData.Values)
            {
                data.Dispose();
            }
        });

        foreach (string id in _renderData.Keys)
        {
            _renderData[id] = new RenderingStepData();
        }
    }

    /// <summary>
    ///     Registers a render target for use with a drawing action or list of
    ///     drawing actions.
    /// </summary>
    /// <param name="id">The ID of the render target and its layer.</param>
    public void RegisterRenderTarget(string id)
    {
        Main.RunOnMainThread(() =>
        {
            _renderData[id] = new RenderingStepData();
        });
    }

    /// <summary>
    ///     Queues a render action to be executed on the next rendering step.
    /// </summary>
    /// <param name="id">The ID of the render target to render to.</param>
    /// <param name="renderAction">The action to be executed.</param>
    public void QueueRenderAction(string id, Action renderAction)
    {
        _renderData[id].RenderEntries.Add(renderAction);
    }

    private void Finish(string id)
    {
        _renderData[id].RenderEntries.Clear();
    }
}