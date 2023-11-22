using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace Zenith
{
    public class PrimitiveSystem : ModSystem
    {
        private readonly Dictionary<string, RenderingStepData> renderData;

        public PrimitiveSystem()
        {
            renderData = new Dictionary<string, RenderingStepData>();
        }

        public override void Load()
        {
            On_Main.DrawProjectiles += DrawRenderTargets;

            Main.OnResolutionChanged += resolution =>
            {
                TargetsNeedResizing();
            };
        }

        public override void Unload()
        {
            Main.OnResolutionChanged -= resolution =>
            {
                TargetsNeedResizing();
            };

            foreach (RenderingStepData data in renderData.Values)
            {
                Main.RunOnMainThread(() =>
                {
                    data.RenderTarget.Dispose();
                });
            }
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

                for (int i = 0; i < renderData[id].RenderEntries.Count; i++)
                {
                    renderData[id].RenderEntries[i].Invoke();
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
                renderData[id] = new();
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
            public List<Action> RenderEntries;

            public RenderTarget2D RenderTarget;

            public RenderingStepData()
            {
                RenderEntries = new List<Action>();

                RenderTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight, false,
                    SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }
        }
    }
}
