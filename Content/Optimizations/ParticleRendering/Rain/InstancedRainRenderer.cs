using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nitrate.Core.Features.Rendering;
using Nitrate.Core.Features.Threading;
using Nitrate.Core.Utilities;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Nitrate.Content.Optimizations.ParticleRendering.Rain;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal class InstancedRainRenderer : AbstractInstancedParticleRenderer<ParticleInstance>
{
    private const string dust_target = "DustTarget";

    protected override Lazy<Effect> InstanceParticleRenderer { get; }

    public InstancedRainRenderer() : base(Main.maxRain, dust_target)
    {
        InstanceParticleRenderer = new Lazy<Effect>(() => Mod.Assets.Request<Effect>("Assets/Effects/InstancedParticleRenderer", AssetRequestMode.ImmediateLoad).Value);
    }

    public override void Load()
    {
        base.Load();
    }

    protected override Texture2D MakeAtlas() => TextureAssets.Rain.Value;

    public override void PreUpdateDusts()
    {
        base.PreUpdateDusts();

        ModContent.GetInstance<ActionableRenderTargetSystem>().QueueRenderAction(dust_target, () =>
        {
            GraphicsDevice device = Main.graphics.GraphicsDevice;

            device.RasterizerState = RasterizerState.CullNone;

            SimdMatrix projection = SimdMatrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);

            InstanceParticleRenderer.Value.Parameters["transformMatrix"].SetValue(projection.ToFna());
            InstanceParticleRenderer.Value.Parameters["dustTexture"].SetValue(ParticleAtlas);

            SetInstanceData();

            // Something has gone seriously wrong.
            if (VertexBuffer is null || IndexBuffer is null)
            {
                return;
            }

            // Instanced render all particles.
            device.SetVertexBuffers(VertexBuffer, new VertexBufferBinding(InstanceBuffer, 0, 1));
            device.Indices = IndexBuffer;

            foreach (EffectPass pass in InstanceParticleRenderer.Value.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, VertexBuffer.VertexCount, 0, IndexBuffer.IndexCount / 3, Particles.Length);
            }
        });
    }

    private void SetInstanceData()
    {
        FasterParallel.For(0, Particles.Length, (inclusive, exclusive, _) =>
        {
            for (int i = inclusive; i < exclusive; i++)
            {
                Terraria.Rain rain = Main.rain[i];

                // Something has gone seriously wrong if the atlas is null.
                if (rain.active && ParticleAtlas is not null)
                {
                    Rectangle frame = new(rain.type * 4, 0, 2, 40);

                    float halfWidth = (int)(frame.Width / 2f);
                    float halfHeight = (int)(frame.Height / 2f);

                    FnaVector2 initialOffset = new(-halfWidth, -halfHeight);

                    SimdMatrix rotation = SimdMatrix.CreateRotationZ(rain.rotation);
                    SimdMatrix offset = SimdMatrix.CreateTranslation(initialOffset.X / 2, initialOffset.Y / 2, 0);
                    SimdMatrix reset = SimdMatrix.CreateTranslation(-initialOffset.X / 2, -initialOffset.Y / 2, 0);

                    SimdMatrix rotationMatrix = offset * rotation * reset;

                    SimdMatrix world =
                        SimdMatrix.CreateScale(rain.scale * frame.Width, rain.scale * frame.Height, 1) *
                        rotationMatrix *
                        SimdMatrix.CreateTranslation(
                            (int)(rain.position.X - Main.screenPosition.X + initialOffset.X),
                            (int)(rain.position.Y - Main.screenPosition.Y + initialOffset.Y),
                            0
                        );

                    float uvX = (float)frame.X / ParticleAtlas.Width;
                    float uvY = (float)frame.Y / ParticleAtlas.Height;
                    float uvW = (float)(frame.X + frame.Width) / ParticleAtlas.Width;
                    float uvZ = (float)(frame.Y + frame.Height) / ParticleAtlas.Height;

                    Color color = Lighting.GetColor((int)(rain.position.X + 4) / 16, (int)(rain.position.Y + 4) / 16) * 0.85f;

                    if (Main.shimmerAlpha > 0f)
                    {
                        color *= 1f - Main.shimmerAlpha;
                    }

                    Particles[i] = new ParticleInstance(world, new Vector4(uvX, uvY, uvW, uvZ), color.ToVector4());
                }
                else
                {
                    Particles[i] = new ParticleInstance();
                }
            }
        });

        InstanceBuffer?.SetData(Particles, 0, Particles.Length, SetDataOptions.None);
    }
}
