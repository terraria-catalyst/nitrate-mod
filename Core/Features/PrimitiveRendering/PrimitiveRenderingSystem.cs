using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace Zenith.Core.Features.PrimitiveRendering;

public class PrimitiveRenderingSystem : ModSystem
{
    private readonly Dictionary<string, RenderingStepData> renderData = new();

    public override void Load()
    {
        On_Main.DrawProjectiles += DrawRenderTargets;

        Main.OnResolutionChanged += OnResolutionChangedTargetsNeedResizing;
    }

    public override void Unload()
    {
        Main.OnResolutionChanged -= OnResolutionChangedTargetsNeedResizing;

        Main.RunOnMainThread(() =>
        {
            foreach (RenderingStepData data in renderData.Values)
            {
                data.RenderTarget.Dispose();
            }
        });
    }

    private void OnResolutionChangedTargetsNeedResizing(Vector2 _)
    {
        TargetsNeedResizing();
    }

    public override void PostUpdateEverything()
    {
        if (Main.gameMenu || Main.dedServ)
        {
            return;
        }

        foreach (string id in renderData.Keys)
        {
            GraphicsDevice device = Main.graphics.GraphicsDevice;

            RenderTargetBinding[] bindings = device.GetRenderTargets();

            device.SetRenderTarget(renderData[id].RenderTarget);
            device.Clear(Color.Transparent);

            foreach (Action action in renderData[id].RenderEntries)
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

        foreach (string id in renderData.Keys)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(renderData[id].RenderTarget, Vector2.Zero, Color.White);

            Main.spriteBatch.End();
        }
    }

    public void TargetsNeedResizing()
    {
        foreach (RenderingStepData data in renderData.Values)
        {
            Main.RunOnMainThread(data.RenderTarget.Dispose);
        }

        foreach (string id in renderData.Keys)
        {
            renderData[id] = new RenderingStepData();
        }
    }

    /// <summary>
    /// Registers a rendertarget for use with a drawing action or list of drawing actions.
    /// </summary>
    /// <param name="id">ID of the rendertarget and its layer.</param>
    public void RegisterRenderTarget(string id)
    {
        Main.RunOnMainThread(() =>
        {
            renderData[id] = new RenderingStepData();
        });
    }

    public void QueueRenderAction(string id, Action renderAction)
    {
        renderData[id].RenderEntries.Add(renderAction);
    }

    private void Finish(string id)
    {
        renderData[id].RenderEntries.Clear();
    }

    private class RenderingStepData
    {
        public List<Action> RenderEntries = new();

        public RenderTarget2D RenderTarget = new(
            Main.graphics.GraphicsDevice,
            Main.screenWidth,
            Main.screenHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.PreserveContents
        );
    }
}